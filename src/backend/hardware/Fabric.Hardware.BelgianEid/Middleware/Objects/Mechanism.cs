using Fabric.Hardware.BelgianEid.Middleware.Params;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of Mechanism.
    /// </summary>
    public class Mechanism
    {
        public Mechanism(CKM ckm)
        {
            this.ckm = ckm;
            parameters = Parameters.GetParameters(ckm);
        }

        public Mechanism(CK_MECHANISM ckMechanism)
        {
            ckm = (CKM)ckMechanism.mechanism;
            parameters = Parameters.GetParameters(ckm);
        }

        private CKM ckm;
        private Parameters parameters;

        public Parameters Parameters
        {
            get => parameters;
            set => parameters = value;
        }

        public CK_MECHANISM CK_MECHANISM
        {
            get
            {
                CK_MECHANISM mech = new CK_MECHANISM();
                mech.mechanism = (uint)ckm;

                parameters.ApplyToMechanism(mech);

                return mech;
            }
        }
    }
}
