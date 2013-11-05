using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Methods
{
    public static class Chunks
    {
        public static string Magic2Str(uint magic)
        {
            return (magic > 255)
                ? new string(new char[]{
                    (char)(magic & 0x000000FF),
                    (char)((magic & 0x0000FF00) >> 8),
                    (char)((magic & 0x00FF0000) >> 16),
                    (char)((magic & 0xFF000000) >> 24)})
                    : (magic == 0)
                        ? "Unified Packager"
                        : magic.ToString("X");
        }

        public enum ByteAlignOptions : uint
        {
            /// <summary>
            /// Aligns data to 16-bytes (e.g. 0x384 > 0x390)
            /// </summary>
            BYTE_ALIGN_16 = 16,

            /// <summary>
            /// Aligns data to 32-bytes (e.g. 0x384 > 0x3A0)
            /// </summary>
            BYTE_ALIGN_32 = 32,
            
            /// <summary>
            /// Aligns data to 512-bytes (e.g. 0x384 > 0x400)
            /// </summary>
            BYTE_ALIGN_512 = 512,

            /// <summary>
            /// Aligns data to 4096-bytes (e.g. 0x384 > 0x1000)
            /// </summary>
            BYTE_ALIGN_4096 = 4096
        }

        private static uint ByteAlignPadding(uint offset, uint align)
        {
            return (align - (offset % align)) % align;
        }

        /// <summary>
        /// Returns an unsigned integer representing the number of bytes to be inserted for proper byte-aligned padding.
        /// This method uses an enumeration to define byte-alignment for common values. See each options documentation for more information.
        /// </summary>
        /// <param name="offset">The offset that will be used to calculate byte-aligned padding</param>
        /// <param name="align">How much padding should be applied</param>
        /// <returns>The number of bytes that should be inserted for proper byte-aligned padding</returns>
        public static uint ByteAlignPadding(uint offset, ByteAlignOptions align)
        {
            return ByteAlignPadding(offset, (uint)align);
        }
    }
}
