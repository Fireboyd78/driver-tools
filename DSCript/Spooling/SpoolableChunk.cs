using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DSCript.Spoolers
{
    /// <summary>
    /// Represents a class for spoolers thats content is a list of spoolers.
    /// </summary>
    public class SpoolableChunk : Spooler
    {
        /// <summary>
        /// Gets or sets the spoolers in this chunk.
        /// </summary>
        public List<Spooler> Spoolers { get; set; }

        public override void Dispose()
        {
            if (Spoolers != null)
            {
                foreach (Spooler spooler in Spoolers)
                    spooler.Dispose();

                Spoolers.Clear();
                Spoolers = null;
            }
        }

        public sealed override int Size
        {
            get { return CalculateSize(); }
        }

        private int[] Offsets;
        private int CalculateSize()
        {
            var count = (Spoolers != null) ? Spoolers.Count : 0;
            var size = 0x10;

            if (count > 0)
            {
                Offsets = new int[count];

                size += (count * 0x10);

                for (int s = 0; s < Spoolers.Count; s++)
                {
                    var spooler = Spoolers[s];

                    size = Memory.Align(size, spooler.Alignment);

                    Offsets[s] = size;

                    size += (spooler.Size + spooler.Description.Length);
                }

                var pack = (Alignment <= 8);

                size = Memory.Align(size, (!pack) ? Spoolers[0].Alignment : Alignment);
            }
            else
            {
                Offsets = null;
            }

            return size;
        }

        public sealed override void Load(Stream stream)
        {
            var baseOffset = stream.Position;

            var CHNK = new {
                Magic = stream.ReadInt32(),
                Size = stream.ReadInt32(),
                Count = stream.ReadInt32(),
                Version = stream.ReadInt32()
            };

            if (CHNK.Magic != (int)ChunkType.Chunk)
                throw new Exception("Chunk load error - bad magic!");
            if (CHNK.Version != 3)
                throw new Exception("Chunk load error - unsupported version!");

            if (CHNK.Count > 0)
            {
                Spoolers = new List<Spooler>();

                int headerSize = (CHNK.Count + 1) * 0x10;

                int cOffset = 0, lastAlignment = 0, lastMagic = 0;

                for (int i = 0; i < CHNK.Count; i++)
                {
                    stream.Seek((i + 1) * 0x10, baseOffset);

                    var magic       = stream.ReadInt32();
                    var offset      = stream.ReadInt32();
                    var reserved    = (byte)stream.ReadByte();
                    var strLen      = (byte)stream.ReadByte();
                    var unused      = stream.ReadInt16();
                    var size        = stream.ReadInt32();

                    var alignment   = (offset - headerSize != 0) ? Memory.GuessAlignment(offset) : 16;
                    var description = String.Empty;

                    if (strLen > 0)
                    {
                        stream.Seek(offset + size, baseOffset);
                        description = stream.ReadString(strLen);
                    }

                    // Determine if alignment is correct
                    if (lastAlignment > 0 && (alignment != lastAlignment))
                    {
                        if (!(alignment == 8 && lastAlignment == 4))
                        {
                            if (Memory.Align(cOffset, lastAlignment) == offset || magic == lastMagic)
                            {
                                //DSC.Log("Correcting alignment ({0}): {1} -> {2}", Encoding.UTF8.GetString(BitConverter.GetBytes(magic)), alignment, lastAlignment);
                                alignment = lastAlignment;
                            }
                        }
                    }

                    cOffset = offset + size + strLen;
                    lastAlignment = alignment;
                    lastMagic = magic;

                    stream.Seek(offset, baseOffset);

                    Spooler spooler = null;

                    if (stream.PeekInt32() == (int)ChunkType.Chunk)
                        spooler = new SpoolableChunk(magic, reserved);
                    else
                        spooler = new SpoolableData(magic, reserved, size);

                    spooler.Alignment = alignment;
                    spooler.Description = description;

                    // Load content
                    if (size > 0)
                        spooler.Load(stream);

                    Spoolers.Add(spooler);
                }
            }
        }

        public sealed override void WriteTo(Stream stream)
        {
            var CHNK = new {
                Magic = (int)ChunkType.Chunk,
                Size = Size,
                Count = (Spoolers != null) ? Spoolers.Count : 0,
                Version = 3
            };

            var baseOffset = stream.Position;

            stream.Write(CHNK.Magic);
            stream.Write(CHNK.Size);
            stream.Write(CHNK.Count);
            stream.Write(CHNK.Version);

            if (CHNK.Count > 0)
            {
                // write header
                for (int s = 0; s < CHNK.Count; s++)
                {
                    stream.Seek((s + 1) * 0x10, baseOffset);

                    var spooler = Spoolers[s];

                    stream.Write(spooler.Magic);
                    stream.Write(Offsets[s]);
                    stream.Write(spooler.Reserved);
                    stream.Write((byte)spooler.Description.Length);
                    stream.Write(0x0C, 0xCC);
                    stream.Write(spooler.Size);
                }

                // write data
                for (int i = 0; i < CHNK.Count; i++)
                {
                    var spooler = Spoolers[i];

                    stream.Fill(Chunk.PaddingBytes, (Offsets[i] - (stream.Position - baseOffset)));

                    stream.Seek(Offsets[i], baseOffset);

                    spooler.WriteTo(stream);

                    stream.Write(spooler.Description);
                }

                // write rest of padding
                var size = stream.Position - baseOffset;

                if (size < CHNK.Size)
                    stream.Fill(Chunk.PaddingBytes, (CHNK.Size - size));
            }
        }

        /// <summary>
        /// Creates a new chunked spooler with the magic number and reserved value set to zero.
        /// </summary>
        public SpoolableChunk() : base() { }

        /// <summary>
        /// Creates a new chunked spooler with the specified magic number and reserved value.
        /// </summary>
        /// <param name="magic">The magic number</param>
        /// <param name="reserved">The reserved value</param>
        public SpoolableChunk(int magic, byte reserved) : base(magic, reserved) { }

        /// <summary>
        /// Creates a new chunked spooler from the specified file.
        /// </summary>
        /// <param name="filename">The file to load.</param>
        public SpoolableChunk(string filename)
            : base()
        {
            using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.PeekInt32() != (int)ChunkType.Chunk)
                    throw new Exception("Invalid chunk file.");

                Load(fs);
            }
        }
    }
}
