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

namespace DSCript.Models
{
    public class IndexData
    {
        public int Length
        {
            get { return 2; }
        }

        public short[] Buffer { get; set; }

        public short this[int id]
        {
            get { return Buffer[id]; }
            set { Buffer[id] = value; }
        }

        public IndexData(int count)
        {
            Buffer = new short[count];
        }
    }
}
