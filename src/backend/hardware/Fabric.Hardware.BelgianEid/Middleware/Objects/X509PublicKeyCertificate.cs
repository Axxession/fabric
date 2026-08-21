using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of X509PublicKeyCertificate.
    /// </summary>
    public class X509PublicKeyCertificate : Certificate
    {
        protected ByteArrayAttribute subject_ = new(CKA.SUBJECT);

        public ByteArrayAttribute Subject => subject_;

        protected ByteArrayAttribute id_ = new(CKA.ID);

        public ByteArrayAttribute Id => id_;

        protected ByteArrayAttribute issuer_ = new(CKA.ISSUER);

        public ByteArrayAttribute Issuer => issuer_;

        protected ByteArrayAttribute serialNumber_ = new(CKA.SERIAL_NUMBER);

        public ByteArrayAttribute SerialNumber => serialNumber_;

        protected ByteArrayAttribute value_ = new(CKA.VALUE);

        public ByteArrayAttribute Value => value_;

        public X509PublicKeyCertificate()
        {
            CertificateType.CertificateType = CKC.X_509;
        }

        public X509PublicKeyCertificate(Session session, uint hObj)
            : base(session, hObj) { }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            return new X509PublicKeyCertificate(session, hObj);
        }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            subject_ = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.SUBJECT));

            id_ = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.ID));

            issuer_ = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.ISSUER));

            serialNumber_ = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.SERIAL_NUMBER));

            value_ = ReadAttribute(session, HObj, new ByteArrayAttribute(CKA.VALUE));
        }
    }
}
