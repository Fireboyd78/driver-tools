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

        List<PaletteData> Palettes { get; }

        SubstanceInfo GetData(bool resolve);
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

        public List<PaletteData> Palettes { get; set; }

        public SubstanceInfo GetData(bool resolve)
        {
            var substance = new SubstanceInfo() {
                Bin = (byte)Bin,

                Flags = Flags,

                TS1 = (byte)(Mode & 0xFF),
                TS2 = (byte)(Mode >> 8),
                TS3 = (byte)(Type & 0xFF),

                TextureFlags = (byte)ExtraFlags,
            };

            if (resolve)
                SubstanceInfo.Resolve(ref substance);

            return substance;
        }

        public SubstanceDataPC() : base()
        {
            Palettes = new List<PaletteData>();
        }
    }
}
