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
    public class MissionObject_10 : MissionObject
    {
        public override int TypeId
        {
            get { return 10; }
        }

        public int Type { get; set; }

        public Vector2 V1 { get; set; }
        public Vector2 V2 { get; set; }

        protected override void LoadData(Stream stream)
        {
            Type = stream.ReadInt32();

            V1 = stream.Read<Vector2>();
            V2 = stream.Read<Vector2>();
        }

        protected override void SaveData(Stream stream)
        {
            stream.Write(Type);
            stream.Write(V1);
            stream.Write(V2);
        }
    }
}
