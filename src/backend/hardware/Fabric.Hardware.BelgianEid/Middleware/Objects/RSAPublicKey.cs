using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of RSAPublicKey.
    /// </summary>
    public class RSAPublicKey : PublicKey
    {
        protected ByteArrayAttribute modulus_ = new(CKA.MODULUS);

        public ByteArrayAttribute Modulus => modulus_;

        protected ByteArrayAttribute publicExponent_ = new(CKA.PUBLIC_EXPONENT);

        public ByteArrayAttribute PublicExponent => publicExponent_;

        protected UIntAttribute modulusBits_ = new((uint)CKA.MODULUS_BITS);

        public UIntAttribute ModulusBits => modulusBits_;

        public RSAPublicKey()
        {
            KeyType.KeyType = CKK.RSA;
        }

        public RSAPublicKey(Session session, uint hObj)
            : base(session, hObj) { }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            return new RSAPublicKey(session, hObj);
        }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            modulus_ = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.MODULUS));

            publicExponent_ = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.PUBLIC_EXPONENT));

            modulusBits_ = ReadAttribute(session, HObj, new UIntAttribute((uint)CKA.MODULUS_BITS));
        }
    }
}
