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
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Microsoft.Win32;

namespace DSCript
{
    public sealed partial class DSC
    {
        public static readonly string IniName = "DSCript.ini";
        
        public static readonly IniConfiguration Configuration;
        public static readonly IniFile IniFile;

        public static readonly CultureInfo CurrentCulture = new CultureInfo("en-US");
        public static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "libDSC");

        public static long GetTempDirectorySize()
        {
            if (Directory.Exists(TempDirectory))
            {
                var files = Directory.GetFiles(TempDirectory);

                long size = 0;

                foreach (var file in files)
                    size += (new FileInfo(file).Length);

                return size;
            }
            else
            {
                return 0;
            }
        }

        public static IniConfiguration CreateConfiguration(string identifier)
        {
            return new IniConfiguration(IniFile, identifier);
        }

        static DSC()
        {
            var iniPath = Path.Combine(Application.StartupPath, IniName);

            if (!File.Exists(iniPath))
            {
                var sb = new StringBuilder();

                string progDir = Environment.GetFolderPath(
                    (Environment.Is64BitOperatingSystem) ? Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles);

                string steamDir = "";

                RegistryKey regKey = Registry.CurrentUser;
                regKey = regKey.OpenSubKey(@"Software\Valve\Steam");

                if (regKey != null)
                    steamDir = regKey.GetValue("SourceModInstallPath").ToString().Replace("sourcemods", "common");

                string ubiDir = Path.Combine(progDir, "Ubisoft");

                string d3Dir  = Path.Combine(progDir, "Atari", "Driv3r");
                string dplDir = Path.Combine(ubiDir, "Driver Parallel Lines");
                string dsfDir = Path.Combine(ubiDir, "Driver San Francisco");

                if (!Directory.Exists(steamDir))
                    steamDir = "";
                if (!Directory.Exists(d3Dir))
                {
                    d3Dir = "";

                    var d3Ubi = Path.Combine(ubiDir, "Driv3r");

                    if (Directory.Exists(d3Ubi))
                        d3Dir = d3Ubi;
                }
                if (!Directory.Exists(dplDir))
                {
                    dplDir = "";

                    var dplSteam = Path.Combine(steamDir, "Driver Parallel Lines");

                    if (!String.IsNullOrEmpty(steamDir) && Directory.Exists(dplSteam))
                        dplDir = dplSteam;
                }
                if (!Directory.Exists(dsfDir))
                {
                    dsfDir = "";

                    var dsfSteam = Path.Combine(steamDir, "Driver San Francisco");

                    if (!String.IsNullOrEmpty(steamDir) && Directory.Exists(dsfSteam))
                        dsfDir = dsfSteam;
                }

                sb.AppendLine(
@"# DSCript Configuration File
# Copyright (c) 2014 Mark Ludwig [CarLuver69]
# http://drivermadness.net
#
# Support/Contact -
#	Gmail: mk.ludwig1
#	Skype: CarLuver69
#
# Encoding: UTF-8
# ==========================================================================

[Global.Directories]
Driv3r={0}", d3Dir);

                if (!String.IsNullOrEmpty(dplDir))
                    sb.AppendLine("DriverPL={0}", dplDir);
                if (!String.IsNullOrEmpty(dsfDir))
                    sb.AppendLine("DriverSF={0}", dsfDir);

                sb.AppendLine();

                File.WriteAllText(iniPath, sb.ToString(), Encoding.UTF8);
            }

            IniFile = new IniFile(iniPath);
            Configuration = CreateConfiguration("Global");

            if (!Directory.Exists(TempDirectory))
            {
                Directory.CreateDirectory(TempDirectory);
            }
            else
            {
                var files = Directory.GetFiles(TempDirectory);

                if (files.Length > 0)
                {
                    DSC.Log("Cleaning temp directory...");

                    int count = 0;

                    foreach (var file in files)
                    {
                        File.Delete(file);
                        ++count;
                    }

                    DSC.Log("Cleaned out {0} files from temp directory.", count);
                }
            }
        }
    }
}
