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
            return;
            try
            {
                var buildStringPtr = GetIcsBuildString();
                string buildString = Marshal.PtrToStringAnsi(buildStringPtr);
                Console.WriteLine($"ICS Build : {buildString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                return;
            }
        }
    }
}
