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
    public class AnimPropObject : MissionObject
    {
        public override int TypeId
        {
            get { return 11; }
        }

        public Vector3 Position { get; set; }

        protected override void LoadData(Stream stream)
        {
            Position = stream.Read<Vector3>();
        }

        protected override void SaveData(Stream stream)
        {
            stream.Write(Position);
        }
    }
}
