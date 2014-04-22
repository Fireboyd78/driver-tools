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
    public delegate void BlockDataEventHandler(object sender, BlockDataEventArgs e);

    public class BlockDataEventArgs
    {
        public ChunkEntry Block { get; private set; }

        public long OldSize { get; private set; }
        public long NewSize { get; private set; }

        public int Difference
        {
            get { return (int)(NewSize - OldSize); }
        }

        public BlockDataEventArgs(long oldSize, long newSize)
        {
            OldSize = oldSize;
            NewSize = newSize;
        }
    }
}
