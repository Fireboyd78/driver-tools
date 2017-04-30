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

using DSCript;

namespace System.Text
{
    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Formats and appends a string using the <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="format">The string to format.</param>
        /// <param name="args">The arguments for the formatted string.</param>
        public static StringBuilder AppendFormatEx(this StringBuilder sb, string format, params object[] args)
        {
            return sb.AppendFormat(CultureInfo.InvariantCulture, format, args);
        }

        public static StringBuilder AppendColumn(this StringBuilder @this, string colText, int colPosition, bool rightAligned = false)
        {
            return @this.Append(String.Format((rightAligned) ? $"{{0,{colPosition}}} " : $"{{0,-{colPosition}}}", $"{colText}:"));
        }

        public static StringBuilder AppendLine(this StringBuilder sb, string format, params object[] args)
        {
            return sb.AppendLine(String.Format(format, args));
        }

        public static StringBuilder AppendLine(this StringBuilder @this, object value)
        {
            return @this.AppendLine(value.ToString());
        }
        
        /// <summary>
        /// Appends a definitive amount of new lines to the end of the current <see cref="StringBuilder"/> object.
        /// </summary>
        /// <param name="count">Number of lines to be inserted</param>
        public static StringBuilder AppendLines(this StringBuilder sb, int count)
        {
            // Append all lines except for the last one
            for (int i = 1; i < count; i++)
                    sb.AppendLine();

            // StringBuilder returns AppendLine, so return the last one (for consistency)
            return sb.AppendLine();
        }
    }
}
