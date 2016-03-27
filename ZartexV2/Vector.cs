using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zartex
{
    public class Vector3
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vector3() { }
        public Vector3(float x, float y, float z)
        {
            X = (double)x;
            Y = (double)y;
            Z = (double)z;
        }
    }

    public class Vector4
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }

        public Vector4() { }
        public Vector4(float x, float y, float z, float w)
        {
            X = (double)x;
            Y = (double)y;
            Z = (double)z;
            W = (double)w;
        }
    }
}
