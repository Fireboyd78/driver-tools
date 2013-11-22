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
}
