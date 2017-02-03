using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GMC2Snooper
{
    /*
    public abstract class TSCTexture : IDisposable
    {
        public Bitmap Bitmap { get; set; }

        public virtual PixelFormat PixelFormat
        {
            get { return PixelFormat.Undefined; }
        }

        public ColorPalette Palette
        {
            get
            {
                if (Bitmap == null) throw new Exception("Cannot retrieve palette from a non-initialized bitmap");
                return Bitmap.Palette;
            }
            set
            {
                if (Bitmap == null) throw new Exception("Cannot replace palette on a non-initialized bitmap");
                Bitmap.Palette = value;
            }
        }

        public byte[] Pixels
        {
            get
            {
                if (Bitmap == null) throw new Exception("Cannot retrieve pixels from a non-initialized bitmap");
                return Bitmap.ToByteArray(PixelFormat);
            }
            set
            {
                if (Bitmap == null) throw new Exception("Cannot replace pixels on a non-initialized bitmap");
                Bitmap.ReplaceBytes(value, PixelFormat);
            }
        }

        public TSCData.Texture TextureInfo { get; set; }

        public void Dispose()
        {
            Bitmap.Dispose();
        }
    }
    */
}
