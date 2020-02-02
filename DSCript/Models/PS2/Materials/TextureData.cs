using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DSCript.Models
{
    public enum PS2TextureCompType
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

    public class TextureDataPS2
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Detail : IClassDetail<TextureDataPS2>
        {
            public TextureDataPS2 ToClass()
            {
                return new TextureDataPS2() {
                    GUID = GUID,

                    CompType = (PS2TextureCompType)CompType,
                    MipMaps = MipMaps,
                    Regs = Regs,

                    Width = Width,
                    Height = Height,

                    K = K,

                    DataOffset = DataOffset,
                    
                    CLUTs = new List<int>(Pixmaps),
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

            public int Reserved;

            /* list of pixmaps follow */   
        }

        public long GUID { get; set; }
        
        public PS2TextureCompType CompType { get; set; }

        public int MipMaps { get; set; }
        public int Regs { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public int K { get; set; }

        public int Flags { get; set; }

        public int DataOffset { get; set; }
        
        public List<int> CLUTs { get; set; }
        
        public TextureDataPS2()
        {
            CLUTs = new List<int>();
        }
    }
}
