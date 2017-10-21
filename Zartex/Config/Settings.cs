using System;
using System.ComponentModel;
using System.Configuration;

namespace Zartex.Settings
{
    public sealed class ZartexConfiguration : ConfigurationSection
    {
        private const string INVALID_CHARS = "*?\"<>|";

        [ConfigurationProperty("locale", DefaultValue = "English", IsRequired = false)]
        [StringValidator(InvalidCharacters = INVALID_CHARS)]
        public string Locale
        {
            get { return (string)this["locale"]; }
            set { this["locale"] = value; }
        }
    }

    public static class Configuration
    {
        public static ZartexConfiguration Settings { get; }

        static Configuration()
        {
            var section = ConfigurationManager.GetSection("Configuration");
            
            // TODO: add basic error checking?
            Settings = (section != null) ? (ZartexConfiguration)section : new ZartexConfiguration();
        }
    }
}
