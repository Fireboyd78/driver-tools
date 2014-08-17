using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using System.Windows.Data;

namespace DSCript
{
    public static class MagicConverter
    {
        public static byte[] GetBytes(uint magic)
        {
            return BitConverter.GetBytes(magic);
        }

        public static char[] ToCharArray(uint magic)
        {
            return new char[] {
                (char)(magic & 0x000000FF),
                (char)((magic & 0x0000FF00) >> 8),
                (char)((magic & 0x00FF0000) >> 16),
                (char)((magic & 0xFF000000) >> 24)
            };
        }

        public static string ToString(uint magic)
        {
            if (magic < 255)
                return (magic == 0) ? "Unified Packager" : magic.ToString("X");

            return new String(ToCharArray(magic));
        }   
    }

    [ValueConversion(typeof(int), typeof(string))]
    public class SpoolerMagicConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
            {
                var val = (int)value;
                return String.Format("[{0}]", (val > 255) ? Encoding.UTF8.GetString(BitConverter.GetBytes(val)).Trim('\0') : String.Format("0x{0:X}", val));
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }

}
