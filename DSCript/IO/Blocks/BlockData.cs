using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DSCript.IO
{
    public class BlockData
    {
        [Browsable(false)]
        public MemoryStream Buffer { get; set; }

        public BlockOld Parent { get; set; }

        public CTypes Type { get; private set; }
        public uint Size { get; set; }
    }
}
