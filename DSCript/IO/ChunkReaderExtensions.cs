using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using DSCript;
using DSCript.Methods;

namespace DSCript.IO
{
    public static class ChunkReaderExtensions
    {
        public static void Seek(this ChunkReader i, long offset, SeekOrigin origin)
        {
            i.Stream.Seek(offset, origin);
        }

        public static CTypes ReadType(this ChunkReader i)
        {
            return (CTypes)i.Reader.ReadUInt32();
        }

        public static bool CheckIfType(this ChunkReader i, CTypes type)
        {
            return (i.Reader.ReadUInt32() == (uint)type) ? true : false;
        }

        public static string ReadString(this ChunkReader i, int length)
        {
            return Encoding.UTF8.GetString(i.Reader.ReadBytes(length));
        }

        public static string ReadUnicodeString(this ChunkReader i, int length)
        {
            return Encoding.Unicode.GetString(i.Reader.ReadBytes(length));
        }

        public static SubChunkBlock FirstOrNull(this ChunkReader i, CTypes type)
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

        public static ChunkBlock GetBlockChildOrNull(this ChunkReader i, SubChunkBlock subChunk)
        {
            int si = i.Chunk.FindIndex((c) => c.BaseOffset == subChunk.BaseOffset);

            return (si != -1) ? i.Chunk[si] : null;
        }
    }
}
