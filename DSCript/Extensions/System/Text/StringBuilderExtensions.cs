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
        /// <summary>
        /// Appends a definitive amount of new lines to the end of the current <see cref="StringBuilder"/> object.
        /// </summary>
        /// <param name="count">Number of lines to be inserted</param>
        public static void AppendLines(this StringBuilder sb, int count)
        {
            for (int i = 0; i < count; i++)
                sb.AppendLine();
        }
    }
}
