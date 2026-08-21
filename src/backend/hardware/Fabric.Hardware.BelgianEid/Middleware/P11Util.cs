#nullable disable
using System.Text;
using Fabric.Hardware.BelgianEid.Middleware.Objects;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware
{
    /// <summary>
    /// Description of P11Util.
    /// </summary>
    internal static class P11Util
    {
        public static String ConvertToUtf8String(byte[] val)
        {
            return Encoding.UTF8.GetString(val);
        }

        public static String ConvertToASCIIString(byte[] val)
        {
            return Encoding.ASCII.GetString(val);
        }

        public static CK_DATE ConvertToCK_DATE(DateTime dateTime)
        {
            CK_DATE d = new CK_DATE();

            d.year = Encoding.ASCII.GetBytes(ConvertIntToString(dateTime.Year, 4));
            d.month = Encoding.ASCII.GetBytes(ConvertIntToString(dateTime.Month, 2));
            d.day = Encoding.ASCII.GetBytes(ConvertIntToString(dateTime.Day, 2));

            return d;
        }

        public static DateTime ConvertToDateTime(CK_DATE ckDate)
        {
            int _year = Int32.Parse(Encoding.ASCII.GetString(ckDate.year));
            int _month = Int32.Parse(Encoding.ASCII.GetString(ckDate.month));
            int _day = Int32.Parse(Encoding.ASCII.GetString(ckDate.day));
            return new DateTime(_year, _month, _day);
        }

        public static string ConvertIntToString(int val, int strSize)
        {
            String str = new String('0', strSize) + val;
            return str.Substring(str.Length - strSize, strSize);
        }

        public static DateTime ConvertToDateTimeYYYYMMDDhhmmssxx(String time)
        {
            try
            {
                return new DateTime(
                    Int32.Parse(time.Substring(0, 4)),
                    Int32.Parse(time.Substring(4, 2)),
                    Int32.Parse(time.Substring(6, 2)),
                    Int32.Parse(time.Substring(8, 2)),
                    Int32.Parse(time.Substring(10, 2)),
                    Int32.Parse(time.Substring(12, 2)),
                    Int32.Parse(time.Substring(14, 2)),
                    DateTimeKind.Utc
                );
            }
            catch
            {
                return new DateTime();
            }
        }

        public static CK_ATTRIBUTE[] ConvertToCK_ATTRIBUTEs(P11Attribute[] attrs)
        {
            if (attrs == null || attrs.Length == 0)
                return null;

            CK_ATTRIBUTE[] ckAttrs = new CK_ATTRIBUTE[attrs.Length];

            for (int i = 0; i < attrs.Length; i++)
                ckAttrs[i] = attrs[i].CK_ATTRIBUTE;

            return ckAttrs;
        }
    }
}
