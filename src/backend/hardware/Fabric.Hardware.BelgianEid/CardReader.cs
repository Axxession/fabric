#nullable disable
using System.Text;
using Fabric.Hardware.BelgianEid.Middleware;
using Fabric.Hardware.BelgianEid.Middleware.Objects;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid
{
    internal class CardReader : IDisposable
    {
        private readonly Session _session;

        public CardReader(Session session)
        {
            _session = session;
        }

        private byte[] GetCertificateFile(String Certificatename)
        {
            byte[] value = null;

            // "The label attribute of the objects should equal ..."
            ByteArrayAttribute fileLabel = new ByteArrayAttribute(CKA.LABEL);
            ObjectClassAttribute certificateAttribute = new ObjectClassAttribute(CKO.CERTIFICATE);
            fileLabel.Value = Encoding.UTF8.GetBytes(Certificatename);
            _session.FindObjectsInit(certificateAttribute, fileLabel);
            P11Object[] foundObjects = _session.FindObjects(1);
            if (foundObjects.Length != 0)
            {
                X509PublicKeyCertificate cert = foundObjects[0] as X509PublicKeyCertificate;
                value = cert.Value.Value;
            }

            _session.FindObjectsFinal();

            return value;
        }

        public string ReadProperty(string label)
        {
            return ReadProperty(
                label,
                x =>
                {
                    var data = x as Data;

                    return data == null ? null : Encoding.UTF8.GetString(data.Value.Value);
                }
            );
        }

        public byte[] ReadFile(string label)
        {
            ByteArrayAttribute classAttribute = new ByteArrayAttribute(CKA.CLASS);
            classAttribute.Value = BitConverter.GetBytes((uint)CKO.DATA);

            ByteArrayAttribute labelAttribute = new ByteArrayAttribute(CKA.LABEL);
            labelAttribute.Value = Encoding.UTF8.GetBytes(label);

            ByteArrayAttribute fileLabel = new ByteArrayAttribute(CKA.LABEL);
            fileLabel.Value = Encoding.UTF8.GetBytes(label);
            ByteArrayAttribute fileData = new ByteArrayAttribute(CKA.CLASS);
            fileData.Value = BitConverter.GetBytes((uint)CKO.DATA);
            _session.FindObjectsInit(fileLabel, fileData);
            P11Object result = _session.FindObjects(50).FirstOrDefault();

            var data = (result as Data)?.Value?.Value;

            _session.FindObjectsFinal();

            return data;
        }

        private T ReadProperty<T>(string label, Func<P11Object, T> reader)
        {
            try
            {
                var classAttribute = new ByteArrayAttribute(CKA.CLASS) { Value = BitConverter.GetBytes((uint)CKO.DATA) };

                var labelAttribute = new ByteArrayAttribute(CKA.LABEL) { Value = Encoding.UTF8.GetBytes(label) };

                _session.FindObjectsInit(classAttribute, labelAttribute);
                var result = _session.FindObjects(50).FirstOrDefault();

                if (result == null)
                    return default;

                var data = reader(result);
                _session.FindObjectsFinal();

                return data;
            }
            catch (Exception e)
            {
                _session.Dispose();
                throw new Exception("Failed to read property", e);
            }
        }

        public void Dispose()
        {
            _session.Dispose();
        }
    }
}
