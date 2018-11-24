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
    public class WeaponObject : MissionObject
    {
        public override int TypeId
        {
            get { return 10; }
        }

        public int Type { get; set; }

        public float Rotation { get; set; }
        public Vector3 Position { get; set; }

        protected override void LoadData(Stream stream)
        {
            Type = stream.ReadInt32();

            Rotation = stream.ReadSingle();
            Position = stream.Read<Vector3>();
        }

        protected override void SaveData(Stream stream)
        {
            stream.Write(Type);
            stream.Write(Rotation);
            stream.Write(Position);
        }
    }
}
