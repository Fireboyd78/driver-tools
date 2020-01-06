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
    public enum MaterialPackageType : int
    {
        PC = 0x504D4350,   // 'PCMP'
        PS2 = 0x32435354,   // 'TSC2'
        Xbox = 0x504D4258,   // 'XBMP'

        Unknown = -1,
    }

    public struct ReferenceInfo<T> : IDetail, IComparer<ReferenceInfo<T>>
        where T : class
    {
        public int Offset;
        
        public T Reference;
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            Offset = stream.ReadInt32();

            if (provider.Version == 6)
                stream.Position += 4;
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(Offset);

            if (provider.Version == 6)
                stream.Write(0);
        }

        int IComparer<ReferenceInfo<T>>.Compare(ReferenceInfo<T> x, ReferenceInfo<T> y)
        {
            return x.Offset.CompareTo(y.Offset);
        }

        public ReferenceInfo(T value)
        {
            Offset = -1;
            Reference = value;
        }

        public ReferenceInfo(T value, int offset)
        {
            Offset = offset;
            Reference = value;
        }
    }

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
    
    public struct SubstanceInfo : IDetail
    {
        public byte Bin;

        public int Flags;

        public byte R1;
        public byte R2;
        public byte R3;

        public byte TextureFlags;

        public int TextureRefsOffset;
        public int TextureRefsCount;

        public int PaletteRefsOffset;
        public int PaletteRefsCount;

        public int Reserved;
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            var sInf = 0;
            var sFlg = 0;

            if (provider.Version == 6)
            {
                sInf = stream.ReadInt32();
                sFlg = stream.ReadInt32();

                stream.Position += 8;
            }
            else
            {
                sFlg = stream.ReadInt32();
                sInf = stream.ReadInt32();
            }

            Bin = (byte)(sInf & 0xFF);
            Flags = ((sInf >> 8) & 0xFFFFFF);

            R1 = (byte)((sFlg >> 0) & 0xFF);
            R2 = (byte)((sFlg >> 8) & 0xFF);
            R3 = (byte)((sFlg >> 16) & 0xFF);

            TextureFlags = (byte)((sFlg >> 24) & 0xFF);

            TextureRefsOffset = stream.ReadInt32();
            TextureRefsCount = stream.ReadInt32();

            if (provider.Version != 6)
            {
                PaletteRefsOffset = stream.ReadInt32();
                PaletteRefsCount = stream.ReadInt32();
                
                Reserved = stream.ReadInt32(); // = 0x8000000
            }
            else
            {
                // TODO: read 'Reserved' properly? 
                stream.Position += 8;
            }
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            var sInf = ((Bin << 0) | ((Flags & 0xFFFFFF) << 8));
            var sFlg = ((R1 << 0) | (R2 << 8) | (R3 << 16) | (TextureFlags << 24));

            if (provider.Version == 6)
            {
                stream.Write(sInf);
                stream.Write(sFlg);

                stream.Write(0L);
            }
            else
            {
                stream.Write(sFlg);
                stream.Write(sInf);
            }

            stream.Write(TextureRefsOffset);
            stream.Write(TextureRefsCount);

            if (provider.Version != 6)
            {
                stream.Write(PaletteRefsOffset);
                stream.Write(PaletteRefsCount);

                stream.Write(Reserved);
            }
            else
            {
                // TODO: write 'Reserved' properly? 
                stream.Write(0L);
            }
        }
    }

    public struct TextureInfo : IDetail
    {
        public int UID;
        public int Hash;

        public int DataOffset;
        public int DataSize;

        public int Type;

        public short Width;
        public short Height;

        public int Flags;

        public int Reserved;
        
        int GetPackedBits(int value)
        {
            int bits = 0;

            for (bits = 0; value > 1; bits++)
                value >>= 1;
            
            return (value == 1) ? bits : 0;
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            UID = stream.ReadInt32();
            Hash = stream.ReadInt32();

            // skip 0xF00(1,4) header?
            if (provider.Version != 6)
                stream.Position += 8;

            DataOffset = stream.ReadInt32();
            DataSize = stream.ReadInt32();

            if (provider.Version != 6)
            {
                var format = stream.ReadInt32();

                // packed data -- very clever!
                Width = (short)(1 << ((format >> 20) & 0xF));
                Height = (short)(1 << ((format >> 24) & 0xF));

                // not 100% sure on this one
                Type = (format >> 16) & 0xF;

                // TODO: figure this stuff out
                Flags = (format & 0xFFFF);

                Reserved = stream.ReadInt32();
            }
            else
            {
                Type = stream.ReadInt32();

                Width = stream.ReadInt16();
                Height = stream.ReadInt16();

                Flags = stream.ReadInt32();

                Reserved = stream.ReadInt32();
            }
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(UID);
            stream.Write(Hash);

            if (provider.Version != 6)
            {
                stream.Write(0xF00);

                stream.Write((short)1);
                stream.Write((short)4);
            }

            stream.Write(DataOffset);
            stream.Write(DataSize); // unlike Reflections, we don't write zero ;)

            if (provider.Version != 6)
            {
                var format = 0;

                format |= (GetPackedBits(Width) & 0xF) << 20;
                format |= (GetPackedBits(Height) & 0xF) << 24;
                format |= (Type & 0xF) << 16;
                format |= (Flags & 0xFFFF);

                stream.Write(format);
                stream.Write(Reserved);
            }
            else
            {
                stream.Write(Type);

                stream.Write(Width);
                stream.Write(Height);

                stream.Write(Flags);

                stream.Write(Reserved);
            }
        }
    }

    public struct MaterialPackageData : IDetail
    {
        public MaterialPackageType PackageType;
        public int Version;
        
        public int MaterialsCount;
        public int MaterialsOffset;

        public int SubstanceLookupCount;
        public int SubstanceLookupOffset;

        public int SubstancesCount;
        public int SubstancesOffset;

        public int PaletteInfoLookupCount;
        public int PaletteInfoLookupOffset;

        public int PaletteInfoCount;
        public int PaletteInfoOffset;

        public int TextureLookupCount;
        public int TextureLookupOffset;
        
        public int TexturesCount;
        public int TexturesOffset;
        
        public int TextureDataOffset;
        public int DataSize;

        public bool HasPaletteInfo;
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            PackageType = (MaterialPackageType)stream.ReadInt32();

            Version = (PackageType == MaterialPackageType.PS2) 
                ? stream.ReadInt16() : stream.ReadInt32();

            Read(stream);
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            Write(stream);
        }

        public int HeaderSize
        {
            get
            {
                switch (PackageType)
                {
                case MaterialPackageType.PC:
                    return (HasPaletteInfo) ? 0x48 : 0x38;
                case MaterialPackageType.Xbox:
                    return 0x48;
                case MaterialPackageType.PS2:
                    return 0x14;
                }
                return 0;
            }
        }

        public int MaterialSize
        {
            get
            {
                switch (PackageType)
                {
                case MaterialPackageType.PC:
                    return (HasPaletteInfo) ? 0x10 : 0x18;
                case MaterialPackageType.PS2:
                case MaterialPackageType.Xbox:
                    return 0x10;
                }
                return 0;
            }
        }

        public int SubstanceSize
        {
            get
            {
                switch (PackageType)
                {
                case MaterialPackageType.PC:
                    return (HasPaletteInfo) ? 0x1C : 0x20;
                case MaterialPackageType.Xbox:
                    return 0x1C;
                case MaterialPackageType.PS2:
                    return 0xC;
                }
                return 0;
            }
        }

        public int TextureSize
        {
            get
            {
                switch (PackageType)
                {
                case MaterialPackageType.PC:
                case MaterialPackageType.Xbox:
                    return 0x20;
                case MaterialPackageType.PS2:
                    return 0x28;
                }
                return 0;
            }
        }

        public int LookupSize
        {
            get
            {
                switch (PackageType)
                {
                case MaterialPackageType.PC:
                    return (HasPaletteInfo) ? 0x4 : 0x8;
                case MaterialPackageType.Xbox:
                case MaterialPackageType.PS2:
                    return 0x4;
                }
                return 0;
            }
        }

        private void GenerateOffsets()
        {
            var alignment = (PackageType == MaterialPackageType.PS2) ? 128 : 4096;

            MaterialsOffset = HeaderSize;

            SubstanceLookupOffset = MaterialsOffset + (MaterialsCount * MaterialSize);
            SubstancesOffset = SubstanceLookupOffset + (SubstanceLookupCount * LookupSize);

            TextureLookupOffset = SubstancesOffset + (SubstancesCount * SubstanceSize);
            TexturesOffset = TextureLookupOffset + (TextureLookupCount * LookupSize);

            TextureDataOffset = Memory.Align(TexturesOffset + (TexturesCount * TextureSize), alignment);
        }
        
        public void Read(Stream stream)
        {
            if (stream.ReadInt32() != (int)PackageType)
                throw new InvalidOperationException("Bad magic - Cannot load material package!");

            if (PackageType == MaterialPackageType.PS2)
            {
                MaterialsCount = stream.ReadInt16();
                SubstanceLookupCount = stream.ReadInt16();
                SubstancesCount = stream.ReadInt16();
                TextureLookupCount = stream.ReadInt16();
                TexturesCount = stream.ReadInt16();

                // skip version
                stream.Position += 2;

                DataSize = stream.ReadInt32();
                
                // offsets need to be generated manually
                GenerateOffsets();
            }
            else
            {
                // skip version
                stream.Position += 4;

                MaterialsCount = stream.ReadInt32();
                MaterialsOffset = stream.ReadInt32();

                SubstanceLookupCount = stream.ReadInt32();
                SubstanceLookupOffset = stream.ReadInt32();

                SubstancesCount = stream.ReadInt32();
                SubstancesOffset = stream.ReadInt32();

                // TODO: process palette information (used on Xbox & possibly DPL PC?)
                if (HasPaletteInfo)
                {
                    PaletteInfoLookupCount = stream.ReadInt32();
                    PaletteInfoLookupOffset = stream.ReadInt32();

                    PaletteInfoCount = stream.ReadInt32();
                    PaletteInfoOffset = stream.ReadInt32();
                }

                TextureLookupCount = stream.ReadInt32();
                TextureLookupOffset = stream.ReadInt32();

                TexturesCount = stream.ReadInt32();
                TexturesOffset = stream.ReadInt32();

                TextureDataOffset = stream.ReadInt32();
                DataSize = stream.ReadInt32();
            }
        }

        public void Write(Stream stream)
        {
            stream.Write((int)PackageType);
            
            if (PackageType == MaterialPackageType.PS2)
            {
                stream.Write((short)MaterialsCount);
                stream.Write((short)SubstanceLookupCount);
                stream.Write((short)SubstancesCount);
                stream.Write((short)TextureLookupCount);
                stream.Write((short)TexturesCount);
                stream.Write((short)Version);
            }
            else
            {
                stream.Write(Version);

                stream.Write(MaterialsCount);
                stream.Write(MaterialsOffset);
                stream.Write(SubstanceLookupCount);
                stream.Write(SubstanceLookupOffset);
                stream.Write(SubstancesCount);
                stream.Write(SubstancesOffset);
                
                if (HasPaletteInfo)
                {
                    switch (PackageType)
                    {
                    case MaterialPackageType.PC:
                        {
                            // no palette info present
                            for (int i = 0; i < 4; i++)
                                stream.Write(0);
                        } break;
                    case MaterialPackageType.Xbox:
                        throw new NotImplementedException();
                    }
                }

                stream.Write(TextureLookupCount);
                stream.Write(TextureLookupOffset);
                stream.Write(TexturesCount);
                stream.Write(TexturesOffset);

                stream.Write(TextureDataOffset);
                stream.Write(DataSize);
            }
        }

        public MaterialPackageData(MaterialPackageType packageType) : this(packageType, false) { }
        public MaterialPackageData(MaterialPackageType packageType, bool hasPaletteInfo)
        {
            PackageType = packageType;
            Version = -1;

            switch (PackageType)
            {
            case MaterialPackageType.PS2:
            case MaterialPackageType.Xbox:
                Version = 2;
                break;
            case MaterialPackageType.PC:
                Version = 3;
                break;
            }
            
            HasPaletteInfo = hasPaletteInfo;

            MaterialsCount = 0;
            MaterialsOffset = 0;

            SubstancesCount = 0;
            SubstancesOffset = 0;

            SubstanceLookupCount = 0;
            SubstanceLookupOffset = 0;

            PaletteInfoLookupCount = 0;
            PaletteInfoLookupOffset = 0;

            PaletteInfoCount = 0;
            PaletteInfoOffset = 0;

            TexturesCount = 0;
            TexturesOffset = 0;

            TextureLookupCount = 0;
            TextureLookupOffset = 0;

            TextureDataOffset = 0;
            DataSize = 0;
        }

        public MaterialPackageData(MaterialPackageType packageType, int nMaterials, int nSubMaterials, int nTextures)
            : this(packageType, nMaterials, nSubMaterials, nTextures, false)
        { }

        public MaterialPackageData(MaterialPackageType packageType, int nMaterials, int nSubMaterials, int nTextures, bool hasPaletteInfo)
            : this(packageType, hasPaletteInfo)
        {
            MaterialsCount = nMaterials;

            SubstanceLookupCount = nSubMaterials;
            SubstancesCount = nSubMaterials;

            TextureLookupCount = nTextures;
            TexturesCount = nTextures;

            GenerateOffsets();
        }

        public MaterialPackageData(MaterialPackageType packageType, Stream stream) : this(packageType, stream, false) { }
        public MaterialPackageData(MaterialPackageType packageType, Stream stream, bool hasPaletteInfo) : this(packageType, hasPaletteInfo)
        {
            Read(stream);
        }
    }
}
