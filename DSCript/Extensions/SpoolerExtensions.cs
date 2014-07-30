using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public static class SpoolerExtensions
    {
        /// <summary>
        /// Returns the first child matching the specified <seealso cref="DSCript.ChunkType"/>.
        /// </summary>
        /// <param name="this">The <see cref="DSCript.Spooling.SpoolablePackage"/>.</param>
        /// <param name="chunk">The <see cref="DSCript.ChunkType"/> to find.</param>
        /// <returns></returns>
        public static Spooler GetFirstChild(this SpoolablePackage @this, ChunkType chunk)
        {
            return @this.Children.FirstOrDefault((s) => s.Magic == (int)chunk);
        }
    }
}
