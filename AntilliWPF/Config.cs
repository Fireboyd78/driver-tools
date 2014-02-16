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

namespace Antilli
{
    public static class Settings
    {
        public static readonly IniConfiguration Configuration = new IniConfiguration("Antilli");
    }
}
