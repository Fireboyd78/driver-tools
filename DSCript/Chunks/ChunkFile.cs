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
    public class ChunkFile : IDisposable
    {
        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
            if (Chunks != null)
            {
                for (int i = 0; i < Chunks.Count; i++)
                    Chunks[i] = null;

                Chunks.Clear();
                Chunks = null;
            }
            if (BlockData != null)
            {
                for (int i = 0; i < BlockData.Count; i++)
                    BlockData[i] = null;

                BlockData.Clear();
                BlockData = null;
            }
        }

        BinaryReaderEx _reader;
        BinaryWriter _writer;

        FileStream _stream;

        /// <summary>
        /// Gets the name of the file that was used to build this <see cref="ChunkFile"/>.
        /// </summary>
        public string Filename { get; protected set; }

        protected internal BinaryReaderEx Reader
        {
            get
            {
                if (_reader == null)
                    _reader = new BinaryReaderEx(Stream);

                return _reader;
            }
        }

        protected internal BinaryWriter Writer
        {
            get
            {
                if (_writer == null)
                    _writer = new BinaryWriter(Stream);

                return _writer;
            }
        }

        /// <summary>
        /// Returns a <see cref="FileStream"/> based on the file that was used to build this <see cref="ChunkFile"/>.
        /// </summary>
        /// <returns></returns>
        public FileStream Stream
        {
            get
            {
                if (_stream == null)
                    _stream = File.Open(Filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    
                return _stream;
            }
        }

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

        public ChunkBlock GetChunkFromEntry(ChunkEntry block)
        {
            if (block.BlockData != null)
                return null;

            return Chunks.Find((c) => c.HasParent && c.Parent == block);
        }

        protected virtual void BlockSizeUpdated(object sender, BlockDataEventArgs e)
        {
            ChunkEntry block = (ChunkEntry)sender;
            ChunkBlock parent = block.Parent;

            parent.CalculateOffsets();

            // Update parents as needed
            // This will update all parents until it reaches the root!
            if (parent.HasParent)
                parent.Parent.Size = parent.CalculateSize();
        }

        protected virtual void ReadChunks(ChunkBlock chunk, int id, uint baseOffset)
        {
            // Create a new ChunkEntry with the specified ID
            // Initialize paramaters using the BinaryReader
            ChunkEntry block = new ChunkEntry(id, chunk) {
                Magic    = Reader.ReadUInt32(),
                Offset   = Reader.ReadUInt32(),
                Reserved = Reader.ReadByte()
            };

            chunk.Entries.Insert(id, block);

            // Read string length
            int strLen = Reader.ReadByte();

            // skip 2 bytes we don't need
            Reader.Seek(0x2, SeekOrigin.Current);

            // Read size
            block.Size = Reader.ReadUInt32();
            block.SizeUpdated += (o, e) => BlockSizeUpdated(o, e);

            // Hold this position so we can come back to it and read the next entry
            long holdPosition = Reader.GetPosition();

            /* ----------------------------------------------------
             * Descriptions are found at the very end of chunks, so
             * we can add up the total size of the chunk (including
             * offsets) to find it.
             * ---------------------------------------------------- */
            if (strLen > 0)
            {
                Reader.SeekFromOrigin(baseOffset, (block.Offset + block.Size));
                block.Description = Reader.ReadString(strLen);
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
            Reader.SeekFromOrigin(baseOffset, block.Offset);

            uint magic = Reader.ReadUInt32();

            if (!Chunk.IsChunkType(magic, ChunkType.Chunk))
            {
                BlockData blockData = new BlockData(this, block);
                block.BlockData = blockData;

                BlockData.Add(blockData);
            }
            else
            {
                // Since we read the first 4 bytes, the offset is the current position - 4
                uint blockOffset = (uint)(Reader.GetPosition() - 0x4);

                int ss = Chunks.Count;
                int ii = 0;
                
                // Read size and count
                uint size = Reader.ReadUInt32();
                int count = Reader.ReadInt32();

                // Skip version
                Reader.Seek(0x4, SeekOrigin.Current);

                // Create our new ChunkBlock using the information from above
                ChunkBlock newChunk = new ChunkBlock(id, blockOffset, block) {
                    Size    = size,
                    Entries = new List<ChunkEntry>(count)
                };

                // Insert it into the master Chunk list
                Chunks.Insert(ss, newChunk);

                // Recurse through the entries
                for (ii = 0; ii < newChunk.Entries.Capacity; ii++)
                    ReadChunks(newChunk, ii, blockOffset);
            }

            // Seek back to the next entry and continue
            Reader.Seek(holdPosition, SeekOrigin.Begin);
        }

        protected virtual int Load()
        {
            // Make sure this is actually a chunk file and not some imitator
            if (!Chunk.IsChunkType(Reader.ReadUInt32(), ChunkType.Chunk))
                return -1;

            uint size = Reader.ReadUInt32();
            int count = Reader.ReadInt32();

            // Check version
            if (Reader.ReadUInt32() != Chunk.Version)
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
                ReadChunks(root, ck, 0x0);

            return numChunksRead;
        }

        public ChunkFile(string filename)
        {
            Filename = filename;
            
            if (Load() != -1)
                DSC.Log("{0} chunks loaded.", Chunks.Count);
        }

        ~ChunkFile()
        {
            Dispose();
        }
    }
}
