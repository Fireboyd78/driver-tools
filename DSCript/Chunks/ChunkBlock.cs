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

using DSCript;

namespace DSCript
{
    /// <summary>
    /// A class that represents a spooling-based data format, used to store data of various formats in Reflections games.
    /// </summary>
    public sealed class ChunkBlock : Block
    {
        /// <summary>
        /// Gets or sets the list of block entries in this chunk.
        /// </summary>
        public List<ChunkEntry> Entries { get; set; }

        /// <summary>
        /// Gets the magic number for this chunk.
        /// </summary>
        public new uint Magic
        {
            get { return (uint)Chunk.Magic; }
        }

        /// <summary>
        /// Gets or sets the parent of this chunk.
        /// </summary>
        public new ChunkEntry Parent
        {
            get { return (ChunkEntry)base.Parent; }
            set { base.Parent = value; }
        }

        /// <summary>
        /// Gets a value representing whether or not this chunk has a parent.
        /// </summary>
        public bool HasParent
        {
            get { return (Parent != null) ? true : false; }
        }

        public override BlockType BlockType
        {
            get { return BlockType.Chunk; }
        }

        /// <summary>
        /// Gets the computed size of this chunk.
        /// </summary>
        public override uint Size
        {
            get { return CalculateSize(); }
        }

        /// <summary>
        /// Gets the computed size of the header.
        /// </summary>
        public uint HeaderSize
        {
            get { return 0x10 * ((uint)Entries.Count + 1); }
        }

        /// <summary>
        /// Calculate's the offsets for each entry.
        /// </summary>
        public void CalculateOffsets()
        {
            // Setup the base offset
            uint baseOffset = Chunk.ByteAlign((uint)HeaderSize, (HasParent) ? Chunk.FilePadding : Chunk.ByteAlignment.ByteAlign4096);

            for (int i = 0; i < Entries.Count; i++)
            {
                ChunkEntry entry = Entries[i];

                entry.Offset = baseOffset;

                baseOffset += Chunk.ByteAlign(entry.Size + (uint)entry.Description.Length, (HasParent) ? Chunk.FilePadding : Chunk.ByteAlignment.ByteAlign4096);
            }

            if (HasParent)
                Parent.Size = CalculateSize();
        }

        /// <summary>
        /// Calculates the overall size of this chunk.
        /// </summary>
        /// <returns>The computed size of this chunk.</returns>
        public uint CalculateSize()
        {
            // Return 0x10 for dummy chunks
            if (Entries.Count == 0)
                return 0x10;

            // We can calculate the chunk size by using the last item entry, since it represents all chunks before it
            // Calculate size using offset, size, and (if it has one) description length

            ChunkEntry last = Entries.Last();

            uint size = last.Offset + last.Size + (uint)last.Description.Length;

            return Chunk.ByteAlign(size, (HasParent) ? Chunk.FilePadding : Chunk.ByteAlignment.ByteAlign4096);
        }

        /// <summary>
        /// Creates a new <see cref="ChunkBlock"/> with the specified id and offset.
        /// Since no parent is defined, this should be used for root chunks only.
        /// </summary>
        /// <param name="id">The zero-based id used to identify this chunk.</param>
        /// <param name="offset">The absolute offset to this chunk.</param>
        public ChunkBlock(int id, uint offset) : this(id, offset, null) { }

        /// <summary>
        /// Creates a new <see cref="ChunkBlock"/> with the specified id, offset, and parent.
        /// The parent-child relationship allows for this chunk to be included in size calculations that are initiated from the parent.
        /// </summary>
        /// <param name="id">The zero-based id used to identify this chunk.</param>
        /// <param name="offset">The relative offset to this chunk.</param>
        /// <param name="parent">The <see cref="ChunkEntry"/> parent of this chunk.</param>
        public ChunkBlock(int id, uint offset, ChunkEntry parent)
        {
            ID = id;
            Offset = offset;
            Parent = parent;
        }
    }
}
