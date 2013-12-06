using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript
{
    public static partial class DSC
    {
        public static void Log(object str)
        {
        #if log
            Console.WriteLine(str);
        #else
            return;
        #endif
        }

        public static void Log(string str, params object[] arg)
        {
        #if log
            Console.WriteLine(str, arg);
        #else
            return;
        #endif
        }
    }
}
