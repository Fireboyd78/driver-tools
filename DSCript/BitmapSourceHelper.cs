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

namespace DSCript
{
    [Flags]
    public enum BitmapSourceLoadFlags : int
    {
        None = 0,
        AlphaBlend = 1,
        AlphaOnly = 2
    }

    public static class BitmapSourceHelper
    {
        public static BitmapSource GetBitmapSource(byte[] buffer, BitmapSourceLoadFlags flags)
        {
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                try
                {
                    FREE_IMAGE_FORMAT format = FreeImage.GetFileTypeFromStream(stream);

                    if (format == FREE_IMAGE_FORMAT.FIF_UNKNOWN)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        DSC.Log("{0:X}{1:X}{2:X}{3:X} magic is not DDS?", stream.ReadByte(), stream.ReadByte(), stream.ReadByte(), stream.ReadByte());
                        return null;
                    }

                    FIBITMAP bmap = FreeImage.LoadFromStream(stream, FREE_IMAGE_LOAD_FLAGS.RAW_DISPLAY, ref format);

                    bool useAlpha = flags.HasFlag(BitmapSourceLoadFlags.AlphaBlend);
                    bool alphaOnly = flags.HasFlag(BitmapSourceLoadFlags.AlphaOnly);

                    if (alphaOnly)
                        bmap = FreeImage.GetChannel(bmap, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);

                    bmap = (useAlpha) ? FreeImage.ConvertTo32Bits(bmap) : FreeImage.ConvertTo24Bits(bmap);

                    using (Bitmap bitmap = FreeImage.GetBitmap(bmap))
                    {
                        return bitmap.ToBitmapSource();
                    }
                }
                catch (BadImageFormatException)
                {
                    return null;
                }
            }
        }
    }
}
