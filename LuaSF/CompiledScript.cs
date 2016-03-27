using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LuaSF
{
    public class CompiledScript
    {
        public int Hash { get; set; }

        public TimeSpan Timestamp { get; set; }

        public string Filename { get; set; }

        public byte[] Buffer { get; set; }
    }
}
