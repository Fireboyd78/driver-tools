using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex
{
    public class MissionObject_8 : MissionObject
    {
        public override int Id
        {
            get { return 8; }
        }

        public override int Size
        {
            get
            {
                if (Flags.Length == 0)
                    throw new Exception("Cannot return size of uninitialized block");
                
                return 0x1C;
            }
        }

        public int[] Flags;
    }
}
