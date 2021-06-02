
using System.Runtime.InteropServices;

namespace SensorsSDK.Win32Utilities
{
    static class Win32Utilities
    {
        [DllImport("Win32Utilities", CallingConvention = CallingConvention.StdCall)]
        public static extern long Query100NanoPerformanceCounter();

        [DllImport("Win32Utilities", CallingConvention = CallingConvention.StdCall)]
        public static extern void FailFast();

        [DllImport("Win32Utilities", CallingConvention = CallingConvention.StdCall)]
        public static extern int GoFast();

        [DllImport("Win32Utilities", CallingConvention = CallingConvention.StdCall)]
        public static extern int GoSlow();
    }
}
