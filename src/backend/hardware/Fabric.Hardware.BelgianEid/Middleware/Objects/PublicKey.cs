#nullable disable
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of PublicKey.
    /// </summary>
    public abstract class PublicKey : Key
    {
        public PublicKey()
        {
            Class.ObjectType = CKO.PUBLIC_KEY;
        }

        public PublicKey(Session session, uint hObj)
            : base(session, hObj) { }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            KeyTypeAttribute keyType = ReadAttribute(session, hObj, new KeyTypeAttribute());

            switch (keyType.KeyType)
            {
                case CKK.RSA:
                    return RSAPublicKey.GetInstance(session, hObj);
                case CKK.EC:
                    return ECPublicKey.GetInstance(session, hObj);
                case CKK.GOST:
                    return GostPublicKey.GetInstance(session, hObj);
                default:
                    return null;
            }
        }

        ByteArrayAttribute subject = new(CKA.SUBJECT);

        BooleanAttribute encrypt = new(CKA.ENCRYPT),
            verify = new(CKA.VERIFY),
            verifyRecover = new(CKA.VERIFY_RECOVER),
            wrap = new(CKA.WRAP),
            trusted = new(CKA.TRUSTED);

        public ByteArrayAttribute Subject => subject;

        public BooleanAttribute Encrypt => encrypt;

        public BooleanAttribute Verify => verify;

        public BooleanAttribute VerifyRecover => verifyRecover;

        public BooleanAttribute Wrap => wrap;

        public BooleanAttribute Trusted => trusted;

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            subject = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.SUBJECT));
            encrypt = ReadAttribute(session, HObj, new BooleanAttribute(CKA.ENCRYPT));
            verify = ReadAttribute(session, HObj, new BooleanAttribute(CKA.VERIFY));
            verifyRecover = ReadAttribute(session, HObj, new BooleanAttribute(CKA.VERIFY_RECOVER));
            wrap = ReadAttribute(session, HObj, new BooleanAttribute(CKA.WRAP));
            trusted = ReadAttribute(session, HObj, new BooleanAttribute(CKA.TRUSTED));
        }
    }
}
