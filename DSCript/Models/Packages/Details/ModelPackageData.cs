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

        public ushort Handle;
        
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

            Handle = (ushort)(stream.ReadUInt32() & 0xFFFF);
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
