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
        
        public static readonly byte[] PaddingBytes = { 0x2D, 0xA1, 0x0B, 0xF0 }; // 0xF00BA12D
    }
}
