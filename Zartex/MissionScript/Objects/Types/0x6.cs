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
    public class MissionObject_6 : MissionObject
    {
        public override int TypeId
        {
            get { return 6; }
        }

        public Vector3 V1 { get; set; }
        public Vector2 V2 { get; set; }

        public int Id { get; set; }

        protected override void LoadData(Stream stream)
        {
            V1 = stream.Read<Vector3>();
            V2 = stream.Read<Vector2>();

            Id = stream.ReadInt32();
        }

        protected override void SaveData(Stream stream)
        {
            stream.Write(V1);
            stream.Write(V2);
            stream.Write(Id);
        }
    }
}
