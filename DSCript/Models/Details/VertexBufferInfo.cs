using System.IO;

namespace DSCript.Models
{
    public struct VertexBufferInfo : IDetail
    {
        public int VerticesCount;
        public int VerticesLength;
        public int VerticesOffset;

        public int VertexLength;
        
        public int Reserved1;
        public int Reserved2;
        public int Reserved3;

        // not part of the spec; don't write this!
        // '0xABCDEF' used to mark as uninitialized
        public int Type;

        public bool HasScaledVertices
        {
            // Driv3r on PC doesn't support any scaling (value is zero),
            // but the Xbox version (and DPL) has this set to 1 -- hmm!
            get { return (Reserved2 == 1); }
        }
        
        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(VerticesCount);
            stream.Write(VerticesLength);
            stream.Write(VerticesOffset);

            stream.Write(VertexLength);

            stream.Write(0);

            // Driv3r PC doesn't support scaled vertices :(
            if (provider.Version != 6)
                stream.Write(1);

            stream.Write(0);
            stream.Write(0);
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            VerticesCount = stream.ReadInt32();
            VerticesLength = stream.ReadInt32();
            VerticesOffset = stream.ReadInt32();

            VertexLength = stream.ReadInt32();

            Reserved1 = stream.ReadInt32();

            if (provider.Version != 6)
                stream.Position += 4;

            Reserved2 = stream.ReadInt32();
            Reserved3 = stream.ReadInt32();
        }
    }
}
