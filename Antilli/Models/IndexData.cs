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

namespace Antilli.Models
{
    public class IndexData
    {
        public int Length
        {
            get { return 2; }
        }

        public ushort[] Buffer { get; set; }

        public ushort this[int id]
        {
            get { return Buffer[id]; }
            set { Buffer[id] = value; }
        }

        public IndexData(int count)
        {
            Buffer = new ushort[count];
        }
    }
}
