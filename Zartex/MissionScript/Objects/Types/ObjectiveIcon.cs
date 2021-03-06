﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

using DSCript;

namespace Zartex
{
    public class ObjectiveIconObject : MissionObject
    {
        public override int TypeId
        {
            get { return 6; }
        }

        public Vector3 Position { get; set; }
        public Vector2 Rotation { get; set; }

        public int Type { get; set; }

        protected override void LoadData(Stream stream)
        {
            Position = stream.Read<Vector3>();
            Rotation = stream.Read<Vector2>();

            Type = stream.ReadInt32();
        }

        protected override void SaveData(Stream stream)
        {
            stream.Write(Position);
            stream.Write(Rotation);
            stream.Write(Type);
        }
    }
}
