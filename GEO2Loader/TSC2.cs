using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GEO2Loader
{
    public static class TSC2TextureExtensions
    {
        public static void UnSwizzle(this TSC2Texture texture, byte[] TSC2Buffer)
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

        public static void ReadCLUT(this TSC2Texture texture, byte[] TSC2Buffer)
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

    public sealed class Texture8bpp : TSC2Texture
    {
        public override PixelFormat PixelFormat
        {
            get
            {
                return PixelFormat.Format8bppIndexed;
            }
        }

        public Texture8bpp()
        {
        }

        public Texture8bpp(TSC2Block.TextureInfo textureDefinition)
        {
            TextureInfo = textureDefinition;
            Bitmap = new Bitmap(TextureInfo.Width, TextureInfo.Height);
        }

        public Texture8bpp(TSC2Block.TextureInfo textureDefinition, byte[] TSC2Buffer)
        {
            TextureInfo = textureDefinition;

            Console.WriteLine("Creating texture with width: {0}, height: {1}", textureDefinition.Width, textureDefinition.Height);

            Bitmap = new Bitmap((int)textureDefinition.Width, (int)textureDefinition.Height, PixelFormat);

            this.UnSwizzle(TSC2Buffer);
            this.ReadCLUT(TSC2Buffer);
        }
    }

    public class TSC2Block
    {
        public struct Material
        {
            public uint SubMaterialsCount;
            
            public const uint Unk = 0x41C80000;
            public const uint Pad = 0x0;

            public uint SubMaterialsOffset;
        }

        public struct SubMaterial
        {
            public uint Offset;

            public ushort Unk1;
            public ushort Unk2;

            public const uint Pad = 0x0;

            public uint TexInfoOffset;
        }

        public struct TextureInfo
        {
            public uint Offset;

            public float UnkFloat1;

            public ushort Unk1;
            public ushort Unk2;

            public ushort Flags;
            public ushort UnkFlags;

            public ushort Width;
            public ushort Height;

            public uint UnkSize;
            public uint TexDataOffset;

            public const uint Pad = 0x0;

            public uint PaletteOffset;
            public uint TextureOffset;

            public uint TexUnknown;
        }

        public uint Offset { get; set; }

        public const BlockType Magic = BlockType.TSC2;

        public ushort MatCount { get; set; }
        public ushort SubMatOffsetCount { get; set; }
        public ushort SubMatCount { get; set; }
        public ushort TexInfoOffsetCount { get; set; }
        public ushort TexInfoCount { get; set; }

        public const ushort Version = 0x2;

        public List<Material> Materials = new List<Material>();
        public List<SubMaterial> SubMaterials = new List<SubMaterial>();
        public List<TextureInfo> TexturesInfo = new List<TextureInfo>();
        public List<TSC2Texture> Textures = new List<TSC2Texture>();
    }
}
