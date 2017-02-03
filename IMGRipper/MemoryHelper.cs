using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMGRipper
{
    public static class MemoryHelper
    {
        /// <summary>
        /// Returns the result of aligning a value to a specified alignment.
        /// </summary>
        /// <param name="value">The value to align.</param>
        /// <param name="alignment">The byte-alignment.</param>
        /// <returns>The byte-aligned value; If the alignment is zero, the unmodified value.</returns>
        public static int Align(int value, int alignment)
        {
            return (alignment != 0) ? (value + (alignment - (value % alignment)) % alignment) : value;
        }

        /// <summary>
        /// Returns the result of aligning a value to a specified alignment.
        /// </summary>
        /// <param name="value">The value to align.</param>
        /// <param name="alignment">The byte-alignment.</param>
        /// <returns>The byte-aligned value; If the alignment is zero, the unmodified value.</returns>
        public static long Align(long value, long alignment)
        {
            return (alignment != 0) ? (value + (alignment - (value % alignment)) % alignment) : value;
        }
    }
}
