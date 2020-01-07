using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DSCript.Models
{
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

    public interface ISubstanceData
    {
        RenderBinType Bin { get; set; }

        int Flags { get; set; }
        
        int Mode { get; set; }
        int Type { get; set; }

        IEnumerable<ITextureData> Textures { get; }

        ITextureData GetTexture(int index);
    }
    
    public abstract class SubstanceDataWrapper<TTextureData> : ISubstanceData
        where TTextureData : ITextureData
    {
        IEnumerable<ITextureData> ISubstanceData.Textures
        {
            get { return (IEnumerable<ITextureData>)Textures; }
        }

        ITextureData ISubstanceData.GetTexture(int index)
        {
            return Textures[index];
        }

        public RenderBinType Bin { get; set; }

        public int Flags { get; set; }

        public int Mode { get; set; }
        public int Type { get; set; }
        
        public List<TTextureData> Textures { get; set; }
        
        public SubstanceDataWrapper()
        {
            Textures = new List<TTextureData>();
        }
    }

    [Flags]
    public enum SubstanceExtraFlags : int
    {
        FLAG_1                      = (1 << 0),
        BumpMap                     = (1 << 1),

        ColorMask                   = (1 << 2),
        Damage                      = (1 << 3),
        DamageAndColorMask          = (Damage | ColorMask),
        DamageAndColorMaskAlphaMaps = (1 << 4),
        
        FLAG_32                     = (1 << 5),
        FLAG_64                     = (1 << 6),
        FLAG_128                    = (1 << 7),

        ValidMaskBits               = (ColorMask | Damage | DamageAndColorMaskAlphaMaps),
    }

    public interface ISubstanceDataPC
    {
        bool HasAlpha { get; }

        bool IsEmissive { get; }
        bool IsSpecular { get; }

        SubstanceExtraFlags ExtraFlags { get; }
        
        int GetCompiledFlags(int resolved);
        int GetResolvedData();
    }

    public class SubstanceDataPC : SubstanceDataWrapper<TextureDataPC>, ISubstanceDataPC
    {
        public virtual bool HasAlpha
        {
            get { return (((Flags & 0x4) != 0) || (Bin == RenderBinType.DrawAlphaLast)); }
        }
        
        public virtual bool IsEmissive
        {
            get { return ((Flags & 0x180) != 0 || (Bin == RenderBinType.FullBrightOverlay)); }
        }

        public virtual bool IsSpecular
        {
            get { return (((Flags & 0x40) != 0) && (Mode != 0) && (Bin != RenderBinType.DrawAlphaLast)) || (Mode == 0x201 || Mode == 0x102); }
        }

        public virtual SubstanceExtraFlags ExtraFlags
        {
            get { return (SubstanceExtraFlags)(Type >> 8); }
        }

        static int[,] m_BinLookup = {
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

        public int GetCompiledFlags(int resolved)
        {
            var result = 0;

            var bin     = (resolved >> 0) & 0xFF;
            var stage   = (resolved >> 8) & 0xFFFF;
            var flags   = (resolved >> 16) & 0xFFFF;
            
            var alpha = 0;

            switch (bin)
            {
            case 4:
                {
                    bin = 7;
                    stage = 0;
                    flags = 0;

                    flags |= 2;
                    flags |= 0x80;
                } break;
            case 18:
            case 20:
                bin = 8;
                result |= 0x4000;
                break;
            }
            
            if (stage != 0)
            {
                var s = (stage - 3);

                if (s == (s & 1))
                    alpha = s;
            }
            
            if ((flags & 1) != 0)
                result |= 0x40000;
            if ((flags & 2) != 0)
                result |= 0x8000;
            if ((flags & 8) != 0)
                result |= 0x10000;
            if ((flags & 0x20) != 0)
                result |= 0x200;
            if ((flags & 0x40) != 0)
                result |= 0x100;
            if ((flags & 0x80) != 0)
                result |= 0x20000;

            if (alpha == 1)
                result |= 0x400;
            
            return result;
        }

        public int GetResolvedData()
        {
            var v1 = (Mode & 0xFF);
            var v2 = 0;

            if ((v1 & 3) != 0)
            {
                var v3 = ((Mode >> 8) & 0xFF);

                if ((v3 & 3) != 0)
                    v2 = 1;
            }
            
            var bin = m_BinLookup[(int)Bin, 1];

            if (bin == -1)
            {
                bin = 8;

                if ((Flags & 0x4000) != 0)
                {
                    if (v2 == 0)
                        bin = (Bin == RenderBinType.Clutter) ? 20 : 18;
                }
            }

            var alpha = 0;
            var flags = 0;
            var stage = 0;

            if ((Flags & 0x1) != 0)
                flags |= 0x40;
            if ((Flags & 0x2) != 0)
                flags |= 0x20;
            if ((Flags & 0x4) != 0)
                alpha = 1;
            if ((Flags & 0x80) != 0)
                flags |= 2;
            if ((Flags & 0x100) != 0)
                flags |= 8;
            if ((Flags & 0x200) != 0)
                flags |= 0x80;
            if ((Flags & 0x400) != 0)
                flags |= 1;

            if (flags != 0)
                stage = ((alpha == 0) ? 3 : 4);

            if ((bin == 7) && ((flags & 0x80) != 0))
            {
                if ((flags & 2) != 0)
                {
                    stage = 0;
                    flags = 0;

                    bin = 4;
                }
            }

            return ((flags << 16) | (stage << 8) | bin);
        }

        public SubstanceDataPC() : base() { }
    }
}
