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
        public BlockData BlockData { get; private set; }

        public MemoryStream Stream { get; set; }

        /// <summary>
        /// Commits all changes made to the buffer and applies them to the Block's data.
        /// </summary>
        public void Commit()
        {
            BlockData.Data = Stream.ToArray();
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        public BlockEditor(BlockData blockData)
        {
            BlockData = blockData;
            Stream = new MemoryStream(BlockData.Data, true);
        }
    }
}
