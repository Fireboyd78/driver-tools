using System.Collections.Generic;
using System.IO;

namespace DSCript.Models
{
    public class PhysicsCollisionModel
    {
        public struct Detail : IDetail
        {
            public int Count;
            public int Offset;

            public float BoundingRadius;

            public int Flags;

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                var ptr = (int)stream.Position;

                Count = stream.ReadInt32();
                Offset = stream.ReadInt32() + ptr;

                BoundingRadius = stream.ReadSingle();

                Flags = stream.ReadInt32();
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                var ptr = (int)stream.Position;

                stream.Write(Count);
                stream.Write(Offset - ptr);

                stream.Write(BoundingRadius);

                stream.Write(Flags);
            }
        }

        public float BoundingRadius { get; set; }

        public int Flags { get; set; }

        public List<PhysicsPrimitive> Children { get; set; }
    }
}