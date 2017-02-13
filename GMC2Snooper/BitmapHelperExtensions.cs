using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace GMC2Snooper
{
    public static class BitmapHelperExtensions
    {
        public static void Swizzle(this BitmapHelper bitmap, int width, int height, SwizzleType type)
        {
            byte[] buffer = new byte[width * height];
            Swizzle(bitmap, buffer, width, height, type);
        }

        public static void Swizzle(this BitmapHelper bitmap, byte[] buffer, int width, int height, SwizzleType type)
        {
            switch (type)
            {
                case (SwizzleType.Swizzle4bit):
                    buffer = Swizzlers.Swizzle4To32(bitmap.Pixels, width, height);
                    bitmap.Pixels = buffer;
                    break;
                case (SwizzleType.Swizzle8bit):
                    buffer = Swizzlers.Swizzle8To32(bitmap.Pixels, width, height);
                    bitmap.Pixels = buffer;
                    break;
            }
        }

        public static void Unswizzle(this BitmapHelper bitmap, int width, int height, SwizzleType type)
        {
            Unswizzle(bitmap, width, height, type, 0);
        }

        public static void Unswizzle(this BitmapHelper bitmap, int width, int height, SwizzleType type, int where)
        {
            byte[] buffer = new byte[width * height];
            Unswizzle(bitmap, buffer, width, height, type, where);
        }

        public static void Unswizzle(this BitmapHelper bitmap, byte[] buffer, int width, int height, SwizzleType type)
        {
            Unswizzle(bitmap, buffer, width, height, type, 0);
        }

        public static void Unswizzle(this BitmapHelper bitmap, byte[] buffer, int width, int height, SwizzleType type, int where)
        {
            switch (type)
            {
                case (SwizzleType.Swizzle4bit):
                    buffer = Swizzlers.UnSwizzle4(bitmap.Pixels, width, height, where);
                    bitmap.Pixels = buffer;
                    break;
                case (SwizzleType.Swizzle8bit):
                    buffer = Swizzlers.UnSwizzle8(bitmap.Pixels, width, height, where);
                    bitmap.Pixels = buffer;
                    break;
            }
        }

        public static void WritePixelsToFile(this BitmapHelper bitmap, string @toFile)
        {
            using (Stream file = new FileStream(toFile, FileMode.Create, FileAccess.Write, FileShare.Read, bitmap.Pixels.Length))
            {
                file.Write(bitmap.Pixels, 0, bitmap.Pixels.Length);
            }
        }

        public static void Read4bppCLUT(this BitmapHelper bitmap, byte[] buffer, int where)
        {
            ColorPalette palette = bitmap.Palette;

            byte[][] clut = new byte[16][];

            int pointer = 0;

            for (int i = 0; i < 16; i++)
            {
                uint pal = BitConverter.ToUInt16(buffer, where + pointer);

                clut[i] = new byte[4];

                //clut[i][0] = (byte)(((pal >> 24) & 0xFF) == 0x80 ? 0xFF : (((pal >> 24) & 0x7F) << 1));
                clut[i][0] = 0xFF;
                clut[i][1] = (byte)(((pal >> 0) & 0xF) * 16.999f);
                clut[i][2] = (byte)(((pal >> 4) & 0xF) * 16.999f);
                clut[i][3] = (byte)(((pal >> 8) & 0xF) * 16.999f);

                pointer += 2;
            }

            byte[][] clutCopy = new byte[16][];
            Array.Copy(clut, clutCopy, 16);

            for (int i = 0; i < 16; i++)
            {
                var entry = ((((i >> 1) & 0x1) * 4) | ((i & 0x1) * 4)) & 0xF;

                clut[i] = clutCopy[entry];
                
                palette.Entries[i] =
                    Color.FromArgb(
                        clut[i][0],
                        clut[i][1],
                        clut[i][2],
                        clut[i][3]
                    );
            }

            bitmap.Palette = palette;
        }

        public static void Read8bppCLUT(this BitmapHelper bitmap, byte[] buffer, int where)
        {
            ColorPalette palette = bitmap.Palette;

            byte[][] clut = new byte[256][];

            int pointer = 0;

            for (int i = 0; i < 256; i++)
            {
                uint pal = BitConverter.ToUInt32(buffer, where + pointer);

                clut[i] = new byte[4];

                //clut[i][0] = (byte)(((pal >> 24) & 0xFF) == 0x80 ? 0xFF : (((pal >> 24) & 0x7F) << 1));
                clut[i][0] = 0xFF;
                clut[i][1] = (byte)(pal & 0xFF);
                clut[i][2] = (byte)((pal >> 8) & 0xFF);
                clut[i][3] = (byte)((pal >> 16) & 0xFF);

                pointer += 4;
            }

            byte[][] clutCopy = new byte[256][];
            Array.Copy(clut, clutCopy, 256);

            for (int i = 0; i < 256; i++)
            {
                var entry = (i & 0xE7);

                entry |= ((i >> 4) & 0x1) << 3;
                entry |= ((i >> 3) & 0x1) << 4;

                clut[i] = clutCopy[entry];

                palette.Entries[i] =
                    Color.FromArgb(
                        clut[i][0],
                        clut[i][1],
                        clut[i][2],
                        clut[i][3]
                    );
            }

            bitmap.Palette = palette;
        }

        public static void CLUTFromRGB(this BitmapHelper bitmap, byte[] buffer, int wR, int wG, int wB)
        {
            ColorPalette palette = bitmap.Palette;

            byte[][] clut = new byte[256][];

            int pointer = 0;

            for (int i = 0; i < 256; i++)
            {
                uint palR = BitConverter.ToUInt32(buffer, wR + pointer);
                uint palG = BitConverter.ToUInt32(buffer, wG + pointer);
                uint palB = BitConverter.ToUInt32(buffer, wB + pointer);

                clut[i] = new byte[4];

                // clut[i][0] = (byte)(((palR >> 24) & 0xFF) == 0x80 ? 0xFF : (((palR >> 24) & 0x7F) << 1));
                clut[i][0] = 0xFF;
                clut[i][1] = (byte)(palR & 0xFF);
                clut[i][2] = (byte)((palG >> 8) & 0xFF);
                clut[i][3] = (byte)((palB >> 16) & 0xFF);

                pointer += 4;
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

            bitmap.Palette = palette;
        }

        public static void CLUTFromAlpha(this BitmapHelper bitmap, byte[] buffer, int wA)
        {
            ColorPalette palette = bitmap.Palette;

            byte[][] clut = new byte[256][];

            int pointer = 0;

            for (int i = 0; i < 256; i++)
            {
                uint pal = BitConverter.ToUInt32(buffer, wA + pointer);

                clut[i] = new byte[4];

                clut[i][0] = (byte)(((pal >> 24) & 0xFF) == 0x80 ? 0xFF : (((pal >> 24) & 0x7F) << 1));
                clut[i][1] = 0xFF;
                clut[i][2] = 0xFF;
                clut[i][3] = 0xFF;

                pointer += 4;
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

            bitmap.Palette = palette;
        }
    }
}
