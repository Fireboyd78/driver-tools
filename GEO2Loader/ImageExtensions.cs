using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GEO2Loader
{
    public static class ImageExtensions
    {
        public static byte[] ToByteArray(this Bitmap image, PixelFormat format)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, format);

            int length = data.Stride * data.Height;

            byte[] pixels = new byte[length];

            Marshal.Copy(data.Scan0, pixels, 0, length);

            image.UnlockBits(data);

            return pixels;
        }

        public static void ReplaceBytes(this Bitmap image, byte[] modifyBytes, PixelFormat format)
        {
            ReplaceBytes(image, modifyBytes, 0, format);
        }

        public static void ReplaceBytes(this Bitmap image, byte[] modifyBytes, int where, PixelFormat format)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, format);

            int length = data.Stride * data.Height;

            Marshal.Copy(modifyBytes, where, data.Scan0, length);

            image.UnlockBits(data);
        }
    }
}
