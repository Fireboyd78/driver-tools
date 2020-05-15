using System;
using System.Collections;
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

    public struct MaterialPackageData : IDetail
    {
        public MaterialPackageType PackageType;
        public int Version;
        
        public int MaterialsCount;
        public int MaterialsOffset;

        public int SubstanceRefsCount;
        public int SubstanceRefsOffset;

        public int SubstancesCount;
        public int SubstancesOffset;

        public int PaletteRefsCount;
        public int PaletteRefsOffset;

        public int PalettesCount;
        public int PalettesOffset;

        public int TextureRefsCount;
        public int TextureRefsOffset;
        
        public int TexturesCount;
        public int TexturesOffset;
        
        public int TextureDataOffset;

        public int DataSize;

        public bool HasPalettes;
        
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
                case MaterialPackageType.Xbox:
                    return (HasPalettes) ? 0x48 : 0x38;
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
                    return (HasPalettes) ? 0x10 : 0x18;
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
                case MaterialPackageType.Xbox:
                    return (HasPalettes) ? 0x1C : 0x20;
                case MaterialPackageType.PS2:
                    return 0xC;
                }
                return 0;
            }
        }

        public int PaletteSize
        {
            get
            {
                if (HasPalettes)
                    return 0x14;

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

        public int ReferenceSize
        {
            get
            {
                switch (PackageType)
                {
                case MaterialPackageType.PC:
                    return (HasPalettes) ? 0x4 : 0x8;
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

            SubstanceRefsOffset = MaterialsOffset + (MaterialsCount * MaterialSize);
            SubstancesOffset = SubstanceRefsOffset + (SubstanceRefsCount * ReferenceSize);

            TextureRefsOffset = SubstancesOffset + (SubstancesCount * SubstanceSize);
            TexturesOffset = TextureRefsOffset + (TextureRefsCount * ReferenceSize);

            TextureDataOffset = Memory.Align(TexturesOffset + (TexturesCount * TextureSize), alignment);
        }
        
        public void Read(Stream stream)
        {
            if (stream.ReadInt32() != (int)PackageType)
                throw new InvalidOperationException("Bad magic - Cannot load material package!");

            if (PackageType == MaterialPackageType.PS2)
            {
                MaterialsCount = stream.ReadInt16();
                SubstanceRefsCount = stream.ReadInt16();
                SubstancesCount = stream.ReadInt16();
                TextureRefsCount = stream.ReadInt16();
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

                SubstanceRefsCount = stream.ReadInt32();
                SubstanceRefsOffset = stream.ReadInt32();

                SubstancesCount = stream.ReadInt32();
                SubstancesOffset = stream.ReadInt32();

                if (HasPalettes)
                {
                    PaletteRefsCount = stream.ReadInt32();
                    PaletteRefsOffset = stream.ReadInt32();

                    PalettesCount = stream.ReadInt32();
                    PalettesOffset = stream.ReadInt32();
                }

                TextureRefsCount = stream.ReadInt32();
                TextureRefsOffset = stream.ReadInt32();

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
                stream.Write((short)SubstanceRefsCount);
                stream.Write((short)SubstancesCount);
                stream.Write((short)TextureRefsCount);
                stream.Write((short)TexturesCount);
                stream.Write((short)Version);
            }
            else
            {
                stream.Write(Version);

                stream.Write(MaterialsCount);
                stream.Write(MaterialsOffset);
                stream.Write(SubstanceRefsCount);
                stream.Write(SubstanceRefsOffset);
                stream.Write(SubstancesCount);
                stream.Write(SubstancesOffset);
                
                if (HasPalettes)
                {
                    stream.Write(PaletteRefsCount);
                    stream.Write(PaletteRefsOffset);

                    stream.Write(PalettesCount);
                    stream.Write(PalettesOffset);
                }

                stream.Write(TextureRefsCount);
                stream.Write(TextureRefsOffset);
                stream.Write(TexturesCount);
                stream.Write(TexturesOffset);

                stream.Write(TextureDataOffset);
                stream.Write(DataSize);
            }
        }

        public MaterialPackageData(MaterialPackageType packageType) : this(packageType, false) { }
        public MaterialPackageData(MaterialPackageType packageType, bool hasPalettes)
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
            
            // Xbox always has palette info
            HasPalettes = hasPalettes | (PackageType == MaterialPackageType.Xbox);

            MaterialsCount = 0;
            MaterialsOffset = 0;

            SubstancesCount = 0;
            SubstancesOffset = 0;

            SubstanceRefsCount = 0;
            SubstanceRefsOffset = 0;

            PaletteRefsCount = 0;
            PaletteRefsOffset = 0;

            PalettesCount = 0;
            PalettesOffset = 0;

            TexturesCount = 0;
            TexturesOffset = 0;

            TextureRefsCount = 0;
            TextureRefsOffset = 0;

            TextureDataOffset = 0;
            DataSize = 0;
        }

        public MaterialPackageData(MaterialPackageType packageType, int nMaterials, int nSubMaterials, int nTextures)
            : this(packageType, nMaterials, nSubMaterials, nTextures, false)
        { }

        public MaterialPackageData(MaterialPackageType packageType, int nMaterials, int nSubMaterials, int nTextures, bool hasPalettes)
            : this(packageType, hasPalettes)
        {
            MaterialsCount = nMaterials;

            SubstanceRefsCount = nSubMaterials;
            SubstancesCount = nSubMaterials;

            TextureRefsCount = nTextures;
            TexturesCount = nTextures;

            GenerateOffsets();
        }

        public MaterialPackageData(MaterialPackageType packageType, Stream stream) : this(packageType, stream, false) { }
        public MaterialPackageData(MaterialPackageType packageType, Stream stream, bool hasPalettes) : this(packageType, hasPalettes)
        {
            Read(stream);
        }
    }
}
