using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Params
{
    /// <summary>
    /// Description of custom Parameters.
    /// </summary>
    public class Parameters
    {
        /// <summary>
        /// Fill CK_MECHANISM according to values of Parameters.
        /// Default implementation fills everything with zeros.
        /// </summary>
        /// <param name="ckMechanism"></param>
        public virtual void ApplyToMechanism(CK_MECHANISM ckMechanism)
        {
            ckMechanism.pParameter = IntPtr.Zero;
            ckMechanism.ulParameterLen = 0;
        }

        /// <summary>
        /// Obtain a parameter class here.
        /// </summary>
        /// <param name="ckm"></param>
        /// <returns></returns>
        public static Parameters GetParameters(CKM ckm)
        {
            Parameters lParameters = new Parameters();
            return lParameters;
        }
    }
}
