using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of RSAPrivateKey.
    /// </summary>
    public class RSAPrivateKey : PrivateKey
    {
        ByteArrayAttribute modulus = new(CKA.MODULUS),
            publicExponent = new(CKA.PUBLIC_EXPONENT),
            privateExponent = new(CKA.PRIVATE_EXPONENT),
            prime1 = new(CKA.PRIME_1),
            prime2 = new(CKA.PRIME_2),
            exponent1 = new(CKA.EXPONENT_1),
            exponent2 = new(CKA.EXPONENT_2),
            coefficient = new(CKA.COEFFICIENT);

        public ByteArrayAttribute Coefficient => coefficient;

        public ByteArrayAttribute Exponent2 => exponent2;

        public ByteArrayAttribute Exponent1 => exponent1;

        public ByteArrayAttribute Prime2 => prime2;

        public ByteArrayAttribute Prime1 => prime1;

        public ByteArrayAttribute PrivateExponent => privateExponent;

        public ByteArrayAttribute PublicExponent => publicExponent;

        public ByteArrayAttribute Modulus => modulus;

        public RSAPrivateKey()
        {
            KeyType.KeyType = CKK.RSA;
        }

        public RSAPrivateKey(Session session, uint hObj)
            : base(session, hObj) { }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            return new RSAPrivateKey(session, hObj);
        }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            modulus = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.MODULUS));
            publicExponent = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.PUBLIC_EXPONENT));
            privateExponent = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.PRIVATE_EXPONENT));
            prime1 = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.PRIME_1));
            prime2 = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.PRIME_2));
            exponent1 = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.EXPONENT_1));
            exponent2 = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.EXPONENT_2));
            coefficient = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.COEFFICIENT));
        }
    }
}
