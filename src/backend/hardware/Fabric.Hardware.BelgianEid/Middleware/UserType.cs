using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware
{
    public enum UserType : uint
    {
        SO = CKU.SO,
        USER = CKU.USER,
    }
}
