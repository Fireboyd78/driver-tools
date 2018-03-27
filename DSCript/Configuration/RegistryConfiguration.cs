using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Win32;

namespace DSCript
{
    public sealed class RegistryConfiguration : IDisposable, IDSCConfiguration
    {
        string IDSCConfiguration.this[string key]
        {
            get { return ((IDSCConfiguration)this).GetProperty(key); }
            set { ((IDSCConfiguration)this).SetProperty(key, value); }
        }

        string[] IDSCConfiguration.GetKeys()
        {
            return Key.GetValueNames();
        }

        bool IDSCConfiguration.HasKey(string key)
        {
            VerifyAccess();
            return Key.GetValue(key) != null;
        }

        string IDSCConfiguration.GetDirectory(string key)
        {
            var dir = GetValue(key)?.ToString();
            
            if (!String.IsNullOrEmpty(dir))
                dir = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dir));

            return dir;
        }

        bool IDSCConfiguration.SetDirectory(string key, string value)
        {
            // they're pretty much the same thing
            return ((IDSCConfiguration)this).SetProperty(key, value);
        }

        string IDSCConfiguration.GetProperty(string key)
        {
            return GetValue(key)?.ToString();
        }

        bool IDSCConfiguration.SetProperty(string key, string value)
        {
            // null is considered an empty string
            if (value == null)
                value = String.Empty;

            SetValue(key, value);

            // TODO: Check key exists?
            return true;
        }

        public RegistryKey Key { get; private set; }
        
        void IDisposable.Dispose()
        {
            if (Key != null)
            {
                Key.Dispose();
                Key = null;
            }
        }

        private void VerifyAccess()
        {
            if (Key == null)
                throw new NullReferenceException("Cannot retrieve configuration data from a RegistryKey that is no longer in use.");
        }

        public object this[string name]
        {
            get { return GetValue(name); }
            set { SetValue(name, value); }
        }

        public object GetValue(string name)
        {
            VerifyAccess();
            return Key.GetValue(name);
        }

        public void SetValue(string name, object value)
        {
            VerifyAccess();
            Key.SetValue(name, value);
        }

        public void SetValue(string name, object value, RegistryValueKind valueKind)
        {
            VerifyAccess();
            Key.SetValue(name, value, valueKind);
        }
        
        public RegistryConfiguration(RegistryKey baseKey, string name)
            : this(baseKey.CreateSubKey(name))
        {

        }

        public RegistryConfiguration(RegistryKey key)
        {
            if (key == null)
                throw new ArgumentNullException("RegistryKey data cannot be null.");

            Key = key;
        }
    }
}
