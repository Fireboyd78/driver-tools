using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows.Data;

namespace DSCript.Spoolers
{
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
