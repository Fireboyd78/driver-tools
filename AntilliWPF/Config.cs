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
        public static readonly IniConfiguration Configuration = DSC.CreateConfiguration("Antilli");

        public static void Verify()
        {
            if (!Configuration.GetKeyExists(Configuration.SettingsKey))
            {
                Configuration.AppendText(
@"[Antilli.Configuration]
#Viewport configuration
DefaultFOV=65
GhostOpacity=0.15
InfiniteSpin=1

[Antilli.Directories]
Export=.\Resources\Exported
Models=.\Resources\Models
Textures=.\Resources\Textures
");
            }
        }
    }
}
