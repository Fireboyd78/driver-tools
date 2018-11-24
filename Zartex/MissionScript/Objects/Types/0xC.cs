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
    public class MissionObject_12 : MissionObject
    {
        public override int TypeId
        {
            get { return 12; }
        }

        public Vector3 V1 { get; set; }
        public Vector3 V2 { get; set; }

        protected override void LoadData(Stream stream)
        {
            V1 = stream.Read<Vector3>();
            V2 = stream.Read<Vector3>();
        }

        protected override void SaveData(Stream stream)
        {
            stream.Write(V1);
            stream.Write(V2);
        }
    }
}
