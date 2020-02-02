using System.IO;

namespace DSCript.Models
{
    public struct PhysicsInfo : IDetail
    {
        public static readonly string Magic = "PDL001.002.003a";

        public int CollisionModelsCount;
        public int PrimitivesCount;

        public int CollisionModelsOffset;
        public int PrimitivesOffset;
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            CollisionModelsCount = stream.ReadInt32();
            PrimitivesCount = stream.ReadInt32();

            CollisionModelsOffset = stream.ReadInt32();
            PrimitivesOffset = stream.ReadInt32();

            var check = stream.ReadString(16);

            if (check != Magic)
                throw new InvalidDataException("Invalid PDL magic!");
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(CollisionModelsCount);
            stream.Write(PrimitivesCount);

            stream.Write(CollisionModelsOffset);
            stream.Write(PrimitivesOffset);

            stream.Write(Magic + "\0");
        }
    }
}
