using System.Runtime.InteropServices;

namespace Ics.Rtos.Common
{
    public static class InternalNetLibrary
    {
        [DllImport("*")]
        public static extern int GetIntrefaceIndex(string ifname);

        [DllImport("*")]
        public static extern int GetMacAddress(string ifname, byte[] mac);

        [DllImport("*")]
        public static extern int GetIpV6Address(string ifname, byte[] ip);

        [DllImport("*")]
        public static extern int SetIpV6Address(string ifname, byte[] ip);

        // IPv4 must be provided as a 4-byte array in network order (dot-decimal order).
        // Example: 127.0.0.1 -> new byte[] { 127, 0, 0, 1 }
        [DllImport("*")]
        public static extern int SetIpV4Address(string ifname, byte[] ip);

        // IPv4 gateway must be 4 bytes in network order.
        // Example: 10.0.0.1 -> new byte[] { 10, 0, 0, 1 }
        [DllImport("*")]
        public static extern int SetIpV4Gateway(string ifname, byte[] gateway);

        // IPv4 netmask must be 4 bytes in network order.
        // Example: 255.255.255.0 -> new byte[] { 255, 255, 255, 0 }
        [DllImport("*")]
        public static extern int SetIpV4Netmask(string ifname, byte[] netmask);

        [DllImport("*")]
        public static extern int IfUp(string ifname);
    }
}
