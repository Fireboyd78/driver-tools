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
                where T : IComparable, IConvertible
        {
            object val = null;

            string keyVal = GetSetting(key);

            if (typeof(T) == typeof(bool))
            {
                if (keyVal != String.Empty)
                {
                    bool bVal = bool.Parse((GetSetting<int>(key, 0) == 1).ToString());

                    if (bVal != false)
                        val = bVal;
                }
            }
            else
            {
                if (keyVal != String.Empty)
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
    }
}
