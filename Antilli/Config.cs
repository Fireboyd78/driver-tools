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
using System.Windows.Forms;

using DSCript;

namespace Antilli
{
    public static class Settings
    {
        public static DSCriptConfiguration Configuration { get; private set; }

        static Settings()
        {
            Configuration = new DSCriptConfiguration("Antilli");
        }
    }
}
