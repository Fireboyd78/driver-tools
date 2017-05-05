using System;
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
        public static readonly bool InfiniteSpin        = false;
        public static readonly int DefaultFOV           = 65;
        public static readonly double GhostOpacity      = 0.15;

        public static readonly string ExportDirectory   = Path.GetFullPath(@".\Resources\Export");
        public static readonly string ModelsDirectory   = Path.GetFullPath(@".\Resources\Models");
        public static readonly string TexturesDirectory = Path.GetFullPath(@".\Resources\Textures");

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

        private static void VerifyDirectory(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public static void Verify()
        {
            VerifyDirectory(ExportDirectory);
            VerifyDirectory(ModelsDirectory);
            VerifyDirectory(TexturesDirectory);
        }
    }
}
