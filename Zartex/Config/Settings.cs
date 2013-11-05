using System;
using System.ComponentModel;
using System.Configuration;

namespace Zartex.Settings
{
    public class LocaleValidator : ConfigurationValidatorBase
    {
        private string[] locales;

        public LocaleValidator(string[] args)
        {
            locales = args;
        }

        public override bool CanValidate(Type type)
        {
 	         return type == typeof(string);
        }

        public override void Validate(object value)
        {
            string val = (string)value;

            try
            {
                foreach (string s in locales)
                    if (val == s)
                        return;

                throw new ConfigurationErrorsException("The specified locale does not exist. Please try a different setting.");
            }
            catch (ConfigurationErrorsException e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public class LocaleValidatorAttribute : ConfigurationValidatorAttribute
    {
        private string[] locales;

        public LocaleValidatorAttribute(params string[] SupportedLocales)
        {
            locales = SupportedLocales;
        }

        public override ConfigurationValidatorBase ValidatorInstance
        {
            get { return new LocaleValidator(locales); }
        }
    }

    public class Configuration : ConfigurationSection
    {
        private const string CONFIG_NAME = "Configuration";

        private const string LOC_EN = "English";
        private const string LOC_FR = "French";
        private const string LOC_DE = "German";
        private const string LOC_IT = "Italian";
        private const string LOC_SP = "Spanish";

        private const string INVALID_CHARS = "*?\"<>|";

        private const string INSTALL_DIR = "installDirectory";
        private const string LOCALE = "locale";

        private static Configuration _settings;

        static Configuration()
        {
            _settings = (Configuration)ConfigurationManager.GetSection(CONFIG_NAME);   
        }

        public static Configuration Settings
        {
            get { return _settings; }
        }

        [ConfigurationProperty(INSTALL_DIR, DefaultValue = "", IsRequired = true)]
        [StringValidator(InvalidCharacters = INVALID_CHARS, MaxLength = 256)]
        public string InstallDirectory
        {
            get { return (string)this[INSTALL_DIR]; }
            set { this[INSTALL_DIR] = value; }
        }

        [ConfigurationProperty(LOCALE, DefaultValue = LOC_EN, IsRequired = true)]
        [LocaleValidator(LOC_EN, LOC_FR, LOC_DE, LOC_IT, LOC_SP)]
        public string Locale
        {
            get { return (string)this[LOCALE]; }
            set { this[LOCALE] = value; }
        }
    }
}
