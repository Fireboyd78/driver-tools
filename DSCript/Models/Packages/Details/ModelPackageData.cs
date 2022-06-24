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
        public static readonly int FLAG_HasWiiExtraData = (1 << 30);

        public static readonly int Revision = 2;

        public PlatformType Platform;

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

        public int Wii_ExtraLength;
        public int Wii_ExtraOffset;

        public bool HasSmallSubModels;
        public bool HasWiiExtraData;

        public MaterialPackageData MaterialPackage;
        
        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            if (provider.Platform != Platform)
                throw new InvalidOperationException($"Tried to serialize {Platform} data to {provider.Platform}");

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

            if (Platform == PlatformType.Wii)
            {
                if (HasWiiExtraData)
                {
                    stream.Write(Wii_ExtraLength);
                    stream.Write(Wii_ExtraOffset);
                }
            }
            else
            {
                if (Version == 6)
                    stream.Write(0);
            }
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            Platform = provider.Platform;

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

            if (Platform == PlatformType.Wii)
            {
                Wii_ExtraLength = stream.ReadInt32();
                Wii_ExtraOffset = stream.ReadInt32();

                // only DSF has this information; otherwise, it's padding
                // the padding bytes shouldn't result in a valid offset
                var test = (Wii_ExtraOffset + Wii_ExtraLength);

                if (test < MaterialDataOffset)
                {
                    DSC.Log("Detected extra Wii platform data");
                    HasWiiExtraData = true;
                    provider.Flags |= FLAG_HasWiiExtraData;
                }
                else
                {
                    Wii_ExtraLength = 0;
                    Wii_ExtraOffset = 0;
                }
            }
            else
            {
                if (Version == 6)
                    stream.Position += 4;

                if (Version == 1 || Version == 9)
                {
                    if ((provider.Flags & SubModelInfo.FLAG_SmallSubModels) != 0)
                    {
                        HasSmallSubModels = true;
                    }
                    else
                    {
                        var declsOffset = Memory.Align(SubModelsOffset + (SubModelsCount * 0x38), 128);

                        if (declsOffset > VertexDeclsOffset)
                        {
                            HasSmallSubModels = true;
                            provider.Flags |= SubModelInfo.FLAG_SmallSubModels;
                        }
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
                    return (HasWiiExtraData) ? 0x4C : 0x44;
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
                    if (HasSmallSubModels)
                        return 0x18;

                    if (Platform == PlatformType.Wii)
                        return (HasWiiExtraData) ? 0x24 : 0x20;

                    return 0x38;
                case 6:
                    return 0x38;
                case 9:
                    if (HasSmallSubModels)
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
                    return (Platform == PlatformType.Wii) ? 0x14 : 0x20;
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
        
        public ModelPackageData(PlatformType platform, int version, int flags)
        {
            Platform = platform;

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

            // DSF only
            Wii_ExtraLength = 0;
            Wii_ExtraOffset = 0;

            HasWiiExtraData = false;

            HasSmallSubModels = (version == 9 || ((flags & SubModelInfo.FLAG_SmallSubModels) != 0));

            MaterialPackage = new MaterialPackageData(MaterialPackageType.Unknown);
        }

        public ModelPackageData(PlatformType platform, int version, int flags, int uid, int nModels, int nInstances, int nSubModels, int nIndices, int nVertexDecls)
            : this(platform, version, flags)
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
        
        public ModelPackageData(PlatformType platform, int version, int flags, int uid)
            : this(platform, version, flags, uid, 0, 0, 0, 0, 0)
        {

        }
    }
}
