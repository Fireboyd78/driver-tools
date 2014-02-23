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

namespace DSCript
{
    public sealed class BlockEditor : IDisposable
    {
        /// <summary>
        /// The block data currently being edited by this <see cref="BlockEditor"/>
        /// </summary>
        public BlockData BlockData { get; private set; }

        /// <summary>
        /// The memory stream representing the buffer.
        /// </summary>
        public MemoryStream Stream { get; private set; }

        /// <summary>
        /// A <see cref="BinaryReader"/> based off of the memory stream representing the buffer.
        /// </summary>
        public BinaryReader Reader { get; private set; }

        /// <summary>
        /// A <see cref="BinaryWriter"/> based off of the memory stream representing the buffer.
        /// </summary>
        public BinaryWriter Writer { get; private set; }

        /// <summary>
        /// Commits all changes made to the buffer and applies them to the Block's data.
        /// </summary>
        public void Commit()
        {
            BlockData.Buffer = Stream.ToArray();
        }

        public void Dispose()
        {
            Writer.Dispose();
            Reader.Dispose();
            Stream.Dispose();
        }

        public BlockEditor(BlockData blockData)
        {
            BlockData = blockData;
            
            Stream = new MemoryStream(BlockData.Buffer, true);

            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);
        }
    }
}
