using Ics.Rtos.Abstractions.CanBus;
using Ics.Rtos.Common;
using System.Collections.Concurrent;
using Ics.Rtos.Common;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ics.Rtos.Abstractions.CanBus
{
    /// <summary>
    /// 管理单个 CAN 设备：查询索引、Open/Close、SendWithKey、并通过 SharedReceiver 接收 Event 并回调。
    /// 多个通道共用一个接收线程；收到 QuitEvent 会停止接收线程（并注销所有注册）。
    /// </summary>
    public sealed class CanDevice : IDisposable
    {
        private readonly object _sync = new();
        private bool _opened;

        public int DeviceIndex { get; }

        /// <summary>
        /// 当此设备接收到 Event 时触发（由 SharedReceiver 分发）。
        /// </summary>
        public event Action<CanXEvent>? EventReceived;

        public CanDevice(int deviceIndex)
        {
            DeviceIndex = deviceIndex;
        }

        // 设置全局接收队列大小（用于创建共享全局队列），默认 1024
        private static int _globalReceiveQueueSize = 1024;
        public static int GlobalReceiveQueueSize
        {
            get => _globalReceiveQueueSize;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(GlobalReceiveQueueSize));
                _globalReceiveQueueSize = value;
                SharedReceiver.EnsureGlobalQueueStarted(value);
            }
        }

        // 设置 SentStore.Capacity
        public static int SentStoreCapacity
        {
            get => SentStore.Capacity;
            set => SentStore.SetCapacity(value);
        }

        /// <summary>
        /// 立即释放全局资源并清空发送缓存（幂等）。
        /// </summary>
        public static void DeInit()
        {
            // 确保接收线程与全局队列停止（幂等）
            try { SharedReceiver.StopAll(); } catch { }
            try { SentStore.Clear(); } catch { }
        }

        public static bool IsDeviceIndexValid(int deviceIndex) => NativeMethods.Hal_CanX_IsDeviceIndexValid(deviceIndex);

        /// <summary>
        /// 打开设备并注册到共享接收线程。现在使用一个全局接收队列（SharedReceiver 内部创建并传给 native via Hal_CanX_SetGlobalEventHandle）。
        /// </summary>
        public bool Open(CanXConfig config, uint queueItemSize = 256, uint queueMaxLen = 1024)
        {
            lock (_sync)
            {
                if (_opened) throw new InvalidOperationException("Device already opened.");
                if (!IsDeviceIndexValid(DeviceIndex)) return false;
                var nativeConfig = CanXConfig.ToNative(config);
                var opened = NativeMethods.Hal_CanX_Open(DeviceIndex, ref nativeConfig);
                if (!opened)
                {
                    return false;
                }

                _opened = true;

                SharedReceiver.Register(DeviceIndex, this);
                return true;
            }
        }

        /// <summary>
        /// 关闭设备并从共享接收线程注销，同时释放资源。
        /// </summary>
        public void Close()
        {
            lock (_sync)
            {
                if (!_opened) return;
                SharedReceiver.Unregister(DeviceIndex);
                try { NativeMethods.Hal_CanX_Close(DeviceIndex); } catch { }
                _opened = false;
            }
        }

        /// <summary>
        /// 原始发送接口（保留）：直接调用 native 发送，不生成 key。
        /// 当用户不需要 key 机制时可使用此方法。
        /// </summary>
        public void Send(CanXPacket packet)
        {
            lock (_sync)
            {
                if (!_opened) throw new InvalidOperationException("Device not opened.");
                var nativePacket = CanXPacket.ToNative(packet);
                NativeMethods.Hal_CanX_SendPacket(DeviceIndex, ref nativePacket);
            }
        }

        /// <summary>
        /// 生成全局唯一 KEY (UInt64) 并发送。若 SentStore 容量已满或 Store 返回 0，则返回 0 表示失败，不调用 native 发送。
        /// 成功返回非 0 的 key。
        /// </summary>
        public ulong SendWithKey(CanXPacket packet)
        {
            lock (_sync)
            {
                if (!_opened) throw new InvalidOperationException("Device not opened.");

                var stored = ClonePacket(packet);
                var key = SentStore.Store(DeviceIndex, stored);
                if (key == 0UL)
                {
                    // 存储失败（容量已满），通知调用方
                    return 0UL;
                }
                packet.Key = key;
                var nativePacket = CanXPacket.ToNative(packet);
                NativeMethods.Hal_CanX_SendPacket(DeviceIndex, ref nativePacket);
                return key;
            }
        }
        private static CanXPacket ClonePacket(CanXPacket src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            var dst = new CanXPacket
            {
                Identifier = src.Identifier,
                DataLen = src.DataLen,
                Flags = src.Flags,
                Key = src.Key,
                UserData = src.UserData
            };

            if (src.Data != null && src.Data.Length > 0)
            {
                var copyLen = Math.Min(src.Data.Length, src.DataLen);
                dst.Data = new byte[copyLen];
                Array.Copy(src.Data, 0, dst.Data, 0, copyLen);
            }
            else
            {
                dst.Data = Array.Empty<byte>();
            }

            return dst;
        }

        internal void OnEventReceived(CanXEvent evt)
        {
            try
            {
                EventReceived?.Invoke(evt);
            }
            catch (Exception ex)
            {
                // 回调异常不应中断接收线程
                Console.Error.WriteLine($"CanDevice[{DeviceIndex}] EventReceived handler error: {ex}");
            }
        }

        public void Dispose() => Close();
    }

    /// <summary>
    /// 全局发送存储与 KEY 生成器（单例静态）。
    /// - 使用 UInt64 key
    /// - 当容量已满时 Store 返回 0 表示失败（由调用方处理）
    /// - SetCapacity 会清空现有数据
    /// </summary>
    internal static class SentStore
    {
        // 全局自增计数器（Int64），初始值 4，使第一次 Interlocked.Increment 返回 5
        private static long s_globalCounter = 4;
        private static readonly ConcurrentDictionary<ulong, (int DeviceIndex, CanXPacket Packet)> s_map = new();
        private static int s_capacity = 1024;


        public static void SetCapacity(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            s_capacity = capacity;
            Clear();
        }

        public static int Capacity => s_capacity;

        /// <summary>
        /// 存储并返回 ulong key；若容量已满则返回 0（表示失败）
        /// </summary>
        public static ulong Store(int deviceIndex, CanXPacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            // 快速检查容量：若已满则直接返回失败（不尝试淘汰）
            if (s_map.Count >= s_capacity)
            {
                return 0UL;
            }

            var newKey = (ulong)Interlocked.Increment(ref s_globalCounter); // first -> 5

            s_map[newKey] = (deviceIndex, packet);
            return newKey;
        }

        public static bool TryGetAndRemove(ulong key, out int deviceIndex, out CanXPacket? packet)
        {
            if (s_map.TryRemove(key, out var tuple))
            {
                deviceIndex = tuple.DeviceIndex;
                packet = tuple.Packet;
                return true;
            }

            deviceIndex = -1;
            packet = null;
            return false;
        }

        public static void Clear()
        {
            try
            {
                foreach (var k in s_map.Keys)
                {
                    s_map.TryRemove(k, out _);
                }
            }
            catch { }
        }
    }

    // SharedReceiver: 使用单个全局队列，并把该队列句柄传入 native（Hal_CanX_SetGlobalEventHandle）。
    internal static class SharedReceiver
    {
        private sealed class Registration
        {
            public int DeviceIndex;
            public CanDevice Device;
        }

        private static readonly ConcurrentDictionary<int, Registration> s_regs = new();
        private static CancellationTokenSource? s_cts;
        private static Thread? s_receiveThread;

        // 全局队列
        public static EmbedQueue<NativeMethods.CanXEventNative>? GlobalQueueHandle { get; private set; }

        /// <summary>
        /// 确保全局队列已创建并已向 native 注册（幂等）。
        /// </summary>
        public static unsafe void EnsureGlobalQueueStarted(int maxItems)
        {
            if (GlobalQueueHandle != null) return;

            // 创建全局队列并注册到 native
            GlobalQueueHandle = new EmbedQueue<NativeMethods.CanXEventNative>("can_global_events", (uint)maxItems);
            try
            {
                NativeMethods.Hal_CanX_SetGlobalEventHandle(GlobalQueueHandle.Handle.DangerousGetHandle(), sizeof(NativeMethods.CanXEventNative));
            }
            catch
            {
                // 若 native 注册失败，释放并重新抛出错误给调用者
                try { GlobalQueueHandle.Dispose(); } catch { }
                GlobalQueueHandle = null;
                throw;
            }

            if (s_receiveThread == null || !s_receiveThread.IsAlive)
            {
                s_cts = new CancellationTokenSource();
                s_receiveThread = new Thread(() => ReceiveLoopProc(s_cts.Token))
                {
                    IsBackground = true,
                    Name = "CanDevice_SharedReceiver"
                };
                s_receiveThread.Start();
            }
        }

        public static void Register(int deviceIndex, CanDevice device)
        {
            var reg = new Registration { DeviceIndex = deviceIndex, Device = device };
            if (!s_regs.TryAdd(deviceIndex, reg))
            {
                throw new InvalidOperationException($"Device {deviceIndex} already registered.");
            }
        }

        public static unsafe void Unregister(int deviceIndex)
        {
            if (s_regs.TryRemove(deviceIndex, out var reg))
            {
                // nothing else per-device to dispose
            }

            if (s_regs.IsEmpty && s_cts != null)
            {
                // 停止接收并释放全局队列
                try
                {
                    s_cts.Cancel();
                }
                catch { }

                try { s_receiveThread?.Join(100); } catch { }
                s_receiveThread = null;

                try
                {
                    GlobalQueueHandle?.Dispose();
                }
                catch { }
                GlobalQueueHandle = null;

                // 通知 native 清空全局 handle (可选)
                try { NativeMethods.Hal_CanX_SetGlobalEventHandle(IntPtr.Zero, sizeof(NativeMethods.CanXEventNative)); } catch { }

                try { s_cts.Dispose(); } catch { }
                s_cts = null;
            }
        }

        private static void ReceiveLoopProc(CancellationToken token)
        {
            const uint perReceiveTimeoutMs = 10;

            while (!token.IsCancellationRequested)
            {
                if (GlobalQueueHandle == null || s_regs.IsEmpty)
                {
                    Thread.Sleep(10);
                    continue;
                }

                try
                {
                    var state = GlobalQueueHandle.Receive(out NativeMethods.CanXEventNative nativeEvt, perReceiveTimeoutMs, isIsr: false);

                    if (state == QueueState.Ok)
                    {
                        var evt = CanXEvent.FromNative(ref nativeEvt);
                        if (evt != null)
                        {
                            // 如果是 SentResult —— native 报告了 key（现在为 64 位）
                            // 我们在 C# 层根据 key 取出原始发送报文并从存储中删除，然后把报文放到 evt.CanXSentResult.Packet
                            if (evt.EventType == CanXEventType.SentResult && evt.CanXSentResult != null)
                            {
                                // native event contains CanXSentResultNative with Key (UInt64) and ErrorCode
                                var nativeKey = nativeEvt.CanXSentResult.Key;
                                if (nativeKey != 0UL)
                                {
                                    if (SentStore.TryGetAndRemove(nativeKey, out var storedDeviceIndex, out var sentPacket))
                                    {
                                        // 填充到事件的 CanXSentResult.Packet，并保留 ErrorCode
                                        evt.CanXSentResult.Packet = sentPacket;
                                    }
                                }
                            }

                            if (s_regs.TryGetValue((int)evt.ChannelIndex, out var reg))
                            {
                                reg.Device.OnEventReceived(evt);

                                if (evt.EventType == CanXEventType.QuitEvent)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                // 未注册的通道，丢弃或记录
                                Console.Error.WriteLine($"Received event for unregistered channel {evt.ChannelIndex}");
                            }
                        }
                    }
                    else if (state == QueueState.Timeout)
                    {
                        // nothing
                        Thread.Yield();
                    }
                    else
                    {
                        // small yield
                        Thread.Yield();
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"SharedReceiver loop error: {ex}");
                    // 小休眠后继续
                    for (int i = 0; i < 5 && !token.IsCancellationRequested; i++)
                    {
                        Thread.Sleep(10);
                    }
                }
            }
        }

        /// <summary>
        /// 停止接收线程、清空注册并释放全局队列（幂等）。
        /// 设计要点：
        /// - 尝试将 QuitEvent 入队以促使接收线程尽快退出（若队列可用）；
        /// - 然后取消 token 并等待接收线程结束（短超时）；
        /// - 最后释放队列和通知 native 清空全局 handle。
        /// </summary>
        internal static unsafe void StopAll()
        {
            // 如果已经没有工作则直接返回（幂等）
            if (GlobalQueueHandle == null && (s_cts == null || s_receiveThread == null)) return;

            // 清空注册表（避免新的分发）
            foreach (var key in s_regs.Keys)
            {
                s_regs.TryRemove(key, out _);
            }

            // 先尝试把 QuitEvent 放入队列，告知接收线程优雅退出（可选）
            if (GlobalQueueHandle != null)
            {
                try
                {
                    var quitEvent = new NativeMethods.CanXEventNative
                    {
                        EventType = CanXEventType.QuitEvent,
                        TimeStamp = 0,
                        ChannelIndex = 0
                    };
                    // 使用 Send(in T) 便捷接口（已实现）
                    GlobalQueueHandle.Send(in quitEvent, isIsr: false);
                }
                catch (Exception ex)
                {
                    // 吞掉异常但记录，后续会通过取消 token 强制结束
                    Console.Error.WriteLine($"StopAll: Failed to enqueue QuitEvent: {ex}");
                }
            }

            // 取消接收循环（如果存在）
            try
            {
                s_cts?.Cancel();
            }
            catch { }

            // 等待接收线程结束（短超时，不阻塞过久）
            try { s_receiveThread?.Join(500); } catch { }

            // 释放并断开全局队列
            try { GlobalQueueHandle?.Dispose(); } catch { }
            GlobalQueueHandle = null;

            try { NativeMethods.Hal_CanX_SetGlobalEventHandle(IntPtr.Zero, sizeof(NativeMethods.CanXEventNative)); } catch { }

            if (s_cts != null)
            {
                try { s_cts.Dispose(); } catch { }
                s_cts = null;
            }

            s_receiveThread = null;
        }
    }
}