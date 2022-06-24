using System.IO;

namespace DSCript.Models
{
    public struct VertexBufferInfo : IDetail
    {
        public int VerticesCount;
        public int VerticesLength;
        public int VerticesOffset;

        public int VertexLength;

        // NB: only relevant to Wii; Xbox/PC this is part of D3DRESOURCE...
        public int CompressionType;
        
        // not part of the spec; don't write this!
        // '0xABCDEF' used to mark as uninitialized
        public int Type;

        public bool HasScaledVertices
        {
            // Driv3r on PC doesn't support any scaling (value is zero),
            // but the Xbox version (and DPL) has this set to 1 -- hmm!
            get { return (CompressionType == 1); }
        }

        public bool HasScaledVertices_Wii
        {
            // Wii platform only
            get { return (CompressionType == 2); }
        }
        
        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(VerticesCount);
            stream.Write(VerticesLength);
            stream.Write(VerticesOffset);

            stream.Write(VertexLength);

            if (provider.Platform == PlatformType.Wii)
            {
                // just this, no extra bytes
                stream.Write(CompressionType);
            }
            else
            {
                stream.Write(0);

                // Driv3r PC doesn't support scaled/compressed vertices
                if (provider.Version != 6)
                    stream.Write(CompressionType);

                stream.Write(0);
                stream.Write(0);
            }
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            VerticesCount = stream.ReadInt32();
            VerticesLength = stream.ReadInt32();
            VerticesOffset = stream.ReadInt32();

            VertexLength = stream.ReadInt32();

            if (provider.Platform == PlatformType.Wii)
            {
                // just this, no extra bytes
                CompressionType = stream.ReadInt32();
            }
            else
            {
                stream.Position += 4;

                // Driv3r PC doesn't support scaled/compressed vertices
                CompressionType = (provider.Version != 6) ? stream.ReadInt32() : -1;

                stream.Position += 8;
            }
        }
    }
}
