using System;
using System.Text;
using System.Threading;
using Ics.Rtos.Abstractions.Uart;

namespace Ics.Rtos.Sample
{
    internal static class UartTestSample
    {
        public static void Run(int deviceIndex = 0)
        {
            UartDevice.GlobalReceiveQueueSize = 1000;

            var dev = new UartDevice(deviceIndex);
            var cfg = new UartConfig
            {
                BaudRate = 115200,
                DataBits = UartDataBits.Bits8,
                StopBits = UartStopBits.One,
                Parity = UartParity.None,
                FlowControl = UartFlowControl.None,
                RxBufferSize = 4096
            };

            dev.EventReceived += evt =>
            {
                try
                {
                    Console.WriteLine($"[UART] Event: Type={evt.EventType}, TimeStamp={evt.TimeStamp}, UartIndex={evt.UartIndex}");

                    if (evt.EventType == UartEventType.RxData && evt.RxData != null)
                    {
                        var text = evt.RxData.Data.Length > 0 ? Encoding.ASCII.GetString(evt.RxData.Data) : string.Empty;
                        Console.WriteLine($"  RxData: Len={evt.RxData.Length}, Data(hex)={BitConverter.ToString(evt.RxData.Data)}");
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            Console.WriteLine($"  RxText: {text}");
                        }
                    }
                    else if (evt.EventType == UartEventType.Error && evt.ErrorEvent != null)
                    {
                        Console.WriteLine($"  Error: {evt.ErrorEvent.ErrorCode}");
                    }
                    else if (evt.EventType == UartEventType.Overflow && evt.OverflowEvent != null)
                    {
                        Console.WriteLine($"  Overflow: LostBytes={evt.OverflowEvent.LostBytes}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[UART] Event handler error: {ex}");
                }
            };

            if (!dev.Open(cfg))
            {
                Console.Error.WriteLine($"[UART] Failed to open UART device {deviceIndex}");
                return;
            }

            Console.WriteLine($"[UART] Device {deviceIndex} opened.");

            var heartbeatThread = new Thread(() =>
            {
                while (true)
                {
                    var msg = Encoding.ASCII.GetBytes("uart test heartbeat\r\n");
                    dev.Send(msg, 1000);
                    Thread.Sleep(2000);
                }
            })
            {
                IsBackground = true,
                Name = "UartHeartbeatThread"
            };

            heartbeatThread.Start();
            Thread.Sleep(Timeout.Infinite);

            dev.Close();
        }
    }
}
