using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ics.Rtos.Common
{
    public enum QueueState
    {
        Empty,
        Full,
        Ok,
        Timeout,
    }

    /// <summary>
    /// 泛型队列实例：直接 new EmbedQueue&lt;T&gt;() 使用，T 必须为 unmanaged（blittable）。
    /// 零拷贝调用 native queue（unsafe + fixed），封装 SafeHandle，负责释放 native 资源。
    /// </summary>
    public sealed unsafe class EmbedQueue<T> : IDisposable where T : unmanaged
    {
        // SafeHandle 封装 native queue
        public sealed class QueueHandle : SafeHandle
        {
            public QueueHandle()
                : base(IntPtr.Zero, true)
            {
            }

            public QueueHandle(IntPtr handle)
                : base(IntPtr.Zero, true)
            {
                SetHandle(handle);
            }

            public override bool IsInvalid => handle == IntPtr.Zero;

            protected override bool ReleaseHandle()
            {
                try
                {
                    InternalEmbedLibrary.El_DeleteQueue(handle);
                }
                catch
                {
                    // 忽略释放时的异常
                }

                SetHandle(IntPtr.Zero);
                return true;
            }
        }

        private readonly QueueHandle _handle;
        private readonly uint _itemSize;
        private bool _disposed;

        // 默认构造：自动生成队列名，默认 maxItemLen = 1024
        public EmbedQueue()
            : this($"embedqueue_{typeof(T).Name}_{Guid.NewGuid():N}", 1024)
        {
        }

        // 指定队列名与最大条目数
        public EmbedQueue(string queueName, uint maxItemLen)
        {
            if (queueName == null) throw new ArgumentNullException(nameof(queueName));
            _itemSize = (uint)sizeof(T);
            var ptr = InternalEmbedLibrary.El_CreateQueue(queueName, _itemSize, maxItemLen);
            if (ptr == IntPtr.Zero) throw new InvalidOperationException("Create native queue failed.");
            _handle = new QueueHandle(ptr);
        }

        // internal：用已有 native handle 包装（例如由其它组件创建）
        internal EmbedQueue(QueueHandle handle, uint itemSize)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _itemSize = itemSize;
        }

        public QueueHandle Handle => _handle;

        public uint ItemSize => _itemSize;

        public bool IsInvalid => _handle == null || _handle.IsInvalid;

        public void Reset(bool isIsr = false)
        {
            ThrowIfDisposed();
            InternalEmbedLibrary.El_ResetQueue(_handle.DangerousGetHandle(), isIsr ? 1 : 0);
        }

        public uint SpacesAvailable(bool isIsr = false)
        {
            ThrowIfDisposed();
            return InternalEmbedLibrary.El_QueueSpacesAvailable(_handle.DangerousGetHandle(), isIsr ? 1 : 0);
        }

        /// <summary>
        /// 接收单个项，写入 destination[0]（destination 必须至少长度 1）。
        /// 返回 QueueState。
        /// </summary>
        public QueueState Receive(Span<T> destination, uint timeoutMilliseconds, bool isIsr = false)
        {
            ThrowIfDisposed();
            if (destination.Length < 1) throw new ArgumentException("destination must have at least 1 element", nameof(destination));

            fixed (T* p = &MemoryMarshal.GetReference(destination))
            {
                var ret = InternalEmbedLibrary.El_ReceiveQueue(_handle.DangerousGetHandle(), (IntPtr)p, _itemSize, timeoutMilliseconds, isIsr ? 1 : 0);
                return (QueueState)(int)ret;
            }
        }

        /// <summary>
        /// 便捷接收到 out item（使用 stackalloc 临时存储）。
        /// </summary>
        public QueueState Receive(out T item, uint timeoutMilliseconds, bool isIsr = false)
        {
            ThrowIfDisposed();
            Span<T> tmp = stackalloc T[1];
            var state = Receive(tmp, timeoutMilliseconds, isIsr);
            item = tmp[0];
            return state;
        }

        /// <summary>
        /// 发送第一个元素（itemSpan[0]）。
        /// </summary>
        public QueueState Send(ReadOnlySpan<T> itemSpan, bool isIsr = false)
        {
            ThrowIfDisposed();
            if (itemSpan.Length < 1) throw new ArgumentException("itemSpan must have at least 1 element", nameof(itemSpan));

            fixed (T* p = &MemoryMarshal.GetReference(itemSpan))
            {
                var ret = InternalEmbedLibrary.El_SendQueue(_handle.DangerousGetHandle(), (IntPtr)p, _itemSize, isIsr ? 1 : 0);
                return (QueueState)(int)ret;
            }
        }

        /// <summary>
        /// 便捷发送单个值。
        /// </summary>
        public QueueState Send(in T item, bool isIsr = false)
        {
            ThrowIfDisposed();
            Span<T> tmp = stackalloc T[1];
            tmp[0] = item;
            return Send(tmp, isIsr);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EmbedQueue<T>));
        }

        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                _handle?.Dispose();
            }
            catch { }
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}