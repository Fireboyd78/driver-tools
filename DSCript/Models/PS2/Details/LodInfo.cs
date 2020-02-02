using System.Collections.Generic;
using System.IO;

namespace DSCript.Models
{
    public struct LodInfoPS2 : IClassDetail<LodPS2>
    {
        public Vector4 Scale;

        public short LodInstanceCount;
        public short LodMask;

        public int LodInstanceDataOffset;
        public int NumTriangles;
        public int Reserved;

        public LodPS2 ToClass()
        {
            return new LodPS2() {
                IsDummy = (LodInstanceCount == 0),

                Mask = LodMask,
                NumTriangles = NumTriangles,

                Scale = Scale,

                Instances = new List<LodInstancePS2>(LodInstanceCount),
            };
        }

        public LodInfoPS2(Stream stream)
        {
            Scale = stream.Read<Vector4>();

            LodInstanceCount = stream.ReadInt16();
            LodMask = stream.ReadInt16();

            LodInstanceDataOffset = stream.ReadInt32();
            NumTriangles = stream.ReadInt32();
            Reserved = stream.ReadInt32();
        }
    }
}
