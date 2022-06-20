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

        int TS1 { get; set; }
        int TS2 { get; set; }
        int TS3 { get; set; }

        int TextureFlags { get; set; }

        [Obsolete("Replace this ASAP !!!")]
        int Mode { get; set; }

        [Obsolete("Replace this ASAP !!!")]
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

        public int TS1 { get; set; }
        public int TS2 { get; set; }
        public int TS3 { get; set; }

        public int TextureFlags { get; set; }

        public int Mode
        {
            get { return (TS1 | (TS2 << 8)); }
            set
            {
                var val = (ushort)value;

                TS1 = val & 0xFF;
                TS2 = (val >> 8) & 0xFF;
            }
        }

        public int Type
        {
            get { return (TS3 | (TextureFlags << 8)); }
            set
            {
                var val = (ushort)value;

                TS3 = val & 0xFF;
                TextureFlags = (val >> 8) & 0xFF;
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
        None                            = 0,

        FLAG_1                          = (1 << 0),
        BumpMap                         = (1 << 1),

        ColorMask                       = (1 << 2),
        Damage                          = (1 << 3),
        DamageAndColorMask              = (Damage | ColorMask),
        DamageAndColorMask_AlphaMaps    = (1 << 4),

        FLAG_32                         = (1 << 5),
        FLAG_64                         = (1 << 6),
        FLAG_128                        = (1 << 7),

        DPL_SwapDamageAndColorMaskBits  = (1 << 15),
        DPL_Damage                      = (ColorMask | DPL_SwapDamageAndColorMaskBits),
        DPL_ColorMask                   = (Damage | DPL_SwapDamageAndColorMaskBits),
        DPL_DamageAndColorMask          = (Damage | ColorMask | DPL_SwapDamageAndColorMaskBits),

        ValidMaskBits                   = (ColorMask | Damage | DamageAndColorMask_AlphaMaps),
    }

    public interface ISubstanceDataPC
    {
        bool HasAlpha { get; }

        bool IsEmissive { get; }
        bool IsSpecular { get; }

        List<PaletteData> Palettes { get; }

        SubstanceInfo GetData(bool resolve);
    }

    public class SubstanceDataPC : SubstanceDataWrapper<TextureDataPC>, ISubstanceDataPC, ICopyCat<SubstanceDataPC>
    {
        bool ICopyCat<SubstanceDataPC>.CanCopy(CopyClassType copyType)                          => true;
        bool ICopyCat<SubstanceDataPC>.CanCopyTo(SubstanceDataPC obj, CopyClassType copyType)   => true;

        bool ICopyCat<SubstanceDataPC>.IsCopyOf(SubstanceDataPC obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        SubstanceDataPC ICopyClass<SubstanceDataPC>.Copy(CopyClassType copyType)
        {
            var substance = new SubstanceDataPC();

            CopyTo(substance, copyType);

            return substance;
        }

        public void CopyTo(SubstanceDataPC obj, CopyClassType copyType)
        {
            obj.Bin = Bin;
            obj.Flags = Flags;

            obj.TS1 = TS1;
            obj.TS2 = TS2;
            obj.TS3 = TS3;

            obj.TextureFlags = TextureFlags;

            var textures = new List<TextureDataPC>();
            var palettes = new List<PaletteData>();

            if (copyType == CopyClassType.DeepCopy)
            {
                // copy textures
                foreach (var _texture in Textures)
                {
                    // DEEP COPY: all new data down the line
                    var texture = CopyCatFactory.GetCopy(_texture, CopyClassType.DeepCopy);

                    textures.Add(texture);
                }

                // copy palettes
                foreach (var _palette in Palettes)
                {
                    var palette = _palette.Clone();

                    palettes.Add(palette);
                }
            }
            else
            {
                // reuse texture/palette references
                textures.AddRange(Textures);
                palettes.AddRange(Palettes);
            }

            obj.Textures = textures;
            obj.Palettes = palettes;
        }

        public virtual bool HasAlpha
        {
            get
            {
                if ((Flags & 0x4) != 0)
                    return true;

                if (!IsSpecular)
                {
                    if ((Flags & 0x40) != 0 && !((SubstanceExtraFlags)TextureFlags).HasFlag(SubstanceExtraFlags.BumpMap))
                        return true;

                    switch (Bin)
                    {
                    case RenderBinType.Clutter:
                        // tree wall hack...
                        if ((Flags + TS1 + TS2 + TS3 + TextureFlags) == 0)
                            return true;

                        break;
                    case RenderBinType.GrimeOverlay:
                    case RenderBinType.DrawAlphaLast:
                        return true;
                    }
                }

                return false;
            }
        }
        
        public virtual bool IsEmissive
        {
            get
            {
                if ((Flags & 0x180) != 0)
                    return true;

                return (Bin == RenderBinType.FullBrightOverlay);
            }
        }

        public virtual bool IsSpecular
        {
            get
            {
                var specular = false;

                if ((TS1 == 1 || TS1 == 2) && (TS2 == 2 || TS2 == 1))
                    specular = true;

                if ((Flags & 0x50) != 0)
                {
                    if (Bin == RenderBinType.DrawAlphaLast)
                        specular = false;
                }

                return specular;
            }
        }

        public List<PaletteData> Palettes { get; set; }

        public SubstanceInfo GetData(bool resolve)
        {
            var substance = new SubstanceInfo() {
                Bin = (byte)Bin,

                Flags = Flags,

                TS1 = (byte)TS1,
                TS2 = (byte)TS2,
                TS3 = (byte)TS3,

                TextureFlags = (byte)TextureFlags,
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
