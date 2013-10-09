using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace DSCript.IO
{
    public sealed class ChunkBlock : Block
    {
        public const CTypes Magic = CTypes.CHUNK;

        public int SubCount { get; set; }
        public const int Version = 0x3;
        
        public List<SubChunkBlock> Subs = new List<SubChunkBlock>();

        public bool IsRoot
        {
            get { return (Parent == null) ? true : false; }
        }

        [Browsable(false)]
        public ChunkBlock(int id, uint offset) : this(id, offset, null) { }

        [Browsable(false)]
        public ChunkBlock(int id, uint offset, SubChunkBlock parent)
        {
            ID = id;
            BaseOffset = offset;
            Parent = parent;
        }
    }
}
