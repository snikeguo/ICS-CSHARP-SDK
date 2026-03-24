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

            if (mode == "uart")
            {
                UartTestSample.Run(index);
            }
            else
            {
                CanTestSample.Run(index);
            }
        }
    }
}
