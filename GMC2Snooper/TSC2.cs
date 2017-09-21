using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Models;

namespace GMC2Snooper
{
    /*
    public static class TSC2TextureExtensions
    {
        public static void UnSwizzle(this TSCTexture texture, byte[] TSC2Buffer)
        {
            byte[] texBuffer = new byte[texture.TextureInfo.Width * texture.TextureInfo.Height];

            switch (texture.PixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                    texBuffer = Swizzlers.UnSwizzle8(TSC2Buffer, texture.TextureInfo.Width, texture.TextureInfo.Height, (int)texture.TextureInfo.TextureOffset);
                    texture.Pixels = texBuffer;
                    break;
                case PixelFormat.Format4bppIndexed:
                    throw new NotImplementedException();
            }
        }

        public static void ReadCLUT(this TSCTexture texture, byte[] TSC2Buffer)
        {
            ColorPalette palette = texture.Palette;

            byte[][] clut = new byte[256][];
            int ptr = 0;

            for (int i = 0; i < 256; i++)
            {
                uint p = BitConverter.ToUInt32(TSC2Buffer, ((int)texture.TextureInfo.PaletteOffset + ptr));

                clut[i] = new byte[4];

                //clut[i][0] = (byte)(((p >> 24) & 0xFF) == 0x80 ? 0xFF : (((p >> 24) & 0x7F) << 1));
                clut[i][0] = 0xFF;
                clut[i][1] = (byte)(p & 0xFF);
                clut[i][2] = (byte)((p >> 8) & 0xFF);
                clut[i][3] = (byte)((p >> 16) & 0xFF);

                ptr += 4;
            }

            byte[][] clutCopy = new byte[256][];
            Array.Copy(clut, clutCopy, 256);

            for (int i = 0; i < 256; i++)
            {
                byte entry = (byte)((i & 0xE7) | (((i >> 4) & 0x1) << 3) | (((i >> 3) & 0x1) << 4));

                clut[i] = clutCopy[entry];

                palette.Entries[i] =
                    Color.FromArgb(
                        clut[i][0],
                        clut[i][1],
                        clut[i][2],
                        clut[i][3]
                    );
            }

            texture.Palette = palette;
        }
    }
    
    public sealed class TSCTexture8bpp : TSCTexture
    {
        public override PixelFormat PixelFormat
        {
            get
            {
                return PixelFormat.Format8bppIndexed;
            }
        }

        public TSCTexture8bpp()
        {
        }

        public TSCTexture8bpp(TSCData.Texture textureDefinition)
        {
            TextureInfo = textureDefinition;
            Bitmap = new Bitmap(TextureInfo.Width, TextureInfo.Height);
        }

        public TSCTexture8bpp(TSCData.Texture textureDefinition, byte[] TSC2Buffer)
        {
            TextureInfo = textureDefinition;

            Console.WriteLine("Creating texture with width: {0}, height: {1}", textureDefinition.Width, textureDefinition.Height);

            Bitmap = new Bitmap((int)textureDefinition.Width, (int)textureDefinition.Height, PixelFormat);

            this.UnSwizzle(TSC2Buffer);
            this.ReadCLUT(TSC2Buffer);
        }
    }
    */

    public class MaterialDataPS2
    {
        public List<SubstanceDataPS2> Substances { get; set; }

        public bool Animated { get; set; } = false;
        public double AnimationSpeed { get; set; } = 25.0;

        public MaterialDataPS2()
        {
            Substances = new List<SubstanceDataPS2>();
        }
    }

    public class SubstanceDataPS2
    {
        public int Mode { get; set; }
        public int Flags { get; set; }

        public int Type { get; set; }
        
        public List<TextureDataPS2> Textures { get; set; }

        public SubstanceDataPS2()
        {
            Textures = new List<TextureDataPS2>();
        }
    }

    public enum TextureFormatType
    {
        Indexed8bpp = 1,
        Indexed4bpp = 2,

        Quantized8bpp = 3,
        Quantized4bpp = 4,
    }

    public class TextureDataPS2
    {
        public long Reserved { get; set; }

        public int Modes { get; set; }

        /*
            PAL8    = 1
            PAL4    = 2
            VQ2     = 3 (?)
            VQ4     = 4 (?)
            HY2     = 5 (?)
            VQ4f    = 6 (?)
        */
        public int Type { get; set; }

        public int MipMaps { get; set; }
        public int Flags { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public int Unknown1 { get; set; }

        public int DataOffset { get; set; }

        public int Unknown2 { get; set; }

        public List<int> CLUTs { get; set; }
        
        public TextureDataPS2()
        {
            CLUTs = new List<int>();
        }
    }
}
