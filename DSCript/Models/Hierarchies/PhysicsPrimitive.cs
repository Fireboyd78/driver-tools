using System;
using System.IO;

namespace DSCript.Models
{
    public class PhysicsPrimitive : IDetail
    {
        public Vector4 Position { get; set; }
        public Matrix44 Transform { get; set; }

        public int Material { get; set; }

        public Vector3 Unknown { get; set; }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            Position = stream.Read<Vector4>();
            Transform = stream.Read<Matrix44>();

            Material = stream.ReadInt32();

            Unknown = stream.Read<Vector3>();
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(Position);
            stream.Write(Transform);

            stream.Write(Material);

            stream.Write(Unknown);
        }
    }
}