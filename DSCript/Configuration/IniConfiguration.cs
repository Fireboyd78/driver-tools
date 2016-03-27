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
    public sealed class IniConfiguration : IDSCConfiguration
    {
        string IDSCConfiguration.this[string key]
        {
            get
            {
                return ((IDSCConfiguration)this).GetProperty(key);
            }
            set
            {
                ((IDSCConfiguration)this).SetProperty(key, value);
            }
        }

        string[] IDSCConfiguration.GetKeys()
        {
            return INIFile?.GetSections();
        }

        bool IDSCConfiguration.HasKey(string key)
        {
            return GetKeyExists(key);
        }

        string IDSCConfiguration.GetDirectory(string key)
        {
            return GetDirectory(key);
        }

        bool IDSCConfiguration.SetDirectory(string key, string value)
        {
            return SetDirectory(key, value);
        }

        string IDSCConfiguration.GetProperty(string key)
        {
            return GetSetting(key);
        }

        bool IDSCConfiguration.SetProperty(string key, string value)
        {
            return SetSetting(key, value);
        }

        private const string DirectoriesKeyName = "Directories";
        private const string SettingsKeyName = "Configuration";
        
        private string dirKey, settingsKey, sectionIdentifier;

        public string DirectoriesKey
        {
            get
            {
                if (dirKey == null)
                    dirKey = (!String.IsNullOrEmpty(SectionIdentifier)) 
                        ? String.Join(".", SectionIdentifier, DirectoriesKeyName)
                        : DirectoriesKeyName;

                return dirKey;
            }
        }

        public string SettingsKey
        {
            get
            {
                if (settingsKey == null)
                    settingsKey = (!String.IsNullOrEmpty(SectionIdentifier)) 
                        ? String.Join(".", SectionIdentifier, SettingsKeyName)
                        : SettingsKeyName;

                return settingsKey;
            }
        }

        public string SectionIdentifier
        {
            get { return sectionIdentifier; }
            set
            {
                sectionIdentifier = value;

                dirKey = null;
                settingsKey = null;
            }
        }

        public IniFile INIFile { get; private set; }
        
        public string GetDirectory(string key)
        {
            string dir = INIFile.ReadValue(DirectoriesKey, key);
            
            if (!String.IsNullOrEmpty(dir))
                dir = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dir));
            
            return dir;
        }

        public bool SetDirectory(string key, string value)
        {
            return INIFile.WriteValue(DirectoriesKey, key, value);
        }

        public string GetSetting(string key)
        {
            return INIFile.ReadValue(SettingsKey, key);
        }

        public bool GetKeyExists(string key)
        {
            var sections = INIFile.GetSections();
            
            return sections.Contains(key);
        }

        public T GetSetting<T>(string key, T defaultValue)
                where T : struct
        {
            dynamic val = null;

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
            else if (typeof(T) == typeof(Vector3D))
            {
                Vector3D v3d = Vector3D.Parse(keyVal);

                if (v3d != null)
                    val = v3d;
            }
            else
            {
                val = keyVal;
            }

            return (val != null) ? (T)Convert.ChangeType(val, typeof(T)) : defaultValue;
        }

        public bool SetSetting(string key, string value)
        {
            return INIFile.WriteValue(SettingsKey, key, value);
        }
        
        /// <summary>
        /// Appends the string to the end of the Ini file.
        /// </summary>
        /// <param name="str">The string to append.</param>
        public void AppendText(string str)
        {
            File.AppendAllText(INIFile.FileName, str, Encoding.UTF8);
        }
        
        public IniConfiguration(string iniFile, string identifier = "") : this(new IniFile(iniFile), identifier) { }
        public IniConfiguration(IniFile iniFile, string identifier = "")
        {
            INIFile = iniFile;
            SectionIdentifier = identifier;
        }
    }
}
