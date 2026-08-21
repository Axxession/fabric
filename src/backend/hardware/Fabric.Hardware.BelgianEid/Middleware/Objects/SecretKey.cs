using System.Text;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of SecretKey.
    /// </summary>
    public class SecretKey : Key
    {
        ByteArrayAttribute subject = new(CKA.SUBJECT),
            checkValue = new(CKA.CHECK_VALUE);

        BooleanAttribute sensitive = new(CKA.SENSITIVE),
            decrypt = new(CKA.DECRYPT),
            encrypt = new(CKA.ENCRYPT),
            sign = new(CKA.SIGN),
            verify = new(CKA.VERIFY),
            wrap = new(CKA.WRAP),
            unwrap = new(CKA.UNWRAP),
            extractable = new(CKA.EXTRACTABLE),
            alwaysSensitive = new(CKA.ALWAYS_SENSITIVE),
            neverExtractable = new(CKA.NEVER_EXTRACTABLE),
            wrapWithTrusted = new(CKA.WRAP_WITH_TRUSTED),
            trusted = new(CKA.TRUSTED);

        public ByteArrayAttribute Subject => subject;

        public ByteArrayAttribute CheckValue => checkValue;

        public BooleanAttribute Sensitive => sensitive;

        public BooleanAttribute Decrypt => decrypt;

        public BooleanAttribute Encrypt => encrypt;

        public BooleanAttribute Sign => sign;

        public BooleanAttribute Verify => verify;

        public BooleanAttribute Wrap => wrap;

        public BooleanAttribute Unwrap => unwrap;

        public BooleanAttribute Extractable => extractable;

        public BooleanAttribute AlwaysSensitive => alwaysSensitive;

        public BooleanAttribute NeverExtractable => neverExtractable;

        public BooleanAttribute WrapWithTrusted => wrapWithTrusted;

        public BooleanAttribute Trusted => trusted;

        public SecretKey()
        {
            Class.ObjectType = CKO.SECRET_KEY;
        }

        public SecretKey(Session session, uint hObj)
            : base(session, hObj) { }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            KeyTypeAttribute keyType = ReadAttribute(session, hObj, new KeyTypeAttribute());

            switch (keyType.KeyType)
            {
                case CKK.DES:
                    return DesSecretKey.GetInstance(session, hObj);
                case CKK.DES2:
                    return Des2SecretKey.GetInstance(session, hObj);
                case CKK.DES3:
                    return Des3SecretKey.GetInstance(session, hObj);
                default:
                    return new SecretKey(session, hObj); // Return at least some info about the secret key.
            }
        }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            subject = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.SUBJECT));
            checkValue = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.CHECK_VALUE));

            sensitive = ReadAttribute(session, HObj, new BooleanAttribute(CKA.SENSITIVE));
            decrypt = ReadAttribute(session, HObj, new BooleanAttribute(CKA.DECRYPT));
            encrypt = ReadAttribute(session, HObj, new BooleanAttribute(CKA.ENCRYPT));
            sign = ReadAttribute(session, HObj, new BooleanAttribute(CKA.SIGN));
            verify = ReadAttribute(session, HObj, new BooleanAttribute(CKA.VERIFY));
            wrap = ReadAttribute(session, HObj, new BooleanAttribute(CKA.WRAP));
            unwrap = ReadAttribute(session, HObj, new BooleanAttribute(CKA.UNWRAP));
            extractable = ReadAttribute(session, HObj, new BooleanAttribute(CKA.EXTRACTABLE));
            alwaysSensitive = ReadAttribute(session, HObj, new BooleanAttribute(CKA.ALWAYS_SENSITIVE));
            neverExtractable = ReadAttribute(session, HObj, new BooleanAttribute(CKA.NEVER_EXTRACTABLE));
            wrapWithTrusted = ReadAttribute(session, HObj, new BooleanAttribute(CKA.WRAP_WITH_TRUSTED));
            trusted = ReadAttribute(session, HObj, new BooleanAttribute(CKA.TRUSTED));
        }

        public override string ToString()
        {
            if (subject.Value != null)
                return "SecretKey object: " + Encoding.ASCII.GetString(subject.Value);
            return "SecretKey: " + base.ToString();
        }
    }
}
