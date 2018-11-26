using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Zartex
{
    public class ActorDefinition : NodeDefinition
    {
        public int ObjectId { get; set; }

        public override void LoadData(Stream stream)
        {
            ObjectId = stream.ReadInt32();

            base.LoadData(stream);
        }

        public override void SaveData(Stream stream)
        {
            stream.Write(ObjectId);

            base.SaveData(stream);
        }
    }
}
