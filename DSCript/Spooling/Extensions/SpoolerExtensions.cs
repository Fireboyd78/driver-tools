using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public static class SpoolerExtensions
    {
        public static Spooler GetFirstChild(this SpoolablePackage @this, int context)
        {
            return @this.Children.FirstOrDefault((s) => (s.Context == context));
        }

        public static T GetFirstChild<T>(this SpoolablePackage @this, int context)
            where T : Spooler
        {
            return @this.Children.FirstOrDefault((s) => (s is T) && (s.Context == context)) as T;
        }

        /// <summary>
        /// Returns the first child matching the specified <seealso cref="DSCript.ChunkType"/>.
        /// </summary>
        /// <param name="this">The <see cref="DSCript.Spooling.SpoolablePackage"/>.</param>
        /// <param name="chunk">The <see cref="DSCript.ChunkType"/> to find.</param>
        /// <returns></returns>
        public static Spooler GetFirstChild(this SpoolablePackage @this, ChunkType chunk)
        {
            return @this.GetFirstChild((int)chunk);
        }
        
        public static T GetFirstChild<T>(this SpoolablePackage @this, ChunkType chunk)
            where T : Spooler
        {
            return @this.GetFirstChild<T>((int)chunk);
        }
    }
}
