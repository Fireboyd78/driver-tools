using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript
{
    public sealed class Memory
    {
        /// <summary>
        /// Returns the result of aligning an offset to a certain alignment.
        /// </summary>
        /// <param name="offset">The offset to align</param>
        /// <param name="align">The byte-alignment</param>
        /// <returns>The byte-aligned offset.</returns>
        public static long Align(long offset, long align)
        {
            return offset + (align - (offset % align)) % align;
        }

        /// <summary>
        /// Returns the result of aligning an offset to a certain alignment.
        /// </summary>
        /// <param name="offset">The offset to align</param>
        /// <param name="align">The byte-alignment</param>
        /// <returns>The byte-aligned offset.</returns>
        public static int Align(int offset, int align)
        {
            return offset + (align - (offset % align)) % align;
        }

        /// <summary>
        /// Returns the result of aligning an offset to a certain alignment.
        /// </summary>
        /// <param name="offset">The offset to align</param>
        /// <param name="align">The byte-alignment</param>
        /// <returns>The byte-aligned offset.</returns>
        public static uint Align(uint offset, uint align)
        {
            return offset + (align - (offset % align)) % align;
        }
    }
}
