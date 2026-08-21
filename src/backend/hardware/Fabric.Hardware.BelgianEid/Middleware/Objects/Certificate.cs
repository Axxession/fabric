#nullable disable
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware.Objects
{
    /// <summary>
    /// Description of Certificate.
    /// </summary>
    public class Certificate : Storage
    {
        protected CertificateTypeAttribute certificateType_ = new();

        public CertificateTypeAttribute CertificateType => certificateType_;

        protected BooleanAttribute trusted_ = new(CKA.TRUSTED);

        public BooleanAttribute Trusted => trusted_;

        public Certificate()
        {
            Class.ObjectType = CKO.CERTIFICATE;
        }

        public Certificate(Session session, uint hObj)
            : base(session, hObj) { }

        public override void ReadAttributes(Session session)
        {
            base.ReadAttributes(session);

            trusted_ = ReadAttribute(session, HObj, new BooleanAttribute(CKA.TRUSTED));

            certificateType_ = ReadAttribute(session, HObj, new CertificateTypeAttribute());
        }

        public static new P11Object GetInstance(Session session, uint hObj)
        {
            if (session == null)
                throw new NullReferenceException("Argument \"session\" must not be null.");

            CertificateTypeAttribute classAtr = ReadAttribute(session, hObj, new CertificateTypeAttribute());

            switch (classAtr.CertificateType)
            {
                case CKC.WTLS:
                case CKC.X_509:
                    return X509PublicKeyCertificate.GetInstance(session, hObj);
                case CKC.X_509_ATTR_CERT:
                case CKC.VENDOR_DEFINED:
                default:
                    break;
            }

            return null;
        }
    }
}
