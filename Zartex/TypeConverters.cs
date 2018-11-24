using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Design;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using DSCript;

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
            if ((value != null) && (value is string))
            {
                var str = value as string;
                var style = NumberStyles.Any;

                if (str.StartsWith("0x"))
                {
                    str = str.Substring(2);
                    style = NumberStyles.HexNumber;
                }

                var result = int.Parse(str, style);

                if ((result & 0xFF) == result)
                    return (byte)result;
                if ((result & 0xFFFF) == result)
                    return (short)result;

                return result;

            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
                return String.Format("0x{0:X}", value);

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public class VectorTypeConverter : TypeConverter
    {
        private static readonly Type[] _supportedTypes = {
            typeof(string),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
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
            if ((value != null) && (value is string))
            {
                var str = value as string;
                var vals = str.Split(',');

                if (vals.Length == 2)
                    return new Vector2(float.Parse(vals[0]), float.Parse(vals[1]));
                if (vals.Length == 3)
                    return new Vector3(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]));
                if (vals.Length == 4)
                    return new Vector4(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]), float.Parse(vals[3]));
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
                return value.ToString();

            return null;
        }
    }
}
