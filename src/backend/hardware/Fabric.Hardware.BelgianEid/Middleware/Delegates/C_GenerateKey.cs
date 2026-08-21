using System.Runtime.InteropServices;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Delegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate CKR C_GenerateKey(uint hSession, ref CK_MECHANISM pMechanism, CK_ATTRIBUTE[] pTemplate, uint ulCount, ref uint phKey);
}
