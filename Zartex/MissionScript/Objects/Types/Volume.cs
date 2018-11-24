using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

using DSCript;

namespace Zartex
{
    public class VolumeObject : MissionObject
    {
        public override int TypeId
        {
            get { return 3; }
        }

        public int Reserved { get; set; }
        public Vector3 Position { get; set; }

        protected override void LoadData(Stream stream)
        {
            Reserved = stream.ReadInt32();
            Position = stream.Read<Vector3>();
        }

        protected override void SaveData(Stream stream)
        {
            stream.Write(Reserved);
            stream.Write(Position);
        }
    }
}
