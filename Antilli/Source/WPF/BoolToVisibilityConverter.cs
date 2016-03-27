using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Data;

namespace Antilli
{
    [ValueConversion(typeof(Boolean), typeof(Visibility))]
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
                throw new ArgumentException("Invalid target type, cannot use converter.", "targetType");

            if (value is bool)
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                throw new Exception("Cannot convert non-boolean value to a visibility!");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new ArgumentException("Invalid target type, cannot use converter.", "targetType");

            if (value is Visibility)
            {
                return ((Visibility)value == Visibility.Visible);
            }
            else
            {
                throw new Exception("Cannot convert non-Visibility value to a boolean!");
            }
        }
    }
}
