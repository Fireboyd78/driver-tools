using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript
{
    public static class Chunk
    {
        /// <summary>
        /// The magic number of a Chunk (CHNK).
        /// </summary>
        public static readonly ChunkType Magic = ChunkType.Chunk;

        /// <summary>
        /// The supported version for loading Chunk files (Version 3).
        /// </summary>
        public static readonly int Version = 3;

        /// <summary>
        /// Defines the amount of padding between each non-root Chunk block. A default value of 4096 is used. Use caution when changing the value, as this may crash the game!
        /// </summary>
        public static ByteAlignment FilePadding = ByteAlignment.ByteAlign4096;

        public static byte[] PaddingBytes = { 0xA1, 0x15, 0xC0, 0xDE };
       
        public static bool CheckType(uint magic, ChunkType type)
        {
            return (magic == (uint)type) ? true : false;
        }

        public static ChunkType GetChunkType(this ChunkEntry @this)
        {
            return (Enum.IsDefined(typeof(ChunkType), @this.Magic)) ? (ChunkType)@this.Magic : ChunkType.Unknown;
        }

        public static ChunkType GetChunkType(this ChunkBlock @this)
        {
            return (@this.HasParent) ? @this.Parent.GetChunkType() : ChunkType.ChunkRoot;
        }

        /// <summary>
        /// An enumeration with common values used to calculate byte-alignment.
        /// </summary>
        public enum ByteAlignment : long
        {
            /// <summary>
            /// Aligns data to 16-bytes (e.g. 0x384 > 0x390)
            /// </summary>
            ByteAlign16 = 16,

            /// <summary>
            /// Aligns data to 32-bytes (e.g. 0x384 > 0x3A0)
            /// </summary>
            ByteAlign32 = 32,
            
            /// <summary>
            /// Aligns data to 512-bytes (e.g. 0x384 > 0x400)
            /// </summary>
            ByteAlign512 = 512,

            /// <summary>
            /// Aligns data to 4096-bytes (e.g. 0x384 > 0x1000)
            /// </summary>
            ByteAlign4096 = 4096
        }

        /// <summary>
        /// Returns an unsigned integer representing the number of bytes to be inserted for proper byte-aligned padding.
        /// </summary>
        /// <param name="offset">The offset that will be used to calculate byte-aligned padding</param>
        /// <param name="align">How much padding should be applied</param>
        /// <returns>The number of bytes that should be inserted for proper byte-aligned padding</returns>
        public static uint GetByteAlignment(uint offset, uint align)
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
        public static uint GetByteAlignment(uint offset, ByteAlignment align)
        {
            return GetByteAlignment(offset, (uint)align);
        }

        /// <summary>
        /// Returns an unsigned integer representing an offset that has been byte-aligned with a given value.
        /// </summary>
        /// <param name="offset">The offset that will be used to calculate byte-aligned padding</param>
        /// <param name="align">How much padding should be applied</param>
        /// <returns>The byte-aligned offset</returns>
        public static uint ByteAlign(uint offset, uint align)
        {
            return offset + GetByteAlignment(offset, align);
        }

        /// <summary>
        /// Returns an unsigned integer representing an offset that has been byte-aligned with a given value.
        /// /// This method uses an enumeration to define byte-alignment for common values. See each options documentation for more information.
        /// </summary>
        /// <param name="offset">The offset that will be used to calculate byte-aligned padding</param>
        /// <param name="align">How much padding should be applied</param>
        /// <returns>The byte-aligned offset</returns>
        public static uint ByteAlign(uint offset, ByteAlignment align)
        {
            return offset + GetByteAlignment(offset, align);
        }
    }
}
