using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GMC2Snooper
{
    public enum SectionType : int
    {
        GEO2 = 0x324F4547,
        MPAK = 0x4B41504D,
        TSC2 = 0x32435354,
    }

    public enum ModelType : int
    {
        VehiclePackage = 0xFF,
        Character = 0x23,
    }

    public enum TextureSource : int
    {
        VehiclePackage = 0xFFFD,
        VehicleGlobals = 0x1D,
    }

    public struct Vector2
    {
        public float X;
        public float Y;

        public void Scale(float scale)
        {
            X /= scale;
            Y /= scale;
        }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public struct Vector3
    {
        public float X;
        public float Y;
        public float Z;

        public void Scale(float scale)
        {
            X /= scale;
            Y /= scale;
            Z /= scale;
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct Vector4
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public void Scale(float scale)
        {
            X /= scale;
            Y /= scale;
            Z /= scale;
            W /= scale;
        }

        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }
}
