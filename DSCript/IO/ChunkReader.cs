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
    public sealed class ChunkReader
    {
        public FileStream Stream { get; private set; }
        public BinaryReader Reader { get; private set; }

        public string Filename { get; private set; }

        public bool IsLoaded { get; private set; }

        public List<ChunkBlockOld> Chunk { get; set; }

        /// <summary> Gets or sets the position of the cursor in the FileStream. </summary>
        public long Position
        {
            get { return Stream.Position; }
            set { Stream.Position = value; }
        }

        public void ReadChunks(int s, int i, uint baseOffset)
        {
            // Build the definitions table
            Chunk[s].Subs.Insert(i, new SubChunkBlock(i, Chunk[s]));
            var subChunk = Chunk[s].Subs[i];

            // Collect some data about our definitions
            subChunk.Magic = Reader.ReadUInt32();
            subChunk.Offset = Reader.ReadUInt32();
            
            subChunk.Unk1 = Reader.ReadByte();
            subChunk.StrLen = Reader.ReadByte();
            subChunk.Unk2 = Reader.ReadByte();
            subChunk.Unk3 = Reader.ReadByte();
            
            subChunk.Size = Reader.ReadUInt32();

            // Hold the position so we can come back to it
            // Our next entry will be read from here
            long holdPosition = Position;

            // Read description if StrLen > 0
            if (subChunk.StrLen > 0)
            {
                // Descriptions are found at the very end of chunks
                // So add up the total size of the chunk (including offsets) to find it
                Reader.Seek(baseOffset + (subChunk.Offset + subChunk.Size), SeekOrigin.Begin);

                // Descriptions are simple strings without a terminator - this is why StrLen is important!
                subChunk.Description = Reader.ReadString(subChunk.StrLen);
            }
            else
            {
                // If no description exists, just give it an empty string value
                subChunk.Description = String.Empty;
            }

            // Now we seek to the beginning of the data
            Reader.Seek(baseOffset + subChunk.Offset, SeekOrigin.Begin);

            // We need to see if the data is in fact a nested chunk
            // So check to see if this is another Chunk so we can recurse through it
            if (this.CheckIfType(ChunkType.Chunk))
            {
                // Since Chunks begin with 'CHNK', it's safe to assume we have found a nested chunk!
                // We'll gather the data from it in a similar fashion to the one that is currently on hold

                // Get the base offset for our nested chunk
                uint subBaseOffset = (uint)(Position - 0x4);

                // Since .Count returns a non-zero based number, we can use it to identify our new chunk
                int ss = Chunk.Count, ii = 0;

                // For each chunk (nested or not) within the file, we add it to the master chunk list
                // So add this one at the very end
                Chunk.Insert(ss, new ChunkBlockOld(ss, subBaseOffset, subChunk));

                // Collect the rest of the data
                Chunk[ss].Size = Reader.ReadUInt32();
                Chunk[ss].SubCount = Reader.ReadUInt32();

                // Skip version, it's pointless to read it
                Reader.Seek(0x4, SeekOrigin.Current);

                // Now we see how many entries are present
                // If there's multiple entries, we can use a loop to grab them
                // But if there is not, we know that the index is '0' so we skip the loop
                if ((ii + 1) != Chunk[ss].SubCount)
                    for (ii = 0; ii < Chunk[ss].SubCount; ii++)
                        ReadChunks(ss, ii, subBaseOffset);
                else
                    ReadChunks(ss, 0, subBaseOffset);
            }
            // Seek back to the list. Lather, rinse, repeat!
            Reader.Seek(holdPosition, SeekOrigin.Begin);
        }

        public ChunkReader(string filename)
        {
            IsLoaded = false;

            Filename = filename;

            using (Stream = File.Open(Filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Reader = new BinaryReader(Stream))
            {
                Stopwatch Timer = new Stopwatch();
                Timer.Start();

                // Error checking: Make sure this is actually a chunk file and not some imitator
                if (!this.CheckIfType(ChunkType.Chunk))
                {
                    Console.WriteLine("Sorry, this is not a chunk file!");
                    Timer.Stop();

                    goto done;
                }

                Chunk = new List<ChunkBlockOld>();
                Chunk.Insert(0, new ChunkBlockOld(0, 0x0));

                Chunk[0].Size = Reader.ReadUInt32();
                Chunk[0].SubCount = Reader.ReadUInt32();

                // Error checking: Again, make sure this is actually a chunk file...
                if (Reader.ReadUInt32() != ChunkBlockOld.Version)
                {
                    Console.WriteLine("Sorry, this chunk file version is unsupported!");
                    Timer.Stop();

                    goto done;
                }

                DSC.Log("Please wait...there are {0} chunks that need to be loaded!", Chunk[0].SubCount);

                // Define this here so the recursive ReadChunks function cannot modify it
                int ck = 0;

                // Loop through the chunks
                for (ck = 0; ck < Chunk[0].SubCount; ck++)
                    ReadChunks(0, ck, 0x0);

                Timer.Stop();
                Console.WriteLine("Parsed file in {0}ms" +
                    ((Timer.ElapsedMilliseconds >= 1000) ? " / {1:F3} seconds" : ""), Timer.ElapsedMilliseconds, Timer.ElapsedMilliseconds / 1000.0);
            }
        done:
            IsLoaded = true;
            return;
        }
    }
}
