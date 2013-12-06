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
    /* Sources:
       - http://stackoverflow.com/a/1470182
       - http://stackoverflow.com/a/1592899
       - http://www.codeproject.com/Articles/1966/An-INI-file-handling-class-using-C */

    public static class NativeMethods
    {
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd,
            int Msg,
            int wParam,
            int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        /* INIFile */
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WritePrivateProfileString(string section,
            string key,
            string val,
            string filePath);

        [DllImport("kernel32.dll")]
        public static extern int GetPrivateProfileString(string section,
            string key,
            string def,
            [In, Out] char[] retVal,
            int size,
            string filePath);
    }
}