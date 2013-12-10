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
    /// <summary>
    /// Represents the base class for chunks/chunk data
    /// </summary>
    public abstract class Block
    {
        /// <summary>
        /// Gets or sets the zero-based ID of this block.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the magic number of this block.
        /// </summary>
        public virtual uint Magic { get; set; }

        /// <summary>
        /// Gets or sets the parent of this block.
        /// </summary>
        public Block Parent { get; set; }

        /// <summary>
        /// Gets the calculated base offset of this block.
        /// </summary>
        public uint BaseOffset
        {
            get
            {
                return (Parent != null) ? (BlockType != BlockType.Chunk) ? Parent.BaseOffset + Offset : Parent.BaseOffset : Offset;
            }
        }

        /// <summary>
        /// Gets or sets the offset to this block. This is usually relative to the parent (if it has one) - otherwise, it should be zero.
        /// </summary>
        public virtual uint Offset { get; set; }

        /// <summary>
        /// Gets or sets the size of this block. This should represent all data contained, depending on the <see cref="BlockType"/>.
        /// </summary>
        public virtual uint Size { get; set; }

        /// <summary>
        /// Gets the <see cref="BlockType"/> for this block.
        /// </summary>
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
