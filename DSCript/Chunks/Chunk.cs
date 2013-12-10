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

        public enum BytePaddingType : int
        {
            /// <summary>
            /// Specifies the padding type used in DRIV3R (0xDECO15A1)
            /// </summary>
            PaddingType1 = 0,

            /// <summary>
            /// Specifies the padding type used in Driver: Parallel Lines and Driver: San Francisco (0x3E)
            /// </summary>
            PaddingType2 = 1,

            /// <summary>
            /// Specifies a custom padding type as defined in <see cref="PaddingBytes"/>.
            /// </summary>
            Custom = 2
        }

        public static BytePaddingType PaddingType = BytePaddingType.PaddingType1;

        static byte[] _paddingBytes       = { 0x00, 0x00, 0x00, 0x00 };
        static byte[][] _paddingByteTypes = {
                                               new byte[4] { 0xA1, 0x15, 0xC0, 0xDE },
                                               new byte[4] { 0x3E, 0x3E, 0x3E, 0x3E }
                                           };

        public static byte[] PaddingBytes
        {
            get
            {
                switch (PaddingType)
                {
                case BytePaddingType.Custom:
                    return _paddingBytes;
                case BytePaddingType.PaddingType2:
                    return _paddingByteTypes[1];
                default:
                    return _paddingByteTypes[0];
                }
            }
        }

        public static void SetCustomPadding(char char1, char char2, char char3, char char4)
        {
            _paddingBytes = new byte[4] {
                (byte)char1,
                (byte)char2,
                (byte)char3,
                (byte)char4
            };
        }

        public static void SetCustomPadding(byte byte1, byte byte2, byte byte3, byte byte4)
        {
            _paddingBytes = new byte[4] { byte1, byte2, byte3, byte4 };
        }

        public static void ResetCustomPadding()
        {
            _paddingBytes = new byte[4] { 0x00, 0x00, 0x00, 0x00 };
        }
       
        public static bool IsChunkType(uint magic, ChunkType type)
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
