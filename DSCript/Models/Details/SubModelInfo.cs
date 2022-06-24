using System.Diagnostics;
using System.IO;

namespace DSCript.Models
{
    public interface ISubModelInfo : IDetail, ICopyDetail<SubModel> { }

    //
    // WIP: still researching this
    //
    public struct WiiSubModelInfo : ISubModelInfo
    {
        public int IndexCount;
        public int IndexOffset;

        public int VertexBuffer;
        public int Reserved;
        public int Unknown1;
        public int PrimitiveType;

        public MaterialHandle Material;

        public int VertexOffset;
        public int VertexCount;

        public int ExtraOffset;

        public void CopyTo(SubModel subModel)
        {
            var instance = subModel.LodInstance;
            var lod = instance.Parent;
            var model = lod.Parent;

            subModel.PrimitiveType = (PrimitiveType)(PrimitiveType + 128);

            subModel.VertexBaseOffset = 0;
            subModel.VertexOffset = VertexOffset;
            subModel.VertexCount = VertexCount;

            subModel.IndexOffset = IndexOffset;
            subModel.IndexCount = IndexCount;

            subModel.Material = Material;
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(0x98);

            stream.Write(VertexCount);
            stream.Write(IndexOffset);

            stream.WriteByte(VertexBuffer);
            stream.WriteByte(Reserved);
            stream.WriteByte(Unknown1);
            stream.WriteByte(PrimitiveType);

            stream.Write(0);

            stream.Write(Material);

            stream.Write(VertexOffset);
            stream.Write(IndexCount);
            
            if ((provider.Flags & ModelPackageData.FLAG_HasWiiExtraData) != 0)
                stream.Write(ExtraOffset);
        }
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            var check = stream.ReadInt32();

            if (check != 0x98)
                throw new InvalidDataException("Bad Wii SubModel data");

            VertexCount = stream.ReadInt32();
            IndexOffset = stream.ReadInt32();

            VertexBuffer = stream.ReadByte();
            Reserved = stream.ReadByte();
            Unknown1 = stream.ReadByte();
            PrimitiveType = stream.ReadByte();

            stream.Position += 4;

            Material = stream.Read<MaterialHandle>();

            VertexOffset = stream.ReadInt32();
            IndexCount = stream.ReadInt32();

            if ((provider.Flags & ModelPackageData.FLAG_HasWiiExtraData) != 0)
                ExtraOffset = stream.ReadInt32();
        }
    }

    public struct SubModelInfo : ISubModelInfo
    {
        public static readonly int FLAG_SmallSubModels = (1 << 25);

        public int PrimitiveType;

        public int VertexBaseOffset;
        public int VertexOffset;
        public int VertexCount;

        public int IndexOffset;
        public int IndexCount;

        public MaterialHandle Material;

        public bool IsOptimizedFormat;

        public void CopyTo(SubModel subModel)
        {
            subModel.PrimitiveType = (PrimitiveType)PrimitiveType;

            subModel.VertexBaseOffset = VertexBaseOffset;
            subModel.VertexOffset = VertexOffset;
            subModel.VertexCount = VertexCount;

            subModel.IndexOffset = IndexOffset;
            subModel.IndexCount = IndexCount;

            subModel.Material = Material;

            subModel.IsOptimizedFormat = IsOptimizedFormat;
        }
        
        public void Serialize(Stream stream, IDetailProvider provider)
        {
            if ((provider.Flags & FLAG_SmallSubModels) != 0)
            {
                stream.Write(VertexCount);
                stream.Write(IndexOffset);

                stream.Write(PrimitiveType);

                stream.Write(0L);
                stream.Write(Material);
            }
            else
            {
                stream.Write(PrimitiveType);

                stream.Write(VertexBaseOffset);
                stream.Write(VertexOffset);
                stream.Write(VertexCount);

                stream.Write(IndexOffset);
                stream.Write(IndexCount);

                // +0x18 reserved bytes
                stream.Write(0L);
                stream.Write(0L);
                stream.Write(0L);

                stream.Write(Material);
                stream.Write(0);
            }
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            if ((provider.Flags & FLAG_SmallSubModels) != 0)
            {
                VertexCount = stream.ReadInt32();
                IndexOffset = stream.ReadInt32();

                PrimitiveType = stream.ReadInt32();

                stream.Position += 8;

                Material = stream.Read<MaterialHandle>();

                IsOptimizedFormat = true;
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

                IsOptimizedFormat = (provider.Version != 6);
            }
        }
    }
}
