using System.Text;

namespace Fabric.Hardware.BelgianEid
{
    public class TlvParser
    {
        private readonly Dictionary<int, byte[]> Tags = new();

        public TlvParser(byte[] data)
        {
            int i = 0;
            int currentTag = 0;
            bool tagExpected = true;

            while (i < data.Length)
            {
                if (tagExpected)
                {
                    currentTag = data[i++];
                    tagExpected = false;
                    continue;
                }

                int tagSize = data[i++];
                byte[] tagData = new byte[tagSize];
                Array.Copy(data, i, tagData, 0, tagSize);
                Tags.Add(currentTag, tagData);
                i += tagSize;
                tagExpected = true;
            }
        }

        public String ReadString(Identity.BelgianIdentityTags tag, Encoding charset)
        {
            byte[] data = Tags[(int) tag];
            return charset.GetString(data);
        }
    }
}