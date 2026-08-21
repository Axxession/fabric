using System.Runtime.InteropServices;

namespace Fabric.Hardware.BelgianEid.Middleware.Wrapper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    public struct CK_MECHANISM
    {
        public uint mechanism;

        public IntPtr pParameter;

        public uint ulParameterLen;
    }
}
