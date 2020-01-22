using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

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

    public struct ModelInfo : IDetail
    {
        public UID UID;

        public Vector4 Scale;

        // INCOMING TRANSMISSION...
        // RE: OPERATION S.T.E.R.N....
        // ...
        // YOUR ASSISTANCE HAS BEEN NOTED...
        // ...
        // <END OF TRANSMISSION>...
        public short BufferIndex;
        public short BufferType;

        public int Flags;

        // reserved space for effect index
        // sadly can't be used to force a specific effect cause game overwrites it :(
        public int Reserved;
        
        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(UID);
            stream.Write(Scale);

            stream.Write(BufferIndex);
            stream.Write(BufferType);

            stream.Write(Flags);
            stream.Write(0);

            if (provider.Version == 6)
                stream.Write(0);
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            UID = stream.Read<UID>();
            Scale = stream.Read<Vector4>();

            BufferIndex = stream.ReadInt16();
            BufferType = stream.ReadInt16();

            Flags = stream.ReadInt32();
            Reserved = stream.ReadInt32();

            if (provider.Version == 6)
                stream.Position += 4;
        }
    }

    public struct LodInfo : IDetail
    {
        public int InstancesOffset;
        public int InstancesCount;

        // vertex + triangle count?
        public int Reserved;

        public int Flags;
        public int Mask;

        public int ExtraData;
        
        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(InstancesOffset);

            if (provider.Version == 6)
                stream.Write(0);

            stream.Write(InstancesCount);
            
            stream.Write((int)MagicNumber.FIREBIRD); // ;)
            
            stream.Write(Flags);

            stream.Write(Mask);
            stream.Write(0);

            if (provider.Version == 6)
                stream.Write(0);
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            // initialize with offset
            InstancesOffset = stream.ReadInt32();

            if (provider.Version == 6)
                stream.Position += 4;

            InstancesCount = stream.ReadInt32();
            
            Reserved = stream.ReadInt32();
            
            Flags = stream.ReadInt32();
            Mask = stream.ReadInt32();

            ExtraData = stream.ReadInt32();
            
            if (provider.Version == 6)
                stream.Position += 4;
        }
    }

    public struct LodInstanceInfo : IDetail
    {
        public struct DebugInfo : IDetail
        {
            public int Reserved;
            public short Handle;
            
            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                stream.Write(Reserved);
                stream.Write(Handle);

                if (provider.Version == 6)
                    stream.Write((short)MagicNumber.FB); // ;)
            }

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                Reserved = stream.ReadInt32();
                Handle = stream.ReadInt16();

                if (provider.Version == 6)
                    stream.Position += 2;
            }
        }

        public int SubModelsOffset;
        
        public Matrix44 Transform;

        public short SubModelsCount;
        public short UseTransform;
        
        public DebugInfo Info;
        
        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(SubModelsOffset);

            if (provider.Version == 6)
                stream.Write(0);

            stream.Write(Transform);

            stream.Write(SubModelsCount);
            stream.Write(UseTransform);

            if (provider.Version == 6)
                stream.Write((int)MagicNumber.FIREBIRD); // ;)

            provider.Serialize(stream, ref Info);
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            // initialize with offset
            SubModelsOffset = stream.ReadInt32();

            if (provider.Version == 6)
                stream.Position += 4;

            Transform = stream.Read<Matrix44>();

            SubModelsCount = stream.ReadInt16();
            UseTransform = stream.ReadInt16();
            
            if (provider.Version == 6)
                stream.Position += 4;

            Info = provider.Deserialize<DebugInfo>(stream);
        }
    }
    
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
                IndexCount = stream.ReadInt32();
                IndexOffset = stream.ReadInt32();

                PrimitiveType = stream.ReadInt32();

                // TODO: Verify these are correct
                VertexCount = stream.ReadInt32();
                VertexOffset = stream.ReadInt32();

                // skip junk
                stream.Position += 4;
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
            }

            Material = stream.Read<MaterialHandle>();

            stream.Position += 4;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ModelPackageData : IDetail
    {
        public static readonly int Revision = 2;
        
        public int Version;
        public int UID;

        public int ModelsCount;
        public int ModelsOffset;

        public int LodInstancesCount;
        public int LodInstancesOffset;

        public int SubModelsCount;
        public int SubModelsOffset;

        public short Handle;
        
        public int Reserved;
        
        public int TextureDataOffset;
        public int MaterialDataOffset;

        public int IndicesCount;
        public int IndicesLength;
        public int IndicesOffset;

        public int VertexDeclsCount;
        public int VertexDeclsOffset;

        // Driv3r on Xbox is special :/
        public bool UseSubModelSizeHacks;

        public MaterialPackageData MaterialPackage;
        
        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(Version);
            stream.Write(UID);

            stream.Write(ModelsCount);
            stream.Write(ModelsOffset);

            stream.Write(LodInstancesCount);
            stream.Write(LodInstancesOffset);

            stream.Write(SubModelsCount);
            stream.Write(SubModelsOffset);

            stream.Write(Handle);
            stream.Write((short)MagicNumber.FB); // ;)

            stream.Write(Revision);

            stream.Write(TextureDataOffset);
            stream.Write(MaterialDataOffset);

            stream.Write(IndicesCount);
            stream.Write(IndicesLength);
            stream.Write(IndicesOffset);

            stream.Write(VertexDeclsCount);
            stream.Write(VertexDeclsOffset);

            if (Version == 6)
                stream.Write(0);
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            Version = stream.ReadInt32();
            UID = stream.ReadInt32();

            ModelsCount = stream.ReadInt32();
            ModelsOffset = stream.ReadInt32();

            LodInstancesCount = stream.ReadInt32();
            LodInstancesOffset = stream.ReadInt32();

            SubModelsCount = stream.ReadInt32();
            SubModelsOffset = stream.ReadInt32();

            Handle = (short)(stream.ReadInt32() & 0xFFFF);
            Reserved = stream.ReadInt32();

            TextureDataOffset = stream.ReadInt32();
            MaterialDataOffset = stream.ReadInt32();

            IndicesCount = stream.ReadInt32();
            IndicesLength = stream.ReadInt32();
            IndicesOffset = stream.ReadInt32();

            VertexDeclsCount = stream.ReadInt32();
            VertexDeclsOffset = stream.ReadInt32();

            if (Version == 6)
                stream.Position += 4;

            if (Version == 9)
            {
                if ((provider.Flags & SubModelInfo.UseXBoxSizeHackFlag) != 0)
                {
                    UseSubModelSizeHacks = true;
                }
                else
                {
                    var declsOffset = Memory.Align(SubModelsOffset + (SubModelsCount * 0x38), 128);

                    if (declsOffset > VertexDeclsOffset)
                    {
                        UseSubModelSizeHacks = true;
                        provider.Flags |= SubModelInfo.UseXBoxSizeHackFlag;
                    }
                }
            }
        }

        public int HeaderSize
        {
            get
            {
                switch (Version)
                {
                case 1:
                case 9:
                    return 0x44;
                case 6:
                    return 0x48;
                }
                return 0x28; // no model/material info
            }
        }
        
        public int ModelSize
        {
            get
            {
                switch (Version)
                {
                case 1:
                case 9:
                    return 0x14C;
                case 6:
                    return 0x188;
                }
                return 0;
            }
        }
        
        public int LodSize
        {
            get
            {
                switch (Version)
                {
                case 1:
                case 9:
                    return 0x18;
                case 6:
                    return 0x20;
                }
                return 0;
            }
        }
        
        public int LodInstanceSize
        {
            get
            {
                switch (Version)
                {
                case 1:
                case 9:
                    return 0x4E;

                case 6:
                    return 0x58;
                }
                return 0;
            }
        }
        
        public int SubModelSize
        {
            get
            {
                switch (Version)
                {
                case 1:
                case 6:
                    return 0x38;
                case 9:
                    // Driv3r's quirks makes me cry T_T
                    if (UseSubModelSizeHacks)
                        return 0x18;

                    return 0x38;
                }
                return 0;
            }
        }
        
        public int VertexDeclSize
        {
            get
            {
                switch (Version)
                {
                case 1:
                case 9:
                    return 0x20;

                case 6:
                    return 0x1C;
                }
                return 0;
            }
        }

        public int GetSizeOfModels()
        {
            return ModelsCount * ModelSize;
        }

        public int GetSizeOfLodInstances()
        {
            return LodInstancesCount * LodInstanceSize;
        }

        public int GetSizeOfSubModels()
        {
            return SubModelsCount * SubModelSize;
        }

        public int GetSizeOfVertexDecls()
        {
            return VertexDeclsCount * VertexDeclSize;
        }

        public int GetVertexBuffersOffset()
        {
            return Memory.Align(IndicesOffset + IndicesLength, 4096);
        }
        
        public ModelPackageData(int version)
        {
            Version = version;

            UID = 0;

            ModelsCount = 0;
            ModelsOffset = 0;

            LodInstancesCount = 0;
            LodInstancesOffset = 0;

            SubModelsCount = 0;
            SubModelsOffset = 0;

            Handle = 0;
            Reserved = 0;

            TextureDataOffset = 0;
            MaterialDataOffset = 0;

            IndicesCount = 0;
            IndicesLength = 0;
            IndicesOffset = 0;

            VertexDeclsCount = 0;
            VertexDeclsOffset = 0;

            UseSubModelSizeHacks = (version == 9);

            MaterialPackage = new MaterialPackageData(MaterialPackageType.Unknown);
        }

        public ModelPackageData(int version, int uid, int nModels, int nInstances, int nSubModels, int nIndices, int nVertexDecls)
            : this(version)
        {
            UID = uid;

            ModelsCount = nModels;
            ModelsOffset = Memory.Align(HeaderSize, 128);

            // only calculate offsets if there's model data
            if (ModelsCount > 0)
            {
                LodInstancesCount = nInstances;
                LodInstancesOffset = Memory.Align(ModelsOffset + (ModelsCount * ModelSize), 128);

                SubModelsCount = nSubModels;
                SubModelsOffset = LodInstancesOffset + (LodInstancesCount * LodInstanceSize);

                IndicesCount = nIndices;
                IndicesLength = IndicesCount * sizeof(short);

                VertexDeclsCount = nVertexDecls;
                VertexDeclsOffset = Memory.Align(SubModelsOffset + (SubModelsCount * SubModelSize), 128);

                IndicesOffset = VertexDeclsOffset + (VertexDeclsCount * VertexDeclSize);
            }
        }
        
        public ModelPackageData(int version, int uid)
            : this(version, uid, 0, 0, 0, 0, 0)
        {

        }
    }
}
