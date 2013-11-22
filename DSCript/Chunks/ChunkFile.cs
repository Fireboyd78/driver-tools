using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

using DSCript;
using DSCript.IO;
using DSCript.Methods;

namespace DSCript
{
    public sealed class ChunkFile
    {
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
            // Access can still be made with 'block' object
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

            /* ################# Read description ################# */
            if (strLen > 0)
            {
                // Descriptions are found at the very end of chunks
                // So add up the total size of the chunk (including offsets) to find it
                reader.Seek(baseOffset + (block.Offset + block.Size), SeekOrigin.Begin);

                // Descriptions are Pascal strings (no terminator)
                block.Description = reader.ReadString(strLen);
            }
            else
            {
                // If no description exists, just give it an empty string value
                block.Description = String.Empty;
            }
            /* #################################################### */


            /* ################# Read data ######################## */
            reader.Seek(baseOffset + block.Offset, SeekOrigin.Begin);

            // We need to see if the data is in fact a nested chunk
            // So check to see if this is another Chunk so we can recurse through it
            if (Chunk.CheckType(reader.ReadUInt32(), ChunkType.Chunk))
            {
                /* Chunks begin with 'CHNK', thus it's safe to assume we have found a nested chunk!
                   We'll gather the data from it in a similar fashion to the one that is currently on hold */

                // Since we read the first 4 bytes, the offset is the current position - 4
                uint blockOffset = (uint)(reader.GetPosition() - 0x4);
                int ss = Chunks.Count, ii = 0;

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

                // If there's multiple entries, we can use a loop to grab them
                // But if there is not, we know that the index is '0' so we skip the loop
                if ((ii + 1) != newChunk.Entries.Capacity)
                {
                    for (ii = 0; ii < newChunk.Entries.Capacity; ii++)
                        ReadChunks(reader, newChunk, ii, blockOffset);
                }
                else
                {
                    ReadChunks(reader, newChunk, 0, blockOffset);
                }
            }
            else
            {
                // If it's not a nested chunk, add a new BlockData entry
                BlockData.Add(new BlockData(this, block));
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
                if (!Chunk.CheckType(f.ReadUInt32(), ChunkType.Chunk))
                    return -1;

                uint size = f.ReadUInt32();
                int count = f.ReadInt32();

                // Check version
                if (f.ReadUInt32() != Chunk.Version)
                    return -1;

                // No errors so far, intialize the Chunks and BlockData!
                Chunks = new List<ChunkBlock>();
                BlockData = new List<BlockData>();

                // Create our root chunk using ID '0' and a base offset of 0x0
                ChunkBlock root = new ChunkBlock(0, 0x0) {
                    Size    = size,
                    Entries = new List<ChunkEntry>(count)
                };

                // Insert it into the master chunk list
                Chunks.Insert(0, root);

                DSC.Log("Loading {0} chunks...please wait", root.Entries.Capacity);

                // Read all chunk entries (the iterator is defined in the local scope so it doesn't lose its value)
                int ck;

                for (ck = 0; ck < root.Entries.Capacity; ck++)
                    ReadChunks(f, root, ck, 0x0);

                return 1;
            }
        }

        private void WriteChunkData(FileStream stream, ChunkBlock chunk)
        {
            byte[] buffer = new byte[(int)chunk.HeaderSize];

            stream.Write(chunk.Magic);
            stream.Write(chunk.Size);
            stream.Write(chunk.Entries.Count);
            stream.Write(Chunk.Version);

            foreach (ChunkEntry block in chunk.Entries)
            {
                stream.Write(block.Magic);
                stream.Write(block.Offset);
                stream.WriteByte(block.Reserved);
                stream.WriteByte(block.Description.Length);
                stream.Write(0x0C, 0xCC);
                stream.Write(block.Size);
            }

            WriteBlocks(stream, chunk);
        }

        private void WriteBlocks(FileStream stream, ChunkBlock chunk)
        {
            foreach (ChunkEntry block in chunk.Entries)
            {
                WritePadding(stream, block.BaseOffset);

                BlockData data = GetBlockData(block);

                if (data != null)
                {
                    stream.Write(data.Data, 0, data.Data.Length);

                    if (block.Description.Length > 0)
                        stream.Write(Encoding.UTF8.GetBytes(block.Description), 0, block.Description.Length);
                }
                else
                {
                    int i = Chunks.FindIndex((c) => c.BaseOffset == block.BaseOffset);

                    if (i != -1)
                    {
                        WriteChunkData(stream, Chunks[i]);
                        WritePadding(stream, (Chunks[i].BaseOffset + Chunks[i].Size));
                    }

                    if (block.Description.Length > 0)
                        stream.Write(Encoding.UTF8.GetBytes(block.Description), 0, block.Description.Length);
                }
            }
        }

        private void WritePadding(FileStream stream, long padToOffset)
        {
            if (stream.Position > padToOffset)
                return;

            stream.ByteAlign(4);

            long padLength = padToOffset - stream.Position;

            for (int i = 0; i < padLength / 4; i++)
                stream.Write(Chunk.PaddingBytes);
        }

        public void Export()
        {
            Export(@"C:\dev\VS2013\Projects\Driver Tools\Antilli\bin\Debug\export\export.chunk");
        }

        public void Export(string filename)
        {
            using (FileStream f = File.Create(filename, (int)Chunks[0].Size))
            {
                WriteChunkData(f, Chunks[0]);
                WritePadding(f, Chunks[0].Size);
            }

            DSC.Log("Successfully exported {0}", filename);
        }

        public ChunkFile(string filename)
        {
            Filename = filename;

            IsLoaded = (Load() != -1) ? true : false;
        }

        ~ChunkFile()
        {
            DestroyTempFile();
        }
    }
}
