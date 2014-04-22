using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DSCript
{
    public sealed partial class DSC
    {
        public static void Log(object str)
        {
        #if DEBUG
            Debug.WriteLine(str);
        #else
            return;
        #endif
        }

        public static void Log(string str, params object[] args)
        {
        #if DEBUG
            Debug.WriteLine(str, args);
        #else
            return;
        #endif
        }

        public static void Log(int level, string str)
        {
        #if DEBUG
            Debugger.Log(level, "DSCript", String.Format("{0}\r\n", str));
        #else
            return;
        #endif
        }

        public static void Log(int level, string str, params object[] args)
        {
        #if DEBUG
            Log(level, String.Format(str, args));
        #else
            return;
        #endif
        }
    }
}
