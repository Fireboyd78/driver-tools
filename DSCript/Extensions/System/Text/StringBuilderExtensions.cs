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

namespace System.Text
{
    public static class StringBuilderExtensions
    {
        // Used in AppendFormat2
        static CultureInfo culture = new CultureInfo("en-US");

        /// <summary>
        /// This extension method forces the 'en-US' culture while formatting lines. See the default AppendFormat for more info.
        /// </summary>
        /// <param name="format">A composite format string</param>
        /// <param name="args">An array of objects to format.</param>
        public static StringBuilder AppendFormat2(this StringBuilder sb, string format, params object[] args)
        {
            return sb.AppendFormat(culture, format, args);
        }

        /// <summary>
        /// Appends a definitive amount of new lines to the end of the current <see cref="StringBuilder"/> object.
        /// </summary>
        /// <param name="count">Number of lines to be inserted</param>
        public static StringBuilder AppendLines(this StringBuilder sb, int count)
        {
            // Append all lines except for the last one
            if (count > 1)
                for (int i = 0; i < count - 1; i++)
                    sb.AppendLine();

            // StringBuilder returns AppendLine, so return the last one (for consistency)
            return sb.AppendLine();
        }
    }
}
