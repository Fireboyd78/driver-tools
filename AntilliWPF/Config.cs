﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
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

        public static readonly bool InfiniteSpin        = false;
        public static readonly int DefaultFOV           = 65;
        public static readonly double GhostOpacity      = 0.15;

        public static readonly string ExportDirectory   = Path.GetFullPath(Environment.ExpandEnvironmentVariables(@".\Resources\Export"));
        public static readonly string ModelsDirectory   = Path.GetFullPath(Environment.ExpandEnvironmentVariables(@".\Resources\Models"));
        public static readonly string TexturesDirectory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(@".\Resources\Textures"));

        static Settings()
        {
            bool infiniteSpin;
            int defaultFov;
            double ghostOpacity;

            if (Boolean.TryParse(ConfigurationManager.AppSettings["InfiniteSpin"], out infiniteSpin))
                InfiniteSpin = infiniteSpin;

            if (Int32.TryParse(ConfigurationManager.AppSettings["DefaultFOV"], out defaultFov))
                DefaultFOV = defaultFov;

            if (Double.TryParse(ConfigurationManager.AppSettings["GhostOpacity"], out ghostOpacity))
                GhostOpacity = ghostOpacity;
        }

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
