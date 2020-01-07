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

namespace Antilli
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

            using (var fs = File.Open(file, FileMode.Open, FileAccess.Read))
            {
                return GetBitmapSource(fs, flags);
            }
        }

        public static BitmapSource GetBitmapSource(Stream stream, BitmapSourceLoadFlags flags)
        {
            try
            {
                var format = FreeImage.GetFileTypeFromStream(stream);

                switch (format)
                {
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
                                return bitmap.ToBitmapSource(flags);
                            }
                        }
                        catch
                        {
                            return null;
                        }
                    }
                case FREE_IMAGE_FORMAT.FIF_UNKNOWN:
                    return null;
                }

                FIBITMAP dib = FreeImage.LoadFromStream(stream, ref format);

                if (dib.IsNull)
                    return null;

                var image = dib;
                var unload = false;

                if (flags.HasFlag(BitmapSourceLoadFlags.AlphaMask))
                {
                    image = FreeImage.GetChannel(dib, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
                    unload = true;
                }

                var bmap = (flags.HasFlag(BitmapSourceLoadFlags.Transparency))
                    ? FreeImage.ConvertTo32Bits(image)
                    : FreeImage.ConvertTo24Bits(image);

                try
                {
                    using (var bitmap = FreeImage.GetBitmap(bmap))
                    {
                        return bitmap.ToBitmapSource(flags);
                    }
                }
                finally
                {
                    FreeImage.UnloadEx(ref bmap);

                    if (unload)
                        FreeImage.UnloadEx(ref image);

                    FreeImage.UnloadEx(ref dib);
                }
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }
    }
}
