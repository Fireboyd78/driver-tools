using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DSCript
{
    public static class StringHelper
    {
        public static CultureInfo DefaultNumberCulture = CultureInfo.InvariantCulture;

        public static byte ToByte(string value)
        {
            return Convert.ToByte(value, DefaultNumberCulture);
        }

        public static sbyte ToSByte(string value)
        {
            return Convert.ToSByte(value, DefaultNumberCulture);
        }

        public static short ToInt16(string value)
        {
            return Convert.ToInt16(value, DefaultNumberCulture);
        }

        public static int ToInt32(string value)
        {
            return Convert.ToInt32(value, DefaultNumberCulture);
        }

        public static long ToInt64(string value)
        {
            return Convert.ToInt64(value, DefaultNumberCulture);
        }

        public static ushort ToUInt16(string value)
        {
            return Convert.ToUInt16(value, DefaultNumberCulture);
        }

        public static uint ToUInt32(string value)
        {
            return Convert.ToUInt32(value, DefaultNumberCulture);
        }

        public static ulong ToUInt64(string value)
        {
            return Convert.ToUInt64(value, DefaultNumberCulture);
        }

        public static double ToDouble(string value)
        {
            return Convert.ToDouble(value, DefaultNumberCulture);
        }

        public static float ToSingle(string value)
        {
            return Convert.ToSingle(value, DefaultNumberCulture);
        }
    }
}
