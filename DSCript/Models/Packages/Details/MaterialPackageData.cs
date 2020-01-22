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

    public enum RenderBinType
    {
        ReflectedSky = 0,
        Portal = 1,
        Sky = 2,
        Road = 3,
        Building = 4,
        Clutter = 5,
        Car = 6,
        Particle = 7,

        MissionIcon = 8,

        Overlay_1_0 = 9,
        Overlay_0_5 = 10,
        Overlay_1_5 = 11,

        OverheadMap_1 = 12,
        OverheadMap_2 = 13,
        OverheadMap_3 = 14,
        OverheadMap_4 = 15,
        OverheadMap_5 = 16,
        OverheadMap_6 = 17,
        OverheadMap_7 = 18,
        OverheadMap_8 = 19,

        NearWater = 20,
        FarWater = 21,

        GrimeOverlay = 22,
        CarInterior = 23,
        LowPoly = 24,
        CarOverlay = 25,

        FarWater_2 = 26,
        FarWater_3 = 27,

        OverheadMap_9 = 28,
        OverheadMap_10 = 29,

        FullBrightOverlay = 30,

        OverheadMap_11 = 31,
        OverheadMap_12 = 32,

        ShadowedParticle = 33,
        GlowingLight = 34,
        PostRoad = 35,

        Overlay_2_0 = 36,

        DrawAlphaLast = 37,
        UntexturedSemiTransparent = 38,
        PreWater = 39,

        Overlay_0_25 = 40,

        //
        // Driver: Parallel Lines and up
        //

        Trees = 48,

        Menus_0_25 = 49,
        Menus_0_5 = 50,
        Menus_0_75 = 51,
        Menus_1_0 = 52,
        Menus_1_25 = 53,
        Menus_1_5 = 54,
        Menus_1_75 = 55,
        Menus_2_0 = 56,

        Clouds = 57,
        Hyperlow = 58,
        LightFlare = 59,

        OverlayMask_1 = 60,
        OverlayMask_2 = 61,

        TreeWall = 62,
        BridgeWires = 63,
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

        public byte TS1;
        public byte TS2;
        public byte TS3;

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

            TS1 = (byte)((sFlg >> 0) & 0xFF);
            TS2 = (byte)((sFlg >> 8) & 0xFF);
            TS3 = (byte)((sFlg >> 16) & 0xFF);

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
            var sFlg = ((TS1 << 0) | (TS2 << 8) | (TS3 << 16) | (TextureFlags << 24));

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

        static int[,] m_BinEffectLookup = {
            {  0,  0 },
            {  1,  1 },
            {  2, 21 },
            {  3,  6 },
            {  4, -1 },
            {  5, -1 },
            {  6, 12 },
            {  7, 30 },
            {  8, 32 },
            {  9, 48 },
            { 10, 47 },
            { 11, 49 },
            { 12, 33 },
            { 13, 34 },
            { 14, 35 },
            { 15, 36 },
            { 16, 37 },
            { 17, 38 },
            { 18, 39 },
            { 19, 40 },
            { 20, 16 },
            { 21, 10 },
            { 22, 19 },
            { 23, 11 },
            { 24, 22 },
            { 25, 17 },
            { 26, 14 },
            { 27, 15 },
            { 28, 41 },
            { 29, 42 },
            { 30, 29 },
            { 31, 43 },
            { 32, 44 },
            { 33, -1 },
            { 34, 25 },
            { 35,  7 },
            { 36, -1 },
            { 37, 28 },
            { 38, 51 },
            { 39,  9 },
            { 40, 46 },
        };

        public static void Resolve(ref SubstanceInfo substance)
        {
            var ts1 = substance.TS1;
            var specular = 0;

            // 1 | 2
            if ((ts1 & 3) != 0)
            {
                var ts2 = substance.TS2;

                // 1 | 2
                if ((ts2 & 3) != 0)
                    specular = 1;
            }

            var bin = substance.Bin;
            var effect = -1;

            if (bin < (m_BinEffectLookup.Length / 2))
                effect = m_BinEffectLookup[bin, 1];

            var flags = substance.Flags;

            if (effect == -1)
            {
                effect = 8;

                if ((flags & 0x40) != 0)
                {
                    if (specular == 0)
                        effect = (bin == (int)RenderBinType.Clutter) ? 20 : 18;
                }
            }

            var flg1 = 0;
            var flg2 = 0;
            
            if ((flags & 0x1) != 0)
                flg2 |= 0x40;
            if ((flags & 0x2) != 0)
                flg2 |= 0x20;
            if ((flags & 0x4) != 0)
                flg1 = 1;
            if ((flags & 0x80) != 0)
                flg2 |= 2;
            if ((flags & 0x100) != 0)
                flg2 |= 8;
            if ((flags & 0x200) != 0)
                flg2 |= 0x80;
            if ((flags & 0x400) != 0)
                flg2 |= 1;

            if (flg2 != 0)
                flg2 = ((flg1 == 0) ? 3 : 4);

            // shiny road?
            if ((effect == 7) && ((flg2 & 0x80) != 0))
            {
                if ((flg2 & 2) != 0)
                {
                    flg2 = 0;
                    flg1 = 0;

                    effect = 4;
                }
            }

            // flg3 = 0
            substance.Bin = (byte)effect;
            substance.Flags = ((flg2 << 8) | flg1);
        }

        public static bool Compile(ref SubstanceInfo substance)
        {
            var flags = (SubstanceExtraFlags)substance.TextureFlags;

            var bump_map = 0;

            if (flags.HasFlag(SubstanceExtraFlags.BumpMap))
                bump_map = 1;

            var effect = substance.Bin;

            var ts1 = substance.TS1;
            var ts2 = substance.TS2;
            var ts3 = substance.TS3;

            var flg1 = (substance.Flags & 0xFF);
            var flg2 = ((substance.Flags >> 8) & 0xFF);
            var flg3 = ts3;

            if (flags.HasFlag(SubstanceExtraFlags.FLAG_1))
            {
                ts3 = 2;
            }
            else
            {
                if ((ts1 & 3) != 0)
                {
                    if ((ts2 & 3) != 0)
                    {
                        if (effect == 9)
                        {
                            substance.TextureFlags = (int)SubstanceExtraFlags.FLAG_1;
                            substance.TS1 = 129;
                            substance.TS2 = 0;
                            substance.TS3 = 0;
                            substance.Bin = 13;
                            substance.Flags = ((flg3 << 16) | (flg2 << 8) | flg1);

                            return true;
                        }

                        switch (flg1)
                        {
                        case 0:
                            flg1 = 2;
                            break;
                        case 1:
                            flg2 |= 0x10;
                            flg1 = 5;
                            break;
                        case 3:
                            flg1 = 5;
                            break;
                        }
                        
                        switch (ts2)
                        {
                        case 0:
                            flags = SubstanceExtraFlags.ColorMask;
                            ts1 = 2;
                            ts2 = 1;
                            break;
                        case 1:
                            flags = (SubstanceExtraFlags.FLAG_1 | SubstanceExtraFlags.BumpMap);
                            ts1 = 2;
                            ts2 = 2;
                            ts3 = 0;
                            break;
                        case 2:
                            flags = SubstanceExtraFlags.ColorMask;
                            ts1 = 2;
                            ts2 = 1;
                            ts3 = 0;
                            break;
                        }
                    }
                }
                else
                {
                    switch (ts2)
                    {
                    case 1:
                        flags = (SubstanceExtraFlags.BumpMap | SubstanceExtraFlags.ColorMask);
                        ts1 = 1;
                        ts2 = 2;
                        ts3 = 0;
                        break;
                    case 2:
                        flags = SubstanceExtraFlags.ColorMask;
                        ts1 = 1;
                        ts2 = 1;
                        ts3 = 0;
                        break;
                    default:
                        if (bump_map == 1)
                        {
                            if (ts2 == 0)
                            {
                                flags = SubstanceExtraFlags.FLAG_1;
                                ts1 = 1;
                                ts2 = 0;
                                ts3 = 0;
                            }
                        }
                        else
                        {
                            flags = SubstanceExtraFlags.FLAG_1;
                            ts1 = 129;
                            ts2 = 0;
                            ts3 = 0;
                        }
                        break;
                    }
                }

                if (bump_map == 1)
                {
                    flags = SubstanceExtraFlags.BumpMap;
                    ts3 = 1;
                }
            }
            
            if (effect == 16)
            {
                if (flg1 == 2)
                    flg1 = ((flg2 == 0x10) ? 1 : 0);

                flg2 &= 0xFB;
                ts3 = 4;
            }

            substance.TextureFlags = (byte)flags;
            substance.TS1 = ts1;
            substance.TS2 = ts2;
            substance.TS3 = ts3;
            substance.Flags = ((flg3 << 16) | (flg2 << 8) | flg1);

            return true;

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
