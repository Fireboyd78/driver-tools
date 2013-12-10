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
    public sealed class ChunkEntry : Block
    {
        public event BlockDataEventHandler SizeUpdated;

        void OnSizeUpdated(BlockDataEventArgs e)
        {
            if (SizeUpdated != null)
                SizeUpdated(this, e);
        }

        public new ChunkBlock Parent
        {
            get { return (ChunkBlock)base.Parent; }
            set { base.Parent = value; }
        }

        public byte Reserved { get; set; }

        public string Description { get; set; }

        public override uint Size
        {
            get { return base.Size; }
            set
            {
                long oldSize = base.Size;
                base.Size = value;

                OnSizeUpdated(new BlockDataEventArgs(oldSize, value));
            }
        }

        public override BlockType BlockType
        {
            get { return BlockType.ChunkItem; }
        }

        public ChunkEntry(int id, ChunkBlock parent)
        {
            ID = id;
            Parent = parent;
        }
    }
}
