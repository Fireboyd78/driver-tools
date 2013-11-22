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
using DSCript.Methods;

namespace DSCript
{
    public abstract class Block
    {
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the magic number for this Block.
        /// </summary>
        public virtual uint Magic { get; set; }

        public Block Parent { get; set; }

        public uint BaseOffset
        {
            get
            {
                return (Parent != null) ? (BlockType != BlockType.Chunk) ? Parent.BaseOffset + Offset : Parent.BaseOffset : Offset;
            }
        }

        public virtual uint Offset { get; set; }

        public virtual uint Size { get; set; }

        public virtual BlockType BlockType
        {
            get { return BlockType.Block; }
        }

        public Block()
        {
            Parent = null;

            Offset = 0;
            Size = 0;
        }
    }
}
