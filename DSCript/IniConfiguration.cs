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
    public sealed class IniConfiguration
    {
        public const string DirectoriesKeyFormat    = "{0}.Directories";
        public const string SettingsKeyFormat       = "{0}.Configuration";

        private string dirKey, settingsKey, sectionIdentifier;

        public string DirectoriesKey
        {
            get
            {
                if (dirKey == null)
                    dirKey = String.Format(DirectoriesKeyFormat, SectionIdentifier);

                return dirKey;
            }
        }

        public string SettingsKey
        {
            get
            {
                if (dirKey == null)
                    settingsKey = String.Format(SettingsKeyFormat, SectionIdentifier);

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

            if (String.IsNullOrEmpty(dir))
                return null;

            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(dir));
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

        public void VerifyDirectory(string name, string verboseName, string callerName)
        {
            var dir = GetDirectory(name);

            if (dir == "" || !Directory.Exists(dir))
            {
                var result = MessageBox.Show(
                    String.Format(
@"{0} directory not found!

Click OK to choose a directory and automatically update your settings.", verboseName),
                    callerName, MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

                if (result == DialogResult.OK)
                {
                    var openFolder = new System.Windows.Forms.FolderBrowserDialog() {
                        RootFolder = Environment.SpecialFolder.MyComputer,
                        Description = String.Format("Please select the root directory of your {0} installation:", verboseName),
                    };

                    var fResult = openFolder.ShowDialog();

                    if (fResult == System.Windows.Forms.DialogResult.OK)
                        SetDirectory(name, openFolder.SelectedPath);
                }
                else
                {
                    MessageBox.Show(
                        String.Format("Your settings have not been updated. Please update your settings or restart {0} and try again.", callerName),
                        callerName, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    Environment.Exit(2);
                }
            }
        }

        /// <summary>
        /// Appends the string to the end of the Ini file.
        /// </summary>
        /// <param name="str">The string to append.</param>
        public void AppendText(string str)
        {
            File.AppendAllText(INIFile.FileName, str, Encoding.UTF8);
        }

        public IniConfiguration(string iniFile, string identifier) : this(new IniFile(iniFile), identifier) { }
        public IniConfiguration(IniFile iniFile, string identifier)
        {
            INIFile = iniFile;
            SectionIdentifier = identifier;
        }
    }
}
