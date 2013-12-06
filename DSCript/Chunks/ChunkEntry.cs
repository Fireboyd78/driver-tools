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

        private void OnSizeUpdated(BlockDataEventArgs e)
        {
            if (SizeUpdated != null)
                SizeUpdated(this, e);
        }

        const string fmt =
@":: Block ::
Magic: '{0}'
Description: {1}
Base Offset: 0x{2:X}
Offset: 0x{3:X}
Size: 0x{4:X}
--
";

        public new ChunkBlock Parent
        {
            get { return (ChunkBlock)base.Parent; }
            set { base.Parent = value; }
        }

        public int Reserved { get; set; }

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

        public override string ToString()
        {
            return String.Format(fmt,
                MagicConverter.ToString(Magic),
                Description,
                BaseOffset,
                Offset,
                Size);
        }

        public ChunkEntry(int id, ChunkBlock parent)
        {
            ID = id;
            Parent = parent;
        }
    }
}
