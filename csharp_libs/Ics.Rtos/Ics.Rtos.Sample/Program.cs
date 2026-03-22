using System;
using Ics.Rtos.Abstractions.CanBus;

namespace Ics.Rtos.Sample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CanDevice.SentStoreCapacity = 100; // 设置发送报文缓存容量（按需调整）
            CanDevice.GlobalReceiveQueueSize = 1000; // 设置全局接收队列容量（按需调整）

            // 示例：打开 1 路 CAN（deviceIndex = 0），打印收到的事件；若为收到报文则回射（echo）
            var dev = new CanDevice(0);

            // 简单配置（按需调整各字段以匹配底层硬件要求）
            var cfg = new CanXConfig
            {
                FrameFormat = CanXFrameFormat.FdBrs,
                IsEnableFilter = 0,
                IsEnableChannel = 1,
                RunMode = CanXRunMode.LoopBack,
                BaudRateConfig = new CanXRate
                {
                    Rate = 500000,
                    Prescaler = 2,
                    Sjw = 16,
                    Ts1 = 63,
                    Ts2 = 16
                },
                FrameId = 0,
                FrameIdMask = 0xFFFFFFFF,
                FdMode = CanFdMode.Iso,
                DataRateConfig = new CanXRate
                {
                    Rate = 2000000,
                    Prescaler = 2,
                    Sjw = 4,
                    Ts1 = 15,
                    Ts2 = 4
                }
            };

            // 订阅事件
            dev.EventReceived += evt =>
            {
                try
                {
                    Console.WriteLine($"Event: Type={evt.EventType}, TimeStamp={evt.TimeStamp}, Channel={evt.ChannelIndex}");

                    if (evt.EventType == CanXEventType.RxPacket && evt.CanXRxPacket != null)
                    {
                        var rx = evt.CanXRxPacket;
                        Console.WriteLine($"  RxPacket: Id=0x{rx.Identifier:X}, Len={rx.DataLen}, Flags=0x{rx.Flags:X}");

                        // Echo：构建回射包并发送（使用 SendWithKey，timeout 1000ms）
                        var echo = new CanXPacket
                        {
                            Identifier = rx.Identifier,
                            DataLen = rx.DataLen,
                            Data = rx.Data != null ? (byte[])rx.Data.Clone() : Array.Empty<byte>(),
                            Flags = rx.Flags,
                            Key = rx.Key,
                            UserData = rx.UserData
                        };

                        // 使用 ref 以匹配 SendWithKey(ref ...)
                        //var key = dev.SendWithKey(echo);
                        //Console.WriteLine($"  Echo sent with Key={key}");
                    }
                    else if (evt.EventType == CanXEventType.SentResult && evt.CanXSentResult != null)
                    {
                        var sentPacket = evt.CanXSentResult.Packet;
                        var err = evt.CanXSentResult.ErrorCode;
                        Console.WriteLine($"  SentResult: ErrorCode={err}");
                        PrintPacket(sentPacket);
                    }
                    else if (evt.EventType == CanXEventType.BusState && evt.CanXBusState != null)
                    {
                        Console.WriteLine($"  BusState: {evt.CanXBusState.State}, TxErr={evt.CanXBusState.TxErrorCounter}, RxErr={evt.CanXBusState.RxErrorCounter}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Event handler error: {ex}");
                }
            };

            // 打开设备
            if (!dev.Open(cfg))
            {
                Console.Error.WriteLine("Failed to open CAN device 0");
                return;
            }
            var testSendThread=new Thread( () =>
            {
                var testPacket = new CanXPacket
                {
                    Identifier = 0x123,
                    DataLen = 8,
                    Data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 },
                    Flags =   CanXPacketFlags.Data | CanXPacketFlags.Standard,
                    Key = 0,
                    UserData = 0
                };
                while (true)
                {
                    dev.Send(testPacket);
                    Thread.Sleep(1000); // 每秒发送一次
                }
            });
            testSendThread.IsBackground = true;
            testSendThread.Name = "TestSendThread";
            testSendThread.Start();
            Thread.Sleep(Timeout.Infinite);
            // 关闭释放
            dev.Close();
        }

        static void PrintPacket(CanXPacket pkt)
        {
            if (pkt == null)
            {
                Console.WriteLine("    <null packet>");
                return;
            }

            var dataHex = pkt.Data == null || pkt.Data.Length == 0 ? "<empty>" : BitConverter.ToString(pkt.Data);
            Console.WriteLine($"    Packet: Id=0x{pkt.Identifier:X}, Len={pkt.DataLen}, Flags=0x{pkt.Flags:X}, Key={pkt.Key}, UserData={pkt.UserData}");
            Console.WriteLine($"    Data: {dataHex}");
        }
    }
}
