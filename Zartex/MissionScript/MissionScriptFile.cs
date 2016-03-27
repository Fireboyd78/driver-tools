using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public class MissionScriptFile : FileChunker
    {
        public ExportedMission MissionData { get; set; }
        public MissionSummaryData MissionSummary { get; set; }

        public bool HasLocale { get; private set; }

        public Dictionary<int, string> LocaleStrings { get; set; }

        public bool HasLocaleString(int id)
        {
            return (LocaleStrings != null) ? LocaleStrings.ContainsKey(id) : false;
        }

        public string GetLocaleString(int id)
        {
            if (HasLocaleString(id))
            {
                var str = LocaleStrings[id];

                return (!String.IsNullOrEmpty(str)) ? str : "<NULL>";
            }
            return "<???>";
        }

        public void LoadLocaleFile(int missionId)
        {
            LoadLocaleFile(MPCFile.GetMissionLocaleFilepath(missionId));
        }

        public void LoadLocaleFile(string filename)
        {
            string text = String.Empty;

            if (!File.Exists(filename))
                return;

            using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read))
            {
                // DPL temporary workaround
                var encoding = (((fs.ReadInt32() >> 16) & 0xFFFF) == 0xFEFF) ? Encoding.Unicode : Encoding.UTF8;

                if (encoding != Encoding.Unicode)
                    fs.Seek(0, SeekOrigin.Begin);

                using (StreamReader f = new StreamReader(fs, encoding, true))
                {
                    text = f.ReadToEnd();
                }
            }

            LocaleStrings = new Dictionary<int, string>();

            var e_ENTRIES = @"(<ID\b[^>]*>.*?<\/TEXT>)";
            var e_ID = @"<ID\b[^>]*>(.*?)<\/ID>";
            var e_TEXT = @"<TEXT\b[^>]*>(.*?)<\/TEXT>";

            foreach (Match m in Regex.Matches(text, e_ENTRIES))
            {
                var val = m.Value;

                var idStr = Regex.Match(val, e_ID).Groups[1].Value;
                var str = Regex.Match(val, e_TEXT).Groups[1].Value;

                var id = int.Parse(idStr);

                if (!LocaleStrings.ContainsKey(id))
                    LocaleStrings.Add(id, str);
            }

            HasLocale = true;
        }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            switch ((ChunkType)sender.Magic)
            {
            case ChunkType.ExportedMissionChunk:
                {
                    MissionData = sender.AsResource<ExportedMission>(true);
                } break;
            case ChunkType.MissionSummary:
                {
                    MissionSummary = sender.AsResource<MissionSummaryData>(true);

                    if (MissionSummary.MissionLocaleId > -1)
                        LoadLocaleFile(MissionSummary.MissionLocaleId);
                } break;
            }

            // fire the event handler
            base.OnSpoolerLoaded(sender, e);
        }

        protected override void OnFileSaveBegin()
        {
            SpoolableResourceFactory.Save(MissionData);
            SpoolableResourceFactory.Save(MissionSummary);
        }

        public MissionScriptFile() : base() { }
        public MissionScriptFile(string filename) : base(filename) { }
    }
}
