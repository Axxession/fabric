using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of ECPublicKey.
    /// </summary>
    public class ECPublicKey : PublicKey
    {
        protected ByteArrayAttribute ecparams_ = new(CKA.EC_PARAMS);

        public ByteArrayAttribute ECParams => ecparams_;

        protected ByteArrayAttribute ecpoint_ = new(CKA.EC_POINT);

        public ByteArrayAttribute ECPoint => ecpoint_;

        public ECPublicKey()
        {
            KeyType.KeyType = CKK.EC;
        }

        public ECPublicKey(Session session, uint hObj)
            : base(session, hObj) { }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            return new ECPublicKey(session, hObj);
        }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            ecparams_ = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.EC_PARAMS));

            ecpoint_ = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.EC_POINT));
        }
    }
}
