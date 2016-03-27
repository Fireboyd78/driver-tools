using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace GEO2Loader
{
    public static class CLUT
    {
        public static Color ReadARGB8888(uint palette)
        {
            int[] clut = new int[4];

            /* Palette[i][0] = (byte)(((entry >> 24) & 0xFF) == 0x80 ? 0xFF : (((entry >> 24) & 0x7F) << 1));
                * Palette[i][1] = (byte)(entry & 0xFF);
                * Palette[i][2] = (byte)((entry >> 8)  & 0xFF);
                * Palette[i][3] = (byte)((entry >> 16) & 0xFF); */

            clut[0] = (int)(((palette >> 24) & 0xFF) == 0x80 ? 0xFF : (((palette >> 24) & 0x7F) << 1));
            clut[1] = (int)(palette & 0xFF);
            clut[2] = (int)((palette >> 8) & 0xFF);
            clut[3] = (int)((palette >> 16) & 0xFF);

            return Color.FromArgb(clut[0], clut[1], clut[2], clut[3]);
        }

        public static void Read8bppCLUT(byte[] buffer, int where, ColorPalette paletteSrc)
        {
            ColorPalette palette = paletteSrc;

            byte[][] clut = new byte[256][];

            int pointer = 0;

            for (int i = 0; i < 256; i++)
            {
                uint pal = BitConverter.ToUInt32(buffer, where + pointer);

                clut[i] = new byte[4];

                clut[i][0] = (byte)(((pal >> 24) & 0xFF) == 0x80 ? 0xFF : (((pal >> 24) & 0x7F) << 1));
                clut[i][1] = (byte)(pal & 0xFF);
                clut[i][2] = (byte)((pal >> 8) & 0xFF);
                clut[i][3] = (byte)((pal >> 16) & 0xFF);

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

            paletteSrc = palette;
        }
    }
}
