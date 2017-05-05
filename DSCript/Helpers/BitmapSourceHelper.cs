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
        /// <summary>
        /// Loads a BitmapSource image with the default settings.
        /// </summary>
        Default         = 0,

        /// <summary>
        /// Loads a BitmapSource image with transparency applied.
        /// </summary>
        Transparency    = 1,

        /// <summary>
        /// Loads a BitmapSource image with only the Alpha channel visible.
        /// </summary>
        AlphaMask       = 2
    }   

    public static class BitmapSourceHelper
    {
        public static BitmapSource GetBitmapSource(string file, BitmapSourceLoadFlags flags = BitmapSourceLoadFlags.Default)
        {
            if (!File.Exists(file))
                return null;

            return GetBitmapSource(File.ReadAllBytes(file), flags);
        }

        public static BitmapSource GetBitmapSource(byte[] buffer, BitmapSourceLoadFlags flags)
        {
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                try
                {
                    FREE_IMAGE_FORMAT format = FreeImage.GetFileTypeFromStream(stream);

                    switch (format)
                    {
                    case FREE_IMAGE_FORMAT.FIF_UNKNOWN:
                        {
                            return null;
                        }
                    case FREE_IMAGE_FORMAT.FIF_BMP:
                    case FREE_IMAGE_FORMAT.FIF_GIF:
                    case FREE_IMAGE_FORMAT.FIF_JPEG:
                    case FREE_IMAGE_FORMAT.FIF_PNG:
                    case FREE_IMAGE_FORMAT.FIF_TIFF:
                        {
                            try
                            {
                                using (var bitmap = new Bitmap(stream))
                                {
                                    return bitmap.ToBitmapSource();
                                }
                            }
                            catch
                            {
                                return null;
                            }
                        }
                    }

                    FIBITMAP bmap = FreeImage.LoadFromStream(stream, ref format);

                    if (bmap.IsNull)
                        return null;

                    bool useAlpha = flags.HasFlag(BitmapSourceLoadFlags.Transparency);
                    bool alphaOnly = flags.HasFlag(BitmapSourceLoadFlags.AlphaMask);

                    if (alphaOnly)
                        bmap = FreeImage.GetChannel(bmap, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);

                    bmap = (useAlpha) ? FreeImage.ConvertTo32Bits(bmap) : FreeImage.ConvertTo24Bits(bmap);

                    using (var bitmap = bmap.ToBitmap(true))
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
