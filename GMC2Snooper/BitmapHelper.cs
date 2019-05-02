using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace GMC2Snooper
{
    public class BitmapHelper : IDisposable
    {
        public Bitmap Bitmap { get; set; }

        public PixelFormat Format { get; set; }
        
        public byte[] Pixels
        {
            get { return Bitmap.ToByteArray(Format); }
            set
            {
                if (Bitmap == null)
                    throw new Exception("Tried to replace pixels on a non-initialized bitmap");

                Bitmap.ReplaceBytes(value, Format);
            }
        }

        public ColorPalette Palette
        {
            get { return Bitmap.Palette; }
            set
            {
                Bitmap.Palette = value;
            }
        }

        public void Dispose()
        {
            Bitmap.Dispose();
            Format = PixelFormat.Undefined;
        }

        public void Save(string filename)
        {
            Save(filename, ImageFormat.Bmp);
        }

        public void Save(string filename, ImageFormat format)
        {
            Bitmap.Save(filename, format);
        }

        public BitmapHelper(string file, PixelFormat format)
        {
            Bitmap = new Bitmap(Bitmap.FromFile(@file));
            Format = format;
        }

        public BitmapHelper(int width, int height, PixelFormat format)
        {
            Format = format;
            Bitmap = new Bitmap(width, height, format);
        }

        public BitmapHelper(byte[] pixels, int width, int height, PixelFormat format) : this(pixels, width, height, 0, format) { }

        public BitmapHelper(byte[] pixels, int width, int height, int where, PixelFormat format)
        {
            Format = format;
            Bitmap = new Bitmap(width, height, Format);

            Bitmap.ReplaceBytes(pixels, where, format);
        }
    }
}
