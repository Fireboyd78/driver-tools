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

namespace DSCript
{
    public class IniConfiguration
    {
        public string DirectoriesKey { get; private set; }
        public string SettingsKey { get; private set; }

        public object this[string key, bool isDirectory = false]
        {
            get
            {
                return (!isDirectory) ? GetSetting(key) : GetDirectory(key);
            }
            set
            {
                if (!isDirectory)
                    SetSetting(key, value.ToString());
                else
                    SetDirectory(key, value.ToString());
            }
        }

        public string GetDirectory(string key)
        {
            string dir = DSC.INIFile.ReadValue(DirectoriesKey, key);
            
            return (!String.IsNullOrEmpty(dir)) ? Path.GetFullPath(Environment.ExpandEnvironmentVariables(dir)) : dir;
        }

        public bool SetDirectory(string key, string value)
        {
            return DSC.INIFile.WriteValue(DirectoriesKey, key, value);
        }

        public string GetSetting(string key)
        {
            return DSC.INIFile.ReadValue(SettingsKey, key);
        }

        public T GetSetting<T>(string key, T defaultValue)
                where T : struct
        {
            object val = null;

            string keyVal = GetSetting(key);

            if (string.IsNullOrEmpty(keyVal))
                return defaultValue;

            if (typeof(T) == typeof(bool))
            {
                bool bVal = (GetSetting<int>(key, 0) == 1);

                if (bVal != false)
                    val = bVal;
            }
            else if (typeof(T) == typeof(Point3D))
            {
                Point3D p3d = Point3D.Parse(keyVal);

                if (p3d != null)
                    val = p3d;
            }
            else
            {
                val = keyVal;
            }

            return (val != null) ? (T)Convert.ChangeType(val, typeof(T)) : defaultValue;
        }

        public bool SetSetting(string key, string value)
        {
            return DSC.INIFile.WriteValue(SettingsKey, key, value);
        }

        public IniConfiguration(string identifier)
        {
            DirectoriesKey = String.Format("{0}.Directories", identifier);
            SettingsKey = String.Format("{0}.Configuration", identifier);
        }
    }

    public static partial class DSC
    {
        internal static readonly INIFile INIFile = new INIFile("DSCript.ini", Application.StartupPath);

        public static readonly IniConfiguration Configuration = new IniConfiguration("Global");
        public static readonly CultureInfo CurrentCulture = new CultureInfo("en-US");
    }
}
