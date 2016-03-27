using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public class WireCollection
    {
        public List<WireNode> Wires { get; set; }

        public WireNode this[int index]
        {
            get { return Wires[index]; }
        }

        public WireCollection(int nWires)
        {
            Wires = new List<WireNode>(nWires);
        }
    }
}