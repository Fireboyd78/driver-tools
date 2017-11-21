using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public class ExportedMissionObjects : SpoolableResource<SpoolableBuffer>
    {
        public List<MissionObject> Objects { get; set; }

        public MissionObject this[int index]
        {
            get { return Objects[index]; }
            set { Objects[index] = value; }
        }

        protected override void Load()
        {
            using (var f = Spooler.GetMemoryStream())
            {
                var nObjects = f.ReadInt32();

                for (int i = 0; i < nObjects; i++)
                {

                }
            }
        }

        protected override void Save()
        {

        }
    }
}