using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public class ExportedMission : SpoolableResource<SpoolablePackage>
    {
        public SpoolableBuffer ExportedMissionObjects { get; set; }
        public SpoolableBuffer ExportedMissionPropHandles { get; set; }

        public LogicExportData LogicExportData { get; set; }

        protected override void Load()
        {
            ExportedMissionObjects = Spooler.GetFirstChild(ChunkType.ExportedMissionObjects) as SpoolableBuffer;
            ExportedMissionPropHandles = Spooler.GetFirstChild(ChunkType.ExportedMissionPropHandles) as SpoolableBuffer;

            LogicExportData = Spooler.GetFirstChild(ChunkType.LogicExportData).AsResource<LogicExportData>(true);
        }

        protected override void Save()
        {
            SpoolableResourceFactory.Save(LogicExportData);
        }
    }
}
