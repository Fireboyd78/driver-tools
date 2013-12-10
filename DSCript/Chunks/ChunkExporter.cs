using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DSCript
{
    public static class ChunkExporter
    {
        static ChunkFile ChunkFile = null;
        static FileStream Stream   = null;

        static void WriteChunkData(ChunkBlock chunk)
        {
            Stream.Write(chunk.Magic);
            Stream.Write(chunk.Size);
            Stream.Write(chunk.Entries.Count);
            Stream.Write(Chunk.Version);

            foreach (ChunkEntry block in chunk.Entries)
            {
                Stream.Write(block.Magic);
                Stream.Write(block.Offset);
                Stream.WriteByte(block.Reserved);
                Stream.WriteByte(block.Description.Length);
                Stream.Write(0x0C, 0xCC);
                Stream.Write(block.Size);
            }

            WriteBlocks(chunk);
        }

        static void WriteBlocks(ChunkBlock chunk)
        {
            List<ChunkBlock> Chunks = ChunkFile.Chunks;

            foreach (ChunkEntry block in chunk.Entries)
            {
                WritePadding(block.BaseOffset);

                BlockData data = ChunkFile.GetBlockData(block);

                if (data != null)
                {
                    Stream.Write(data.Data, 0, data.Data.Length);
                }
                else
                {
                    int i = Chunks.FindIndex((c) => c.BaseOffset == block.BaseOffset);

                    if (i != -1)
                    {
                        WriteChunkData(Chunks[i]);
                        WritePadding(Chunks[i].BaseOffset + Chunks[i].Size);
                    }
                }

                if (block.Description.Length > 0)
                    Stream.Write(Encoding.UTF8.GetBytes(block.Description), 0, block.Description.Length);
            }
        }

        static void WritePadding(long padToOffset)
        {
            if (Stream.Position > padToOffset)
                return;

            Stream.WriteByteAlignment(4);

            long padLength = padToOffset - Stream.Position;

            for (int i = 0; i < padLength / 4; i++)
                Stream.Write(Chunk.PaddingBytes);
        }

        public static void Export(string filename, ChunkFile chunkFile)
        {
            ChunkFile = chunkFile;
            Stream    = File.Create(filename, (int)chunkFile.Chunks[0].Size);

            WriteChunkData(chunkFile.Chunks[0]);
            WritePadding(chunkFile.Chunks[0].Size);

            ChunkFile = null;
            Stream.Dispose();
        }

        public static void Export(this ChunkFile @this, string filename)
        {
            Export(filename, @this);
        }
    }
}
