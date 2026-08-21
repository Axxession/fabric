using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware
{
    /// <summary>
    /// Description of TokenException.
    /// </summary>
    public class TokenException : Exception
    {
        public TokenException() { }

        public TokenException(CKR errorCode)
            : base(errorCode.ToString())
        {
            this.errorCode = errorCode;
        }

        CKR errorCode;

        public CKR ErrorCode => errorCode;
    }
}
