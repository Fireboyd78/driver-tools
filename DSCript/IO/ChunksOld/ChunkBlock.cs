#if LEGACY
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DSCript.Legacy
{
    public sealed class ChunkBlock : Block
    {
        public const ChunkType Magic = ChunkType.Chunk;

        public uint SubCount { get; set; }
        public const uint Version = 0x3;
        
        public List<SubChunkBlock> Subs = new List<SubChunkBlock>();

        public bool IsRoot
        {
            get { return (Parent == null) ? true : false; }
        }

        public ChunkBlock(int id, uint offset) : this(id, offset, null) { }

        public ChunkBlock(int id, uint offset, SubChunkBlock parent)
        {
            ID = id;
            BaseOffset = offset;
            Parent = parent;
        }
    }
}
#endif