using System.IO;

namespace DSCript.Models
{
    public struct SubModelInfo : IDetail
    {
        public static readonly int UseXBoxSizeHackFlag = 0x2000000;

        public int PrimitiveType;

        public int VertexBaseOffset;
        public int VertexOffset;
        public int VertexCount;

        public int IndexOffset;
        public int IndexCount;

        public MaterialHandle Material;
        
        public void Serialize(Stream stream, IDetailProvider provider)
        {
            if ((provider.Flags & UseXBoxSizeHackFlag) != 0)
            {
                stream.Write(IndexCount);
                stream.Write(IndexOffset);

                stream.Write(PrimitiveType);

                stream.Write(VertexCount);
                stream.Write(VertexOffset);
                stream.Write(0);
            }
            else
            {
                stream.Write(PrimitiveType);

                stream.Write(VertexBaseOffset);
                stream.Write(VertexOffset);
                stream.Write(VertexCount);

                stream.Write(IndexOffset);
                stream.Write(IndexCount);

                stream.Write(0L);
                stream.Write(0L);
                stream.Write(0L);
            }
            
            stream.Write(Material);
            stream.Write(0);
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            if ((provider.Flags & UseXBoxSizeHackFlag) != 0)
            {
                VertexCount = stream.ReadInt32();
                IndexOffset = stream.ReadInt32();

                PrimitiveType = stream.ReadInt32();

                // TODO: Verify these are correct
                VertexBaseOffset = stream.ReadInt32();
                VertexOffset = stream.ReadInt32();

                Material = stream.Read<MaterialHandle>();
            }
            else
            {
                PrimitiveType = stream.ReadInt32();

                VertexBaseOffset = stream.ReadInt32();
                VertexOffset = stream.ReadInt32();
                VertexCount = stream.ReadInt32();

                IndexOffset = stream.ReadInt32();
                IndexCount = stream.ReadInt32();

                stream.Position += 0x18;

                Material = stream.Read<MaterialHandle>();

                stream.Position += 4;
            }
        }
    }
}
