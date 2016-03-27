using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public class LogicExportData : SpoolableResource<SpoolablePackage>
    {
        public StringCollectionData StringCollection { get; set; }
        public SpoolableBuffer SoundBankTable { get; set; }

        public LogicDataCollection<ActorDefinition> Actors { get; set; }
        public LogicDataCollection<NodeDefinition> Nodes { get; set; }

        public SpoolableBuffer ActorSetTable { get; set; }
        public WireCollectionData WireCollection { get; set; }
        public SpoolableBuffer ScriptCounters { get; set; }
        
        protected override void Load()
        {
            StringCollection = Spooler.GetFirstChild(ChunkType.LogicExportStringCollection).AsResource<StringCollectionData>(true);
            SoundBankTable = Spooler.GetFirstChild(ChunkType.LogicExportSoundBank) as SpoolableBuffer;

            Actors = Spooler.GetFirstChild(ChunkType.LogicExportActorsChunk).AsResource<LogicDataCollection<ActorDefinition>>(true);
            Nodes = Spooler.GetFirstChild(ChunkType.LogicExportNodesChunk).AsResource<LogicDataCollection<NodeDefinition>>(true);

            ActorSetTable = Spooler.GetFirstChild(ChunkType.LogicExportActorSetTable) as SpoolableBuffer;
            WireCollection = Spooler.GetFirstChild(ChunkType.LogicExportWireCollections).AsResource<WireCollectionData>(true);
            ScriptCounters = Spooler.GetFirstChild(ChunkType.LogicExportScriptCounters) as SpoolableBuffer;
        }

        protected override void Save()
        {
            if (Spooler.Parent == null)
                throw new InvalidOperationException("What have you done!");

            SpoolableResourceFactory.Save(StringCollection);
            SpoolableResourceFactory.Save(Actors);
            SpoolableResourceFactory.Save(Nodes);
            SpoolableResourceFactory.Save(WireCollection);
        }
    }
}
