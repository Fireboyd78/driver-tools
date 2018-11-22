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
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

using FreeImageAPI;

namespace DSCript.Models
{
    public interface ISubstanceData
    {
        int Bin { get; set; }
        int Flags { get; set; }
        
        int Mode { get; set; }
        int Type { get; set; }

        IEnumerable<ITextureData> Textures { get; }

        ITextureData GetTexture(int index);

        string RenderBin { get; }
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

        public int Bin { get; set; }
        public int Flags { get; set; }

        public int Mode { get; set; }
        public int Type { get; set; }

        public string RenderBin
        {
            get
            {
                // what the fuck, guys
                var lookup = new Dictionary<int, string>() {
                    { 0, "ReflectedSky" },                              //  0
                    { 1, "Portal" },                                    //  1
                    
                    { 4, "Building" },                                  // -1
                    { 5, "Clutter" },                                   // -1

                    { 3, "Road" },                                      //  6
                    { 35, "PostRoad" },                                 //  7
                    
                    { 39, "PreWater" },                                 //  9
                    { 21, "FarWater" },                                 // 10
                    { 23, "CarInterior" },                              // 11
                    { 6, "Car" },                                       // 12
                    
                    { 26, "FarWater_2" },                               // 14
                    { 27, "FarWater_3" },                               // 15
                    { 20, "NearWater" },                                // 16
                    
                    { 25, "CarOverlay" },                               // 17
                    { 22, "GrimeOverlay" },                             // 19

                    { 2, "Sky" },                                       // 21
                    { 24, "LowPoly" },                                  // 22

                    { 34, "GlowingLight" },                             // 25
                    
                    { 37, "DrawAlphaLast" },                            // 28
                    { 30, "FullBrightOverlay" },                        // 29
                    { 7, "Particle" },                                  // 30

                    { 33, "ShadowedParticle" },                         // -1
                    
                    { 8, "MissionIcon" },                               // 32

                    { 12, "OverheadMap_1" },                            // 33
                    { 13, "OverheadMap_2" },                            // 34
                    { 14, "OverheadMap_3" },                            // 35
                    { 15, "OverheadMap_4" },                            // 36
                    { 16, "OverheadMap_5" },                            // 37
                    { 17, "OverheadMap_6" },                            // 38
                    { 18, "OverheadMap_7" },                            // 39
                    { 19, "OverheadMap_8" },                            // 40
                    { 28, "OverheadMap_9" },                            // 41
                    { 29, "OverheadMap_10" },                           // 42
                    { 31, "OverheadMap_11" },                           // 43
                    { 32, "OverheadMap_12" },                           // 44
                    
                    { 40, "Overlay_0_25" },                             // 46
                    { 10, "Overlay_0_5" },                              // 47
                    { 9, "Overlay_1_0" },                               // 48
                    { 11, "Overlay_1_5" },                              // 49
                    { 36, "Overlay_2_0" },                              // -1

                    { 38, "UntexturedSemiTransparent" },                // 51
                    
                    // not used in Driv3r?
                    { 48, "Trees" },

                    { 49, "Menus_0_25" },
                    { 50, "Menus_0_5" },
                    { 51, "Menus_0_75" },
                    { 52, "Menus_1_0" },
                    { 53, "Menus_1_25" },
                    { 54, "Menus_1_5" },
                    { 55, "Menus_1_75" },
                    { 56, "Menus_2_0" },

                    { 57, "Clouds" },
                    { 58, "Hyperlow" },
                    { 59, "LightFlare" },

                    { 60, "OverlayMask_1" },
                    { 61, "OverlayMask_2" },

                    { 62, "TreeWall" },
                    { 63, "BridgeWires" },
                };

                if (lookup.ContainsKey(Bin))
                    return lookup[Bin];

                return "???";
            }
        }

        public List<TTextureData> Textures { get; set; }
        
        public SubstanceDataWrapper()
        {
            Textures = new List<TTextureData>();
        }
    }

    [Flags]
    public enum SubstanceExtraFlags : int
    {
        flag_1              = (1 << 0),
        flag_2              = (1 << 1),

        ColorMask           = (1 << 2),
        Damage              = (1 << 3),
        DamageWithColorMask = (1 << 4),

        flag_32             = (1 << 5),
        flag_64             = (1 << 6),
        flag_128            = (1 << 7),

        ValidMaskBits       = (ColorMask | Damage | DamageWithColorMask),
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
            get { return (((Flags & 0x4) != 0) || (Bin == 37)); }
        }
        
        public virtual bool IsEmissive
        {
            get { return ((Flags & 0x180) != 0 || (Bin == 30)); }
        }

        public virtual bool IsSpecular
        {
            get { return (((Flags & 0x40) != 0) && (Mode != 0) && (Bin != 37)) || (Mode == 0x201 || Mode == 0x102); }
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

            var rst     = (resolved >> 0) & 0xFF;
            var stage   = (resolved >> 8) & 0xFFFF;
            var flags   = (resolved >> 16) & 0xFFFF;

            var bin = 0;
            var alpha = 0;

            switch (rst)
            {
            case 4:
                {
                    rst = 7;
                    stage = 0;
                    flags = 0;

                    flags |= 2;
                    flags |= 0x80;
                } break;
            case 18:
            case 20:
                rst = 8;
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
            
            var rst = m_BinLookup[Bin, 1];

            if (rst == -1)
            {
                rst = 8;

                if ((Flags & 0x4000) != 0)
                {
                    if (v2 == 0)
                        rst = (Bin == 5) ? 20 : 18;
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

            if ((rst == 7) && ((flags & 0x80) != 0))
            {
                if ((flags & 2) != 0)
                {
                    stage = 0;
                    flags = 0;

                    rst = 4;
                }
            }

            return ((flags << 16) | (stage << 8) | rst);
        }

        public SubstanceDataPC() : base() { }
    }
}
