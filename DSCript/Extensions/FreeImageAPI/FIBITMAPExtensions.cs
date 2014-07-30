using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace FreeImageAPI
{
    public static class FIBITMAPExtensions
    {
        /// <summary>
        /// Deletes the <see cref="FIBITMAP"/> from memory. Returns the result of the operation.
        /// </summary>
        /// <returns>True if the operation succeeded; otherwise, false.</returns>
        public static bool Unload(this FIBITMAP @this)
        {
            FreeImage.Unload(@this);
            @this.SetNull();

            return @this.IsNull;
        }

        /// <summary>
        /// Converts the <see cref="FIBITMAP"/> to a .NET <see cref="System.Drawing.Bitmap"/>.
        /// <para>If <paramref name="unload"/> is set to <b>true</b>, the original bitmap will be unloaded from memory.</para>
        /// </summary>
        /// <returns>A .NET <see cref="System.Drawing.Bitmap"/>.</returns>
        public static Bitmap ToBitmap(this FIBITMAP @this, bool unload = false)
        {
            if (@this.IsNull)
                return null;

            var bitmap = FreeImage.GetBitmap(@this);

            if (unload)
                @this.Unload();

            return bitmap;
        }
    }
}
