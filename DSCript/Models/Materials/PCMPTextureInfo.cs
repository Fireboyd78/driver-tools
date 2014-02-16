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
    public class PCMPTexture
    {
        internal uint BaseOffset { get; set; }

        public byte Unk1 { get; set; }
        public byte Unk2 { get; set; }
        public byte Unk3 { get; set; }
        public byte Unk4 { get; set; }

        public uint CRC32 { get; set; }
        public uint Offset { get; set; }
        public uint Size { get; set; }
        public uint Type { get; set; }

        public ushort Width { get; set; }
        public ushort Height { get; set; }

        public uint Unk5 { get; set; }
        public uint Unk6 { get; set; }

        public byte[] Buffer { get; set; }

        public void ExportFile(string filename)
        {
            using (MemoryStream f = new MemoryStream(Buffer))
            {
                if (!File.Exists(filename))
                    f.WriteTo(File.Create(filename, (int)f.Length));
            }
        }

        public Bitmap GetBitmap()
        {
            return GetBitmap(false, false);
        }

        public Bitmap GetBitmap(bool useAlpha, bool alphaOnly)
        {
            using (MemoryStream stream = new MemoryStream(Buffer))
            {
                FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_DDS;

                try
                {
                    FIBITMAP bmap = FreeImage.LoadFromStream(stream, FREE_IMAGE_LOAD_FLAGS.RAW_DISPLAY, ref format);

                    if (useAlpha && alphaOnly)
                        bmap = FreeImage.GetChannel(bmap, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);

                    bmap = (useAlpha) ? FreeImage.ConvertTo32Bits(bmap) : FreeImage.ConvertTo24Bits(bmap);

                    return FreeImage.GetBitmap(bmap);
                }
                catch (System.BadImageFormatException)
                {
                    return null;
                }
            }
        }

        public BitmapSource GetBitmapSource(bool useAlpha, bool alphaOnly = false)
        {
            Bitmap bitmap = GetBitmap(useAlpha, alphaOnly);

            if (bitmap == null)
                return null;

            BitmapSource bitSrc = bitmap.ToBitmapSource();

            bitmap.Dispose();
            bitmap = null;

            return bitSrc;
        }
    }
}
