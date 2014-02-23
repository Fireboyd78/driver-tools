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

namespace System
{
    public static class StringExtensions
    {
        /// <summary>
        /// Formats a delimited string using the "|" character into a split column with the specified distance.
        /// </summary>
        /// <param name="format">The delimited string that contains a "|" delimiter which defines where the split column should be.</param>
        /// <param name="column">The column where everything after the delimiter should go.</param>
        /// <returns></returns>
        public static string SplitColumn(string format, int column)
        {
            string[] cols = format.Split('|');

            if (format.Length <= 1)
                throw new Exception("The string is too short!");
            if (cols.Length > 2)
                throw new Exception("Cannot use more than one column separator!");
            else if (cols.Length <= 1)
                throw new Exception("No column separator defined!");

            if (cols[0].Length >= column || cols[0].Length >= column - 2)
            {
                char[] trimStr = cols[0].ToCharArray(0, ((column - 2) >= 0) ? column - 2 : column);

                for (int i = trimStr.Length - 1, k = 0; k < 3 && (i >= 0); i--, k++)
                    trimStr[i] = '.';

                cols[0] = new string(trimStr);
            }

            return String.Format(
                "{0}{1}",
                cols[0],
                String.Format("{0," + (((cols[0].Length >= column) ? column + 2 : column) - cols[0].Length) + "}", cols[1]));
        }

        public static string Merge(this string[] @this)
        {
            return Merge(@this, 0);
        }

        public static string Merge(this string[] @this, int index)
        {
            var builder = new StringBuilder();
            
            for (int s = index; s < @this.Length; s++)
                builder.Append(@this[s]);

            return builder.ToString();
        }
    }
}
