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
    public delegate void ProgressUpdateEventHandler(object sender, ProgressUpdateEventArgs args);

    public sealed class ProgressUpdateEventArgs : EventArgs
    {
        public static readonly new ProgressUpdateEventArgs Empty = new ProgressUpdateEventArgs("", -1.0);

        public string Message { get; set; }
        public double Progress { get; set; }

        public ProgressUpdateEventArgs(string message) 
            : this(message, -1.0f)
        {

        }

        public ProgressUpdateEventArgs(string message, double progress)
        {
            Message = message;
            Progress = progress;
        }
    }

    public static partial class DSC
    {
        private static ProgressUpdateEventArgs _lastUpdate = ProgressUpdateEventArgs.Empty;

        /// <summary>
        /// An event that is called when a progress update is reported. The sender object can be a null value.
        /// </summary>
        public static event ProgressUpdateEventHandler ProgressUpdated;

        /// <summary>
        /// Sends a progress update to the <see cref="ProgressUpdated"/> event handler.
        /// </summary>
        /// <param name="sender">The object sending a progress update.</param>
        /// <param name="message">The message of the update being reported.</param>
        /// <param name="progress">The current progress of the update being reported. Optional.</param>
        public static void Update(object sender, string message, double progress = -1.0)
        {
            _lastUpdate = new ProgressUpdateEventArgs(message, progress);

            if (ProgressUpdated != null)
                ProgressUpdated(sender, _lastUpdate);
        }

        /// <summary>
        /// Returns the most recently reported progress update.
        /// </summary>
        /// <returns>A <see cref="ProgressUpdateEventArgs"/> object containing the progress data.</returns>
        public static ProgressUpdateEventArgs GetLastUpdate()
        {
            return _lastUpdate;
        }

        /// <summary>
        /// Sends a progress update to the <see cref="ProgressUpdated"/> event handler with a null sender object.
        /// </summary>
        /// <param name="message">The message of the update being reported.</param>
        /// <param name="progress">The current progress of the update being reported. Optional.</param>
        public static void Update(string message, double progress = -1.0)
        {
            Update(null, message, progress);
        }

        private static DSCConfiguration m_config;

        public static DSCConfiguration Configuration
        {
            get
            {
                if (m_config == null)
                    m_config = new DSCConfiguration(); // will be able to load both kinds of ini files (if present)

                return m_config;
            }
        }
        
        private static string GetFirstValidDirectory(string name, string[] dirs)
        {
            var paths = from d in dirs
                        where !String.IsNullOrEmpty(d)
                        select Path.Combine(d, name);

            foreach (var dir in paths)
            {
                if (Directory.Exists(dir))
                    return dir;
            }

            // none of the directories exist
            return String.Empty;
        }

        private static string GetSteamAppsDirectory()
        {
            var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            if (regKey != null)
            {
                // first we'll try the quickest method
                var steamDir = Path.Combine(GetProgramFilesDirectory(), "Steam");

                if (String.IsNullOrEmpty(steamDir))
                {
                    // ok, try using SteamPath
                    var steamPath = regKey.GetValue("SteamPath")?.ToString();

                    // hopefully we found steam!
                    if (String.IsNullOrEmpty(steamPath))
                        return String.Empty;
                 
                    // sanitize path
                    steamDir = Path.GetFullPath(Environment.ExpandEnvironmentVariables(steamPath));
                }

                var steamApps = Path.Combine(steamDir, @"steamapps\common");

                // hopefully we found the path!
                if (Directory.Exists(steamApps))
                    return Path.GetFullPath(steamApps);
            }

            // couldn't find directory
            return String.Empty;
        }

        private static string GetProgramFilesDirectory()
        {
            var progFiles = (Environment.Is64BitOperatingSystem)
                                ? Environment.SpecialFolder.ProgramFilesX86
                                : Environment.SpecialFolder.ProgramFiles;

            return Environment.GetFolderPath(progFiles);
        }

        private static string m_progDir;
        private static string m_steamDir;
        private static string m_atariDir;
        private static string m_ubiDir;
        
        private static bool m_gameLookupReady = false;

        private static void InitGameLookup()
        {
            if (m_gameLookupReady)
                return;

            m_progDir = GetProgramFilesDirectory();
            m_steamDir = GetSteamAppsDirectory();
            m_atariDir = Path.Combine(m_progDir, "Atari");
            m_ubiDir = Path.Combine(m_progDir, "Ubisoft");

            m_gameLookupReady = true;
        }

        private static string GetDefaultGameDirectory(string game)
        {
            InitGameLookup();

            string[] dirs1 = { m_atariDir, m_ubiDir };
            string[] dirs2 = { m_ubiDir, m_steamDir };

            switch (game)
            {
            case "Driv3r":
                return GetFirstValidDirectory(game, dirs1);
            case "DriverPL":
                return GetFirstValidDirectory(game, dirs2);
            case "DriverSF":
                return GetFirstValidDirectory(game, dirs2);
            }

            return String.Empty;
        }

        private static void SetupRegistryConfig()
        {
            // setup the registry configuration if necessary
            var config = (IDSCConfiguration)Configuration.RegistryConfiguration;

            string[] keys = {
                "Driv3r",
                "DriverPL",
                "DriverSF",
            };

            // this will only update values if they do not already exist
            foreach (var key in keys)
            {
                if (!config.HasKey(key))
                    config.SetDirectory(key, GetDefaultGameDirectory(key));
            }
        }

        public static bool VerifyGameDirectory(string name)
        {
            var dir = Configuration.GetDirectory(name);
            
            // directory must exist
            return (!String.IsNullOrEmpty(dir) && Directory.Exists(dir));
        }

        public static bool VerifyGameDirectory(string name, string callee)
        {
            // use 'name' as 'gameName' too
            return VerifyGameDirectory(name, name, callee);
        }

        public static bool VerifyGameDirectory(string name, string gameName, string callee)
        {
            if (VerifyGameDirectory(name))
                return true;

            // try to pick the directory manually
            var result = MessageBox.Show(
                    String.Format(
@"{0} directory not found!

Click OK to choose a directory and automatically update your settings.", gameName),
                    callee, MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

            if (result == DialogResult.OK)
            {
                var openFolder = new FolderBrowserDialog() {
                    RootFolder = Environment.SpecialFolder.MyComputer,
                    Description = String.Format("Please select the root directory of your {0} installation:", gameName),
                };

                var fResult = openFolder.ShowDialog();

                if (fResult == DialogResult.OK)
                {
                    // update configuration with user selection
                    Configuration.SetDirectory(name, openFolder.SelectedPath);
                    return true;
                }
            }

            // user didn't select a directory :(
            MessageBox.Show(
                    String.Format("Your settings have not been updated. Please update your settings or restart {0} and try again.", callee),
                    callee, MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return false;
        }

        static DSC()
        {
            SetupRegistryConfig();
        }
    }
}
