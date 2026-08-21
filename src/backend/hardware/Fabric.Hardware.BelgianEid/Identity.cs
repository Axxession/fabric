using System.Text;

namespace Fabric.Hardware.BelgianEid
{
    /// <summary>
    /// This represents a read identity from a belgian eid card. This data has not be parsed.
    /// </summary>
    public class Identity
    {
        /// <summary>
        /// The Identity data on the card
        /// </summary>
        public byte[] IdentityData { get; }

        /// <summary>
        /// A signature of the identity data
        /// </summary>
        public byte[] IdentitySignature { get; }

        /// <summary>
        /// The certificate which was used to sign <see cref="IdentityData"/>.
        /// </summary>
        public byte[] Certificate { get; }

        /// <summary>
        /// The picture data on the card
        /// </summary>
        public byte[] Picture { get; }

        private readonly TlvParser _parser;

        public Identity(byte[] data, byte[] signature, byte[] cert, byte[] picture)
        {
            IdentityData = data;
            IdentitySignature = signature;
            Certificate = cert;
            Picture = picture;
            _parser = new TlvParser(data);
        }


        /// <summary>
        /// Returns the <see cref="IdentityData"/> in a human recognizable format. 
        /// <see cref="https://raw.githubusercontent.com/Fedict/eid-mw/master/doc/sdk/documentation/Applet%201.7%20eID%20Cards/belgian_electronic_identity_card_content_v2.8.a.pdf"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var builder = new StringBuilder();

            var i = 0;
            var tagExpected = true;
            var currentTag = 0;

            while (i < IdentityData.Length)
            {
                if (tagExpected)
                {
                    currentTag = IdentityData[i++];
                    tagExpected = false;
                    continue;
                }

                int size = IdentityData[i++];

                byte[] data = new byte[size];
                Array.Copy(IdentityData.Skip(i).ToArray(), data, size);

                builder.AppendLine($"Tag {currentTag} ({size}): {Encoding.UTF8.GetString(data)}");

                i += size;

                tagExpected = true;
            }

            return builder.ToString();
        }


        public string GetTag(BelgianIdentityTags tag) => _parser.ReadString(tag, Encoding.UTF8);


        public enum BelgianIdentityTags
        {
            Version = 0,
            CardNumber = 1,
            ChipNumber,
            CardValidityStart,
            CardValidityStop,
            Municipality,
            NationalNumber,
            Lastname,
            Firstname,
            Thirdname,
            Nationality,
            BirthLocation,
            BirthData,
            Gender,
            NobleCondition,
            DocumentType,
            SpecialStatus,
            PictureHash
        }
    }
}