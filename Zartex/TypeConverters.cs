using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Design;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Zartex.Converters
{
    public class HexStringConverter : TypeConverter
    {
        private static readonly Type[] _supportedTypes = {
            typeof(int),
            typeof(uint),
            typeof(string)
        };

        private bool IsSupportedType(Type type)
        {
            return _supportedTypes.Contains(type);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (IsSupportedType(sourceType))
                return true;
            
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (IsSupportedType(destinationType))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            throw new NotImplementedException();
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return String.Format("{0:X}", value);
            }

            return null;
        }
    }
}
