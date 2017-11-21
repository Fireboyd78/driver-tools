using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex
{
    public abstract class MissionObject
    {
        public abstract int Id { get; }
        public abstract int Size { get; }
    }
}
