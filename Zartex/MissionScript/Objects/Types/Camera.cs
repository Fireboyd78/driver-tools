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
    public class CameraObject : MissionObject
    {
        public override int TypeId
        {
            get { return 7; }
        }
        
        public Vector4 V1 { get; set; }
        public Vector4 V2 { get; set; }
        public Vector4 V3 { get; set; }

        public int Reserved { get; set; }

        protected override void LoadData(Stream stream)
        {
            V1 = stream.Read<Vector4>();
            V2 = stream.Read<Vector4>();
            V3 = stream.Read<Vector4>();

            Reserved = stream.ReadInt32();
        }

        protected override void SaveData(Stream stream)
        {
            stream.Write(V1);
            stream.Write(V2);
            stream.Write(V3);
            stream.Write(Reserved);
        }
    }
}
