using Ics.Rtos.Abstractions.CanBus;
using System;
using System.Runtime.InteropServices;

namespace Ics.Rtos.Sample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var mode = "uart";
            var index = 1;
            //UartTestSample.Run(1);

            CanDevice.SentStoreCapacity = 100;
            CanDevice.GlobalReceiveQueueSize = 1000;

            CanTestSample.Run(0);
            CanTestSample.Run(1);
        }
    }
}
