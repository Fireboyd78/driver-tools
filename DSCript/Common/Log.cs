using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

#if DEBUG
    using Logger = System.Diagnostics.Debug;
#else
    using Logger = System.Diagnostics.Trace;
#endif

namespace DSCript
{
    public static partial class DSC
    {
        public static void Log(string message)
        {
            Logger.WriteLine($"[DSC] {message}");
        }
        
        public static void Log(string str, params object[] args)
        {
            Log(String.Format(str, args));
        }
    }
}
