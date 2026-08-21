using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    public class DesSecretKey : SecretKey
    {
        ByteArrayAttribute value_ = new(CKA.VALUE);

        public ByteArrayAttribute Value => value_;

        public DesSecretKey()
        {
            KeyType.KeyType = CKK.DES;
        }

        public DesSecretKey(Session session, uint hObj)
            : base(session, hObj) { }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            value_ = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.VALUE));
        }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            return new DesSecretKey(session, hObj);
        }
    }
}
