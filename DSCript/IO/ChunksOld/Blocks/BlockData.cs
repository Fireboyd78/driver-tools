#if LEGACY
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DSCript.Legacy
{
    public class BlockData
    {
        [Browsable(false)]
        public MemoryStream Buffer { get; set; }

        public Block Parent { get; set; }

        public ChunkType Type { get; private set; }
        public uint Size { get; set; }
    }
}
#endif