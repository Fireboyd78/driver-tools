using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DSCript
{
    public static partial class DSC
    {
        private static void LogImpl(string message)
        {
#       if DEBUG
            Debug.WriteLine(message);
#       elif TRACE
            Trace.WriteLine(message);
#       else
            return;
#       endif
        }

        public static void Log(string message)
        {
            LogImpl(message);
        }
        
        public static void Log(string str, params object[] args)
        {
            LogImpl(String.Format(str, args));
        }
    }
}
