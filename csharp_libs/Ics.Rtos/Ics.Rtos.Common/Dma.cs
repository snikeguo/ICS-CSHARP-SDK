using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Ics.Rtos.Common
{
    public static class Dma
    {
        [DllImport("*")]
        internal extern static int Hal_Dma_Init();

        public static void Initialize()
        {
            Hal_Dma_Init();
        }
    }
}
