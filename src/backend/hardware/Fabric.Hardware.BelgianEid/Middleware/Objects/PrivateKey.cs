#nullable disable
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of PrivateKey.
    /// </summary>
    public class PrivateKey : Key
    {
        ByteArrayAttribute subject = new(CKA.SUBJECT);

        public ByteArrayAttribute Subject => subject;

        BooleanAttribute sensitive = new(CKA.SENSITIVE),
            decrypt = new(CKA.DECRYPT),
            sign = new(CKA.SIGN),
            signRecover = new(CKA.SIGN_RECOVER),
            unWrap = new(CKA.UNWRAP),
            extractable = new(CKA.SENSITIVE),
            alwaysSensitive = new(CKA.ALWAYS_SENSITIVE),
            neverExtractable = new(CKA.NEVER_EXTRACTABLE),
            wrapWithTrusted = new(CKA.WRAP_WITH_TRUSTED),
            alwaysAuthenticate = new(CKA.ALWAYS_AUTHENTICATE);

        public BooleanAttribute AlwaysAuthenticate => alwaysAuthenticate;

        public BooleanAttribute WrapWithTrusted => wrapWithTrusted;

        public BooleanAttribute NeverExtractable => neverExtractable;

        public BooleanAttribute AlwaysSensitive => alwaysSensitive;

        public BooleanAttribute Extractable => extractable;

        public BooleanAttribute UnWrap => unWrap;

        public BooleanAttribute SignRecover => signRecover;

        public BooleanAttribute Sign => sign;

        public BooleanAttribute Decrypt => decrypt;

        public BooleanAttribute Sensitive => sensitive;

        //TODO: CKA_UNWRAP_TEMPLATE

        public PrivateKey() { }

        public PrivateKey(Session session, uint hObj)
            : base(session, hObj) { }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            subject = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.SUBJECT));
            sensitive = ReadAttribute(session, HObj, new BooleanAttribute(CKA.SENSITIVE));
            decrypt = ReadAttribute(session, HObj, new BooleanAttribute(CKA.DECRYPT));
            sign = ReadAttribute(session, HObj, new BooleanAttribute(CKA.SIGN));
            signRecover = ReadAttribute(session, HObj, new BooleanAttribute(CKA.SIGN_RECOVER));
            unWrap = ReadAttribute(session, HObj, new BooleanAttribute(CKA.UNWRAP));
            extractable = ReadAttribute(session, HObj, new BooleanAttribute(CKA.EXTRACTABLE));
            alwaysSensitive = ReadAttribute(session, HObj, new BooleanAttribute(CKA.ALWAYS_SENSITIVE));
            neverExtractable = ReadAttribute(session, HObj, new BooleanAttribute(CKA.NEVER_EXTRACTABLE));
            wrapWithTrusted = ReadAttribute(session, HObj, new BooleanAttribute(CKA.WRAP_WITH_TRUSTED));
            alwaysAuthenticate = ReadAttribute(session, HObj, new BooleanAttribute(CKA.ALWAYS_AUTHENTICATE));
        }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            KeyTypeAttribute keyAttr = ReadAttribute(session, hObj, new KeyTypeAttribute());

            switch (keyAttr.KeyType)
            {
                case CKK.RSA:
                    return RSAPrivateKey.GetInstance(session, hObj);
                case CKK.EC:
                    return ECPrivateKey.GetInstance(session, hObj);
                case CKK.GOST:
                    return GostPrivateKey.GetInstance(session, hObj);
                default:
                    return null;
            }
        }
    }
}
