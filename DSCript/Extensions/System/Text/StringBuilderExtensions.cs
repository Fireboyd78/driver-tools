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
        /// This extension method forces the 'en-US' culture while formatting lines. See the default AppendFormat for more info.
        /// </summary>
        /// <param name="format">A composite format string</param>
        /// <param name="args">An array of objects to format.</param>
        public static StringBuilder AppendFormatEx(this StringBuilder sb, string format, params object[] args)
        {
            return sb.AppendFormat(DSC.CurrentCulture, format, args);
        }

        public static StringBuilder AppendLine(this StringBuilder sb, string format, params object[] args)
        {
            return sb.AppendFormat(format, args).AppendLine();
        }

        public static StringBuilder AppendLine(this StringBuilder @this, object value)
        {
            return @this.AppendLine(value.ToString());
        }

        public static StringBuilder AppendColumn(this StringBuilder @this, string colText, int colPosition, bool rightAligned = false)
        {
            return @this.AppendFormat("{0," + ((!rightAligned) ? "-" : "") + colPosition + "}" + ((rightAligned) ? " " : ""), (!colText.EndsWith(":")) ? colText + ":" : colText);
        }

        public static StringBuilder AppendLineEx(this StringBuilder sb, string format, params object[] args)
        {
            return sb.AppendFormatEx(format, args).AppendLine();
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
