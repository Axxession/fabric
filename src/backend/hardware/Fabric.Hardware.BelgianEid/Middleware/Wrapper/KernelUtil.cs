using System.Runtime.InteropServices;

namespace Fabric.Hardware.BelgianEid.Middleware.Wrapper
{
    /// <summary>
    /// Description of LibraryManager.
    /// </summary>
    public static class KernelUtil
    {
        #region KernelCalls

        [DllImport("kernel32")]
        public static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("kernel32")]
        public static extern bool FreeLibrary(IntPtr hLibModule);

        [DllImport("kernel32", CharSet = CharSet.Ansi)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        #endregion
    }
}
