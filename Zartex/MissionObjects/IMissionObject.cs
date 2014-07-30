using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public abstract class MissionObject
    {
        public int Offset { get; set; }

        public abstract int Id { get; }
        public abstract int Size { get; }
    }
}
