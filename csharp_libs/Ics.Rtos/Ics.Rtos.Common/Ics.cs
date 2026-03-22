using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Ics.Rtos.Common
{
    public static class Ics
    {
        [DllImport("*")]
        internal static extern void SetIcsNativeaotActiveCode(byte[] activeCode);

        [DllImport("*")]//获取ICS build string
        internal static extern IntPtr GetIcsBuildString();


        public static void Initialize()
        {
            
            byte[] activeCode = new byte[32];
            try
            {
                var buildStringPtr = GetIcsBuildString();
                string buildString = Marshal.PtrToStringAnsi(buildStringPtr);
                Console.WriteLine($"ICS Build : {buildString}");
                using (var fs = new FileStream("/dev/activeCodeArea", FileMode.Open, FileAccess.Read))
                {
                    fs.Read(activeCode, 0, activeCode.Length);
                }
                SetIcsNativeaotActiveCode(activeCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                return;
            }
        }
    }
}
