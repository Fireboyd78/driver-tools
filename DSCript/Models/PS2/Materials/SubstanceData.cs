using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DSCript.Models
{
    public enum PS2SubstanceType
    {
        Normal,     // One texture
        Multiple,   // Many textures
        Blended,    // One texture, many CLUTs
    }

    public class SubstanceDataPS2
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Detail : IClassDetail<SubstanceDataPS2>
        {
            public SubstanceDataPS2 ToClass()
            {
                return new SubstanceDataPS2() {
                    Type = (PS2SubstanceType)Type,
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

        public PS2SubstanceType Type { get; set; }
        public int Bin { get; set; }

        public int Flags { get; set; }
        
        public List<TextureDataPS2> Textures { get; set; }
        
        public SubstanceDataPS2()
        {
            Textures = new List<TextureDataPS2>();
        }
    }
}
