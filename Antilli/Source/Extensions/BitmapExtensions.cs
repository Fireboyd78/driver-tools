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
using System.Windows.Media.Imaging;

using Interop = System.Windows.Interop;

namespace Antilli
{
    public static class BitmapExtensions
    {
        // Source: http://stackoverflow.com/a/1470182

        /// <summary>
        /// Converts a <see cref="System.Drawing.Bitmap"/> into a WPF <see cref="BitmapSource"/>.
        /// </summary>
        /// <remarks>
        /// Uses GDI to do the conversion. Hence the call to the marshalled DeleteObject.
        /// </remarks>
        /// <param name="source">The source bitmap.</param>
        /// <param name="flags">The flags to use when loading the bitmap.</param>
        /// <returns>A BitmapSource</returns>
        public static BitmapSource ToBitmapSource(this Bitmap source, BitmapSourceLoadFlags flags = BitmapSourceLoadFlags.Default)
        {
            BitmapSource result = null;

            var color = (flags.HasFlag(BitmapSourceLoadFlags.Transparency))
                            ? Color.Transparent
                            : Color.Black;
            
            IntPtr hBitmap = source.GetHbitmap(color);

            try
            {
                result = Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Win32Exception)
            {
                result = null;
            }
            finally
            {
                NativeMethods.DeleteObject(hBitmap);
            }

            return result;
        }
    }
}
