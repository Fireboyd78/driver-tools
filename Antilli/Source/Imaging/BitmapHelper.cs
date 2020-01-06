using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FreeImageAPI;

namespace DSCript
{
    public sealed class BitmapHelper
    {
        /// <summary>
        /// Loads a <see cref="FIBITMAP"/> from the specified buffer.
        /// <para>See <see cref="FreeImage.LoadFromStream(Stream)"/> for more information.</para>
        /// </summary>
        /// <param name="buffer">The buffer to load from.</param>
        public static FIBITMAP GetFIBITMAP(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                return FreeImage.LoadFromStream(stream);
            }
        }

        /// <summary>
        /// See <see cref="FreeImage.LoadFromStream(Stream)"/> for more information.
        /// </summary>
        public static FIBITMAP GetFIBITMAP(Stream stream)
        {
            return FreeImage.LoadFromStream(stream);
        }
    }
}
