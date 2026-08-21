using System.Runtime.InteropServices;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Delegates
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate CKR C_GenerateKeyPair(
        uint hSession,
        ref CK_MECHANISM pMechanism,
        CK_ATTRIBUTE[] pPublicKeyTemplate,
        uint ulPublicKeyAttributeCount,
        CK_ATTRIBUTE[] pPrivateKeyTemplate,
        uint ulPrivateKeyAttributeCount,
        ref uint phPublicKey,
        ref uint phPrivateKey
    );
}
