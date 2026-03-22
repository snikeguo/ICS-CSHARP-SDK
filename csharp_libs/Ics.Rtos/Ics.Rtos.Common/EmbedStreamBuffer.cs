using System;
using System.Runtime.InteropServices;

namespace Ics.Rtos.Common
{
    /// <summary>
    /// 托管封装：stream buffer 相关 API（SafeHandle + 零拷贝 unsafe 路径）。
    /// </summary>
    public static class EmbedStreamBuffer
    {
        public sealed class StreamBufferHandle : SafeHandle
        {
            public StreamBufferHandle()
                : base(IntPtr.Zero, true)
            {
            }

            public StreamBufferHandle(IntPtr handle)
                : base(IntPtr.Zero, true)
            {
                SetHandle(handle);
            }

            public override bool IsInvalid => handle == IntPtr.Zero;

            protected override bool ReleaseHandle()
            {
                try
                {
                    InternalEmbedLibrary.vStreamBufferDelete(handle);
                }
                catch
                {
                }

                SetHandle(IntPtr.Zero);
                return true;
            }
        }

        public static StreamBufferHandle Create(uint bufferSizeBytes, uint triggerLevelBytes, bool isMessageBuffer)
        {
            var ptr = InternalEmbedLibrary.xStreamBufferGenericCreate(bufferSizeBytes, triggerLevelBytes, isMessageBuffer ? 1 : 0);
            if (ptr == IntPtr.Zero) return null;
            return new StreamBufferHandle(ptr);
        }

        public static unsafe uint Send(StreamBufferHandle buffer, ReadOnlySpan<byte> data, uint ticksToWait)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            var size = (uint)data.Length;
            if (size == 0) return 0;

            fixed (byte* p = &System.Runtime.InteropServices.MemoryMarshal.GetReference(data))
            {
                return InternalEmbedLibrary.xStreamBufferSend(buffer.DangerousGetHandle(), (IntPtr)p, size, ticksToWait);
            }
        }

        public static unsafe uint SendFromIsr(StreamBufferHandle buffer, ReadOnlySpan<byte> data, IntPtr higherPriorityTaskWoken)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            var size = (uint)data.Length;
            if (size == 0) return 0;

            fixed (byte* p = &System.Runtime.InteropServices.MemoryMarshal.GetReference(data))
            {
                return InternalEmbedLibrary.xStreamBufferSendFromISR(buffer.DangerousGetHandle(), (IntPtr)p, size, higherPriorityTaskWoken);
            }
        }

        public static unsafe uint Receive(StreamBufferHandle buffer, Span<byte> destination, uint bufferLengthBytes, uint ticksToWait)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if ((uint)destination.Length < bufferLengthBytes) throw new ArgumentException("destination too small", nameof(destination));

            fixed (byte* p = &System.Runtime.InteropServices.MemoryMarshal.GetReference(destination))
            {
                return InternalEmbedLibrary.xStreamBufferReceive(buffer.DangerousGetHandle(), (IntPtr)p, bufferLengthBytes, ticksToWait);
            }
        }

        public static unsafe uint ReceiveFromIsr(StreamBufferHandle buffer, Span<byte> destination, uint bufferLengthBytes, IntPtr higherPriorityTaskWoken)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if ((uint)destination.Length < bufferLengthBytes) throw new ArgumentException("destination too small", nameof(destination));

            fixed (byte* p = &System.Runtime.InteropServices.MemoryMarshal.GetReference(destination))
            {
                return InternalEmbedLibrary.xStreamBufferReceiveFromISR(buffer.DangerousGetHandle(), (IntPtr)p, bufferLengthBytes, higherPriorityTaskWoken);
            }
        }

        public static void Delete(StreamBufferHandle buffer)
        {
            buffer?.Dispose();
        }

        public static bool IsFull(StreamBufferHandle buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return InternalEmbedLibrary.xStreamBufferIsFull(buffer.DangerousGetHandle()) != 0;
        }

        public static bool IsEmpty(StreamBufferHandle buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return InternalEmbedLibrary.xStreamBufferIsEmpty(buffer.DangerousGetHandle()) != 0;
        }

        public static bool Reset(StreamBufferHandle buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return InternalEmbedLibrary.xStreamBufferReset(buffer.DangerousGetHandle()) != 0;
        }

        public static uint SpacesAvailable(StreamBufferHandle buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return InternalEmbedLibrary.xStreamBufferSpacesAvailable(buffer.DangerousGetHandle());
        }

        public static uint NextMessageLengthBytes(StreamBufferHandle buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return InternalEmbedLibrary.xStreamBufferNextMessageLengthBytes(buffer.DangerousGetHandle());
        }
    }
}