using System.Runtime.InteropServices;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Delegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate CKR C_CreateObject(uint hSession, CK_ATTRIBUTE[] pTemplate, uint ulCount, ref uint phObject);
}
