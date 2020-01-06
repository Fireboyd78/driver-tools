using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript
{
    public static class Chunk
    {
        /// <summary>
        /// The supported version for loading Chunk files (Version 3).
        /// </summary>
        public static readonly int Version = 3;
        
        public static readonly byte[] PaddingBytes = { 0xA1, 0xAC, 0x83, 0xDF }; // ;)
    }
}
