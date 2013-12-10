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

using DSCript.IO;

namespace DSCript
{
    public sealed class ChunkFile : IDisposable
    {
        /// <summary>
        /// Gets the name of the file that was used to build this <see cref="ChunkFile"/>.
        /// </summary>
        public string Filename { get; private set; }

        #region Temp File management
        private string TempFilename
        {
            get { return String.Format("{0}.TEMP", Filename); }
        }

        public bool HasTempFile
        {
            get { return (File.Exists(TempFilename)) ? true : false; }
        }

        public FileStream GetTempFile()
        {
            if (!HasTempFile)
                File.Copy(Filename, TempFilename, true);

            return File.Open(TempFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        public void DestroyTempFile()
        {
            if (HasTempFile)
                File.Delete(TempFilename);
        }
        #endregion

        public void Dispose()
        {
            if (Chunks != null)
            {
                for (int i = 0; i < Chunks.Count; i++)
                    Chunks[i] = null;

                Chunks.Clear();
            }

            if (BlockData != null)
            {
                for (int i = 0; i < BlockData.Count; i++)
                    BlockData[i] = null;

                BlockData.Clear();
            }

            DestroyTempFile();
        }

        /// <summary>
        /// Returns a new <see cref="FileStream"/> based on the file that was used to build this <see cref="ChunkFile"/>.
        /// </summary>
        /// <returns></returns>
        public FileStream GetStream()
        {
            return File.Open(Filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public bool IsLoaded { get; private set; }

        public List<ChunkBlock> Chunks { get; set; }
        public List<BlockData> BlockData { get; set; }

        /// <summary>
        /// Gets the block data for the specified block.
        /// </summary>
        /// <param name="block">The block to retrieve the data from</param>
        public BlockData GetBlockData(ChunkEntry block)
        {
            return BlockData.Find((data) => data.Block == block);
        }

        private void BlockSizeUpdated(object sender, BlockDataEventArgs e)
        {
            ChunkEntry block = (ChunkEntry)sender;
            ChunkBlock parent = (ChunkBlock)block.Parent;

            parent.CalculateOffsets();

            // Update parents as needed
            // This will update all parents until it reaches the root!
            if (parent.HasParent)
                parent.Parent.Size = parent.CalculateSize();
        }

        private void ReadChunks(BinaryReader reader, ChunkBlock chunk, int id, uint baseOffset)
        {
            // Create a new ChunkEntry with the specified ID
            // Initialize paramaters using the BinaryReader
            ChunkEntry block = new ChunkEntry(id, chunk) {
                Magic    = reader.ReadUInt32(),
                Offset   = reader.ReadUInt32(),
                Reserved = reader.ReadByte()
            };

            // Add our ChunkEntry to the ChunkBlock
            chunk.Entries.Insert(id, block);

            // Read string length
            int strLen = reader.ReadByte();

            // skip 2 bytes we don't need
            reader.Seek(0x2, SeekOrigin.Current);

            // Read size
            block.Size = reader.ReadUInt32();
            block.SizeUpdated += (o, e) => BlockSizeUpdated(o, e);

            // Hold this position so we can come back to it and read the next entry
            long holdPosition = reader.GetPosition();

            /* ----------------------------------------------------
             * Descriptions are found at the very end of chunks, so
             * we can add up the total size of the chunk (including
             * offsets) to find it. Descriptions are Pascal strings
             * (those with no terminator character ('\0').
             * 
             * If there isn't a description, an empty string value
             * will be assigned.
             * ---------------------------------------------------- */
            if (strLen > 0)
            {
                reader.Seek(baseOffset + (block.Offset + block.Size), SeekOrigin.Begin);
                block.Description = reader.ReadString(strLen);
            }
            else
            {
                block.Description = String.Empty;
            }


            /* ----------------------------------------------------
             * Now we read the actual data contained in this block.
             * We first check to see if the data is either a nested
             * chunk, or raw data that should be accounted for.
             * 
             * If it's a chunk 'CHNK', we'll do some more recursing
             * - otherwise, we'll just add a new BlockData entry to
             * keep track of the raw data.
             * ---------------------------------------------------- */
            reader.Seek(baseOffset + block.Offset, SeekOrigin.Begin);

            uint magic = reader.ReadUInt32();

            if (!Chunk.IsChunkType(magic, ChunkType.Chunk))
            {
                BlockData.Add(new BlockData(this, block));
            }
            else
            {
                // Since we read the first 4 bytes, the offset is the current position - 4
                uint blockOffset = (uint)(reader.GetPosition() - 0x4);

                int ss = Chunks.Count;
                int ii = 0;
                
                // Read size and count
                uint size = reader.ReadUInt32();
                int count = reader.ReadInt32();

                // Skip version
                reader.Seek(0x4, SeekOrigin.Current);

                // Create our new ChunkBlock using the information from above
                ChunkBlock newChunk = new ChunkBlock(ss, blockOffset, block) {
                    Size    = size,
                    Entries = new List<ChunkEntry>(count)
                };

                // Insert it into the master Chunk list
                Chunks.Insert(ss, newChunk);

                // Recurse through the entries
                for (ii = 0; ii < newChunk.Entries.Capacity; ii++)
                    ReadChunks(reader, newChunk, ii, blockOffset);
            }

            // Seek back to the next entry and continue
            reader.Seek(holdPosition, SeekOrigin.Begin);
        }

        public int Load()
        {
            using (FileStream fs = GetStream())
            using (BinaryReader f = new BinaryReader(fs))
            {
                // Make sure this is actually a chunk file and not some imitator
                if (!Chunk.IsChunkType(f.ReadUInt32(), ChunkType.Chunk))
                    return -1;

                uint size = f.ReadUInt32();
                int count = f.ReadInt32();

                // Check version
                if (f.ReadUInt32() != Chunk.Version)
                    return -1;

                // Intialize the Chunks and BlockData
                Chunks = new List<ChunkBlock>();
                BlockData = new List<BlockData>();

                // Create our root chunk using ID '0' and a base offset of 0x0
                // Then insert it into the master chunk list
                ChunkBlock root = new ChunkBlock(0, 0x0) {
                    Size    = size,
                    Entries = new List<ChunkEntry>(count)
                };

                Chunks.Insert(0, root);

                DSC.Log("Loading {0} chunks...", root.Entries.Capacity);

                // Read all chunk entries (the iterator is defined in the local scope so it doesn't lose its value)
                int ck = 0, numChunksRead = 0;

                for (ck = 0; ck < root.Entries.Capacity; ck++, ++numChunksRead)
                    ReadChunks(f, root, ck, 0x0);

                return numChunksRead;
            }
        }

        public ChunkFile(string filename)
        {
            Filename = filename;

            int numChunksRead = Load();

            IsLoaded = (numChunksRead != -1) ? true : false;

            if (IsLoaded)
                DSC.Log("{0} chunks loaded.", numChunksRead);
        }

        ~ChunkFile()
        {
            DestroyTempFile();
        }
    }
}
