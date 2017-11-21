using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using DSCript;
using DSCript.Models;

namespace GMC2Snooper
{
    public enum MaterialType
    {
        Normal,
        Animated,
    }

    public enum SubstanceType
    {
        Normal,     // One texture
        Multiple,   // Many textures
        Blended,    // One texture, many CLUTs
    }

    public enum TextureCompType
    {
        RGBA    = 0,
        PAL8    = 1,
        PAL4    = 2,
        VQ2     = 3,
        VQ4     = 4,
        HY2     = 5,
        HY2f    = 6,
        VQ4f    = 7,
    }
    
    public interface ICopyDetail<T>
        where T : class
    {
        T Copy();
    }
    
    public class MaterialDataPS2
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Detail : ICopyDetail<MaterialDataPS2>
        {
            public MaterialDataPS2 Copy()
            {
                return new MaterialDataPS2() {
                    Type = (MaterialType)Type,
                    AnimationSpeed = AnimationSpeed,

                    Substances = new List<SubstanceDataPS2>(NumSubstances),
                };
            }

            public byte NumSubstances;
            public byte Type;
            
            public float AnimationSpeed;

            public int Reserved;
            
            public int SubstanceRefsOffset;
        }
        
        public MaterialType Type { get; set; }

        public float AnimationSpeed { get; set; } = 25.0f;

        public List<SubstanceDataPS2> Substances { get; set; }

        public MaterialDataPS2()
        {
            Substances = new List<SubstanceDataPS2>();
        }
    }
    
    public class SubstanceDataPS2
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Detail : ICopyDetail<SubstanceDataPS2>
        {
            public SubstanceDataPS2 Copy()
            {
                return new SubstanceDataPS2() {
                    Type = (SubstanceType)Type,
                    Bin = Bin,
                    Flags = Flags,

                    Textures = new List<TextureDataPS2>(NumTextures),
                };
            }

            public byte Type;
            public byte Bin;
            public byte NumTextures;
            public byte Flags;

            public int Reserved;

            public int TextureRefsOffset;
        }

        public SubstanceType Type { get; set; }
        public int Bin { get; set; }

        public int Flags { get; set; }
        
        public List<TextureDataPS2> Textures { get; set; }
        
        public SubstanceDataPS2()
        {
            Textures = new List<TextureDataPS2>();
        }
    }
    
    public class TextureDataPS2
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Detail : ICopyDetail<TextureDataPS2>
        {
            public TextureDataPS2 Copy()
            {
                return new TextureDataPS2() {
                    GUID        = GUID,

                    CompType    = (TextureCompType)CompType,
                    MipMaps     = MipMaps,
                    Regs        = Regs,

                    Width       = Width,
                    Height      = Height,

                    K           = K,

                    DataOffset  = DataOffset,
                    Unknown     = Unk_18,

                    CLUTs       = new List<int>(Pixmaps),
                };
            }

            public long GUID;

            public byte Pixmaps;
            public byte CompType;
            public byte MipMaps;
            public byte Regs;

            public short Width;
            public short Height;

            public short K;

            public byte Unk_12; // flags?
            public byte Unk_13;

            public int DataOffset;

            public int Unk_18;

            /* list of pixmaps follow */   
        }

        public long GUID { get; set; }
        
        public TextureCompType CompType { get; set; }

        public int MipMaps { get; set; }
        public int Regs { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public int K { get; set; }

        public int DataOffset { get; set; }

        public int Unknown { get; set; }

        public List<int> CLUTs { get; set; }
        
        public TextureDataPS2()
        {
            CLUTs = new List<int>();
        }
    }
}
