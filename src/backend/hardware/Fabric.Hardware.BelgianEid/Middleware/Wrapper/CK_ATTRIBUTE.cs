using System.Runtime.InteropServices;

namespace Fabric.Hardware.BelgianEid.Middleware.Wrapper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    public struct CK_ATTRIBUTE
    {
        public uint type;

        public IntPtr pValue;

        public uint ulValueLen;
    }
}
