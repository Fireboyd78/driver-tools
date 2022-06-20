using System;
using System.IO;

namespace DSCript.Models
{
    public class PhysicsPrimitive : IDetail
    {
        public Vector4 Bounds { get; set; }

        public Matrix44 Transform { get; set; }

        // sphere, cylinder
        public float Radius
        {
            get { return Bounds.X; }
        }

        // cylinder
        public float Height
        {
            get { return Bounds.Y; }
        }

        // tells us the primitive type?
        public int Flags { get; set; }

        public float Elasticity { get; set; }
        public float Friction { get; set; }

        public float Zestiness { get; set; } // 'eT'

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            Bounds = stream.Read<Vector4>();
            Transform = stream.Read<Matrix44>();

            Flags = stream.ReadInt32();

            Elasticity = stream.ReadSingle();
            Friction = stream.ReadSingle();
            Zestiness = stream.ReadSingle();
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(Bounds);
            stream.Write(Transform);

            stream.Write(Flags);

            stream.Write(Elasticity);
            stream.Write(Friction);
            stream.Write(Zestiness);
        }
    }
}