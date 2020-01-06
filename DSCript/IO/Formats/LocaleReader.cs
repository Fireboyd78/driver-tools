using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using DSCript;

namespace DSCript
{
    public sealed class LocaleReader
    {
        private static readonly string e_Entries = @"(<ID\b[^>]*>.*?<\/TEXT>)";
        private static readonly string e_Id = @"<ID\b[^>]*>(.*?)<\/ID>";
        private static readonly string e_Text = @"<TEXT\b[^>]*>(.*?)<\/TEXT>";
        private static readonly string e_Lang = @"<LANGUAGE\b[^>]*>(.*?)<\/LANGUAGE>";
        private static readonly string e_Platform = @"<PLATFORM\b[^>]*>(.*?)<\/PLATFORM>";

        public Dictionary<int, string> Strings { get; private set; }

        public string Language { get; private set; }
        public string Platform { get; private set; }

        public string this[int localeId]
        {
            get { return Strings[localeId]; }
        }

        private void ReadLocaleData(Stream stream)
        {
            var text = String.Empty;

            using (var s = new StreamReader(stream, true))
            {
                text = s.ReadToEnd();
            }

            Strings = new Dictionary<int, string>();

            Platform = Regex.Match(text, e_Platform).Groups[1].Value;
            Language = Regex.Match(text, e_Lang).Groups[1].Value;

            foreach (Match m in Regex.Matches(text, e_Entries))
            {
                //DSC.Log(m.Value);

                var val = m.Value;

                var idStr = Regex.Match(val, e_Id).Groups[1].Value;
                var str = Regex.Match(val, e_Text).Groups[1].Value;

                var id = int.Parse(idStr);

                if (!Strings.ContainsKey(id))
                    Strings.Add(id, str);
            }
        }

        public LocaleReader(string file)
        {
            if (File.Exists(file))
            {
                using (var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ReadLocaleData(fs);
                }
            }
        }

        public LocaleReader(Stream stream)
        {
            ReadLocaleData(stream);
        }
    }
}
