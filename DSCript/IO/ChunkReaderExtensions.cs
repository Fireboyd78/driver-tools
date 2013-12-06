using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using DSCript;

namespace DSCript.IO
{
    public static class ChunkReaderExtensions
    {
        public static ChunkType ReadType(this ChunkReader i)
        {
            return (ChunkType)i.Reader.ReadUInt32();
        }

        public static bool CheckIfType(this ChunkReader i, ChunkType type)
        {
            return (i.Reader.ReadUInt32() == (uint)type) ? true : false;
        }

        public static SubChunkBlock FirstOrNull(this ChunkReader i, ChunkType type)
        {
            return i.FirstOrNull((uint)type);
        }

        public static SubChunkBlock FirstOrNull(this ChunkReader i, uint magic)
        {
            for (int k = 0; k < i.Chunk.Count; k++)
            {
                int si = i.Chunk[k].Subs.FindIndex((c) => c.Magic == magic);

                if (si != -1)
                    return i.Chunk[k].Subs[si];
            }
            return null;
        }

        public static ChunkBlockOld GetBlockChildOrNull(this ChunkReader i, SubChunkBlock subChunk)
        {
            int si = i.Chunk.FindIndex((c) => c.BaseOffset == subChunk.BaseOffset);

            return (si != -1) ? i.Chunk[si] : null;
        }
    }
}
