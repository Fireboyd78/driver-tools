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

        /// <summary>World X of the centre.</summary>
        public float X { get; set; }

        /// <summary>World Z of the centre (there is no height).</summary>
        public float Z { get; set; }

        public float Radius { get; set; }

        [Obsolete("The three floats are (x, z, radius), not a position. Use X, Z and Radius.")]
        [Browsable(false)]
        public Vector3 Position
        {
            get { return new Vector3() { X = X, Y = Z, Z = Radius }; }
            set { X = value.X; Z = value.Y; Radius = value.Z; }
        }

        protected override void LoadData(Stream stream)
        {
            Reserved = stream.ReadInt32();

            X = stream.ReadSingle();
            Z = stream.ReadSingle();
            Radius = stream.ReadSingle();
        }

        protected override void SaveData(Stream stream)
        {
            stream.Write(Reserved);

            stream.Write(X);
            stream.Write(Z);
            stream.Write(Radius);
        }
    }
}
