using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of GostPublicKey.
    /// </summary>
    public class GostPublicKey : PublicKey
    {
        /// <summary>
        /// Params.
        /// </summary>
        protected ByteArrayAttribute params_ = new((uint)CKA.GOSTR3410PARAMS);

        public ByteArrayAttribute Params => params_;

        public GostPublicKey()
        {
            KeyType.KeyType = CKK.GOST;
            params_.Value = PKCS11Constants.SC_PARAMSET_GOSTR3410_A;
        }

        public GostPublicKey(Session session, uint hObj)
            : base(session, hObj)
        {
            params_.Value = PKCS11Constants.SC_PARAMSET_GOSTR3410_A;
        }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            return new GostPublicKey(session, hObj);
        }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            params_ = ReadAttribute(session, HObj, params_);
        }
    }
}
