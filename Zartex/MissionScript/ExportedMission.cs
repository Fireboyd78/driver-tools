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
        public ExportedMissionObjects Objects { get; set; }
        public SpoolableBuffer PropHandles { get; set; }

        public LogicExportData LogicData { get; set; }

        protected override void Load()
        {
            Objects = Spooler.GetFirstChild(ChunkType.ExportedMissionObjects).AsResource<ExportedMissionObjects>(true);
            PropHandles = Spooler.GetFirstChild(ChunkType.ExportedMissionPropHandles) as SpoolableBuffer;

            LogicData = Spooler.GetFirstChild(ChunkType.LogicExportData).AsResource<LogicExportData>(true);
        }

        protected override void Save()
        {
            SpoolableResourceFactory.Save(Objects);
            SpoolableResourceFactory.Save(LogicData);
        }
    }
}
