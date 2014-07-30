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
            /// Specifies a custom padding type as defined in <see cref="Chunk.PaddingBytes"/>.
            /// </summary>
            Custom = 2
        }

        public static BytePaddingType PaddingType = BytePaddingType.PaddingType1;

        static byte[] _paddingBytes = { 0x00, 0x00, 0x00, 0x00 };

        static byte[][] _paddingByteTypes = {
            new byte[] { 0xA1, 0x15, 0xC0, 0xDE },
            new byte[] { 0x3E, 0x3E, 0x3E, 0x3E }
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
    }
}
