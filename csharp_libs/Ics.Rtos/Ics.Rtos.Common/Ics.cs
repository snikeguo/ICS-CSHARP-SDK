using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Ics.Rtos.Common
{
    public static class Ics
    {

        [DllImport("*")]//获取ICS build string
        internal static extern IntPtr GetIcsBuildString();


        public static void Initialize()
        {

        }
    }
}
