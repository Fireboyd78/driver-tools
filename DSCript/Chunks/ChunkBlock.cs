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
using DSCript.IO;

namespace DSCript
{
    public sealed class ChunkBlock : Block
    {
        const string fmt =
@":: Chunk Block ::
Magic: '{0}'
Base Offset: 0x{1:X}
Offset: 0x{2:X}
Size: 0x{3:X}
Parent? {4}
--
";

        public List<ChunkEntry> Entries { get; set; }

        /// <summary>
        /// Gets the Magic Number for this Chunk block. Note: The set accessor cannot be used to change the magic.
        /// </summary>
        public override uint Magic
        {
            get { return (uint)Chunk.Magic; }
            set { return; }
        }

        public new ChunkEntry Parent
        {
            get { return (ChunkEntry)base.Parent; }
            set { base.Parent = value; }
        }

        public bool HasParent
        {
            get { return (Parent != null) ? true : false; }
        }

        public override BlockType BlockType
        {
            get { return BlockType.Chunk; }
        }

        /// <summary>
        /// Gets the computed size of this Chunk. Note: The set accessor cannot be used to change the size.
        /// </summary>
        public override uint Size
        {
            get { return CalculateSize(); }
            set { return; }
        }

        /// <summary>
        /// Gets the computed size of the header
        /// </summary>
        public uint HeaderSize
        {
            get { return 0x10 * ((uint)Entries.Count + 1); }
        }

        /// <summary>
        /// Calculate's the offsets for each entry
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
        /// Calculates the size of the Chunk
        /// </summary>
        /// <returns>The computed size of the Chunk</returns>
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

        public override string ToString()
        {
            return String.Format(fmt,
                MagicConverter.ToString(Magic),
                BaseOffset,
                Offset,
                Size,
                (HasParent) ? "Yes" : "No");
        }

        public ChunkBlock(int id, uint offset) : this(id, offset, null) { }
        public ChunkBlock(int id, uint offset, ChunkEntry parent)
        {
            ID = id;
            Offset = offset;
            Parent = parent;
        }
    }
}
