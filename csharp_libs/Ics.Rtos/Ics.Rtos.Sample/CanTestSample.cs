using System;
using System.Threading;
using Ics.Rtos.Abstractions.CanBus;

namespace Ics.Rtos.Sample
{
    internal static class CanTestSample
    {
        public static void Run(int deviceIndex = 0)
        {
            CanDevice.SentStoreCapacity = 100;
            CanDevice.GlobalReceiveQueueSize = 1000;

            var dev = new CanDevice(deviceIndex);

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

            dev.EventReceived += evt =>
            {
                try
                {
                    Console.WriteLine($"[CAN] Event: Type={evt.EventType}, TimeStamp={evt.TimeStamp}, Channel={evt.ChannelIndex}");

                    if (evt.EventType == CanXEventType.RxPacket && evt.CanXRxPacket != null)
                    {
                        var rx = evt.CanXRxPacket;
                        Console.WriteLine($"  RxPacket: Id=0x{rx.Identifier:X}, Len={rx.DataLen}, Flags=0x{rx.Flags:X}");
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
                    Console.Error.WriteLine($"[CAN] Event handler error: {ex}");
                }
            };

            if (!dev.Open(cfg))
            {
                Console.Error.WriteLine($"[CAN] Failed to open CAN device {deviceIndex}");
                return;
            }

            Console.WriteLine($"[CAN] Device {deviceIndex} opened. Sending test frame every 1s...");

            var testSendThread = new Thread(() =>
            {
                var testPacket = new CanXPacket
                {
                    Identifier = 0x123,
                    DataLen = 8,
                    Data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 },
                    Flags = CanXPacketFlags.Data | CanXPacketFlags.Standard,
                    Key = 0,
                    UserData = 0
                };

                while (true)
                {
                    dev.Send(testPacket);
                    Thread.Sleep(1000);
                }
            })
            {
                IsBackground = true,
                Name = "CanTestSendThread"
            };

            testSendThread.Start();
            Thread.Sleep(Timeout.Infinite);

            dev.Close();
        }

        private static void PrintPacket(CanXPacket? pkt)
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
