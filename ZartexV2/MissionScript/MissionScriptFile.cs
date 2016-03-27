using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public class MissionScriptFile : FileChunker
    {
        public MissionSummaryData MissionSummary { get; set; }

        public StringCollectionData StringCollection { get; set; }

        public SpoolableBuffer SoundBankTable { get; set; }

        public LogicCollectionData<ActorDefinition> Actors { get; set; }
        public LogicCollectionData<LogicDefinition> Nodes { get; set; }

        public WireCollectionData WireCollection { get; set; }

        public SpoolableBuffer ActorSetTable { get; set; }
        public SpoolableBuffer ScriptCounters { get; set; }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            switch ((ChunkType)sender.Magic)
            {
            case ChunkType.LogicExportStringCollection:
                StringCollection = sender.AsResource<StringCollectionData>(true);
                Console.WriteLine("Loaded string collection!");
                break;

            case ChunkType.LogicExportSoundBank:
                SoundBankTable = sender as SpoolableBuffer;
                break;

            case ChunkType.LogicExportActorsChunk:
                Actors = sender.AsResource<LogicCollectionData<ActorDefinition>>(true);
                Console.WriteLine("Loaded Actors chunk!");
                break;

            case ChunkType.LogicExportNodesChunk:
                Nodes = sender.AsResource<LogicCollectionData<LogicDefinition>>(true);
                Console.WriteLine("Loaded LogicNodes chunk!");
                break;

            case ChunkType.LogicExportWireCollections:
                WireCollection = sender.AsResource<WireCollectionData>(true);
                Console.WriteLine("Loaded wire collections!");
                break;

            case ChunkType.LogicExportActorSetTable:
                ActorSetTable = sender as SpoolableBuffer;
                break;

            case ChunkType.LogicExportScriptCounters:
                ScriptCounters = sender as SpoolableBuffer;
                break;

            case ChunkType.MissionSummary:
                MissionSummary = sender.AsResource<MissionSummaryData>(true);
                Console.WriteLine("Loaded mission summary!");
                break;
            }

            // fire the event handler
            base.OnSpoolerLoaded(sender, e);
        }

        public override bool CanSave
        {
            get
            {
                if (MissionSummary != null
                    && StringCollection != null
                    && SoundBankTable != null
                    && Actors != null
                    && Nodes != null
                    && WireCollection != null
                    && ScriptCounters != null)
                {
                    return true;
                }
                return false;
            }
        }

        public MissionScriptFile() : base() { }
        public MissionScriptFile(string filename) : base(filename) { }
    }
}
