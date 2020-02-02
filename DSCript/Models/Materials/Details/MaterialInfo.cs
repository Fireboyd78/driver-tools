using System.IO;

namespace DSCript.Models
{
    public struct MaterialInfo : IDetail
    {
        public int SubstanceRefsOffset;
        public int SubstanceRefsCount;

        public int Type;

        public float AnimationSpeed;
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            SubstanceRefsOffset = stream.ReadInt32();
            SubstanceRefsCount = stream.ReadInt32();

            Type = stream.ReadInt32();

            AnimationSpeed = stream.ReadSingle();

            if (provider.Version == 6)
                stream.Position += 8;
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(SubstanceRefsOffset);
            stream.Write(SubstanceRefsCount);

            stream.Write(Type);

            stream.Write(AnimationSpeed);

            if (provider.Version == 6)
                stream.Write(0L);
        }
    }
}
