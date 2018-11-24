using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

using DSCript;

using Zartex.Converters;

namespace Zartex
{
    public class SwitchObject : MissionObject
    {
        public override int TypeId
        {
            get { return 9; }
        }

        public Vector3 Position { get; set; }
        public Vector2 V1 { get; set; }
        public Vector2 V2 { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public int UID { get; set; }

        protected override void LoadData(Stream stream)
        {
            Position = stream.Read<Vector3>();
            V1 = stream.Read<Vector2>();
            V2 = stream.Read<Vector2>();

            UID = stream.ReadInt32();
        }

        protected override void SaveData(Stream stream)
        {
            stream.Write(Position);
            stream.Write(V1);
            stream.Write(V2);
            stream.Write(UID);
        }
    }
}
