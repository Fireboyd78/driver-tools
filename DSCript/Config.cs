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
                if (isDirectory)
                {
                    string dir = GetDirectory(key);

                    if (dir.StartsWith(@".\", StringComparison.OrdinalIgnoreCase))
                        return String.Format(@"{0}\{1}", Application.StartupPath, dir.Substring(2));
                    else
                        return dir;
                }
                else
                {
                    return GetSetting(key);
                }
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
            return DSC.INIFile.ReadValue(DirectoriesKey, key);
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
        public static IniConfiguration Configuration { get; private set; }
        
        internal static INIFile INIFile { get; private set; }

        public static bool IsConfigLoaded
        {
            get { return INIFile != null; }
        }

        static DSC()
        {
            INIFile = new INIFile(String.Format("{0}\\DSCript.ini", Application.StartupPath));
            Configuration = new IniConfiguration("Global");
        }
    }
}
