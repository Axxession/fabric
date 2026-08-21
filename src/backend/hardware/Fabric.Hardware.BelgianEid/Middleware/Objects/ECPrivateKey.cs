using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of ECPrivateKey.
    /// </summary>
    ///
    public class ECPrivateKey : PrivateKey
    {
        //CKA_EC_PARAMS with { 0x06, 0x05, 0x2b, 0x81, 0x04, 0x00, 0x22 }

        ByteArrayAttribute ecparams = new(CKA.EC_PARAMS);

        public ByteArrayAttribute ECParams => ecparams;

        public ECPrivateKey()
        {
            KeyType.KeyType = CKK.EC;
        }

        public ECPrivateKey(Session session, uint hObj)
            : base(session, hObj) { }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            return new ECPrivateKey(session, hObj);
        }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            ecparams = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.EC_PARAMS));
        }
    }
}
