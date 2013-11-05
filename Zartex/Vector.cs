using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zartex
{
    public sealed class Vector3
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vector3(float x, float y, float z)
        {
            X = BitConverter.ToSingle(BitConverter.GetBytes(x), 0);
            Y = BitConverter.ToSingle(BitConverter.GetBytes(y), 0);
            Z = BitConverter.ToSingle(BitConverter.GetBytes(z), 0);
        }
    }

    public sealed class Vector4
    {
        public double W { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vector4(float x, float y, float z, float w)
        {
            X = BitConverter.ToSingle(BitConverter.GetBytes(x), 0);
            Y = BitConverter.ToSingle(BitConverter.GetBytes(y), 0);
            Z = BitConverter.ToSingle(BitConverter.GetBytes(z), 0);
            W = BitConverter.ToSingle(BitConverter.GetBytes(w), 0);
        }
    }
}
