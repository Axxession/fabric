using System.Runtime.InteropServices;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Delegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate CKR C_DigestFinal(uint hSession, byte[] pDigest, ref uint pulDigestLen);
}
