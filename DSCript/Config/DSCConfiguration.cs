using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Win32;

namespace DSCript
{
    public interface IDSCConfiguration
    {
        string this[string key] { get; set; }

        string[] GetKeys();

        bool HasKey(string key);
        
        string GetDirectory(string key);
        bool SetDirectory(string key, string value);

        string GetProperty(string key);
        bool SetProperty(string key, string value);
    }

    public sealed class DSCConfiguration : IDSCConfiguration
    {
        private static RegistryKey m_regKey;
        
        public static RegistryKey RegistryKey
        {
            get
            {
                if (m_regKey == null)
                    m_regKey = Registry.CurrentUser.CreateSubKey("Software\\Driver Tools");

                return m_regKey;
            }
        }
        
        private IDSCConfiguration m_registryConfig;
        private IDSCConfiguration m_iniConfig;

        private IDSCConfiguration Config
        {
            get
            {
                // ini gets precedence
                if (m_iniConfig != null)
                    return m_iniConfig;
                // then the registry
                if (m_registryConfig != null)
                    return m_registryConfig;

                return null;
            }
        }
        
        public RegistryConfiguration RegistryConfiguration
        {
            get { return m_registryConfig as RegistryConfiguration; }
        }

        public IniConfiguration IniConfiguration
        {
            get { return m_iniConfig as IniConfiguration; }
        }

        public string this[string key]
        {
            get { return GetProperty(key); }
            set { SetProperty(key, value); }
        }

        public string[] GetKeys()
        {
            var regKeys = m_registryConfig?.GetKeys();
            var iniKeys = m_iniConfig?.GetKeys();

            var keys = new List<String>();

            if (iniKeys != null)
            {
                // only add keys that aren't null/empty
                keys.AddRange(from key in iniKeys
                              where (!String.IsNullOrEmpty(key))
                              select key);
            }

            if (regKeys != null)
            {
                if (keys.Count == 0)
                {
                    // no ini keys to compare against
                    keys.AddRange(regKeys);
                }
                else
                {
                    // don't add dupes (empty keys are ok)
                    keys.AddRange(from key in regKeys
                                  where (!keys.Contains(key))
                                  select key);
                }
            }

            return keys.ToArray();
        }

        public bool HasKey(string key)
        {
            return GetKeys().Contains(key);
        }

        public string GetDirectory(string key)
        {
            var dir = m_iniConfig?.GetDirectory(key);
            
            if (String.IsNullOrEmpty(dir))
                dir = m_registryConfig?.GetDirectory(key);

            return dir;
        }

        public bool SetDirectory(string key, string value)
        {
            return Config?.SetDirectory(key, value) ?? false;
        }

        public string GetProperty(string key)
        {
            var dir = m_iniConfig?.GetProperty(key);

            if (String.IsNullOrEmpty(dir))
                dir = m_registryConfig?.GetProperty(key);

            return dir;
        }

        public bool SetProperty(string key, string value)
        {
            return Config?.SetProperty(key, value) ?? false;
        }

        public DSCConfiguration(string identifier = "")
        {
            var hasIdentifier = !String.IsNullOrEmpty(identifier);

            m_registryConfig = (hasIdentifier)
                ? new RegistryConfiguration(RegistryKey, identifier)
                : new RegistryConfiguration(RegistryKey);

            var iniFilename = Path.Combine(Environment.CurrentDirectory, "settings.ini");

            // FALLBACK: try looking for DSCript.ini (old format)
            if (!File.Exists(iniFilename))
            {
                iniFilename = Path.Combine(Environment.CurrentDirectory, "DSCript.ini");

                if (File.Exists(iniFilename))
                    m_iniConfig = (hasIdentifier)
                        ? new IniConfiguration(iniFilename, identifier)
                        : new IniConfiguration(iniFilename, "Global");
            }
            else
            {
                // use new format (no "Global.Directories" crap)
                m_iniConfig = new IniConfiguration(iniFilename, identifier);
            }
        }
    }
}
