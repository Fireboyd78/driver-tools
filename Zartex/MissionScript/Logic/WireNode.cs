using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zartex
{
    public class WireNode
    {
        public byte WireType { get; set; }
        public byte OpCode { get; set; }

        public short NodeId { get; set; }

        public WireNodeType GetWireNodeType()
        {
            return (WireNodeType)WireType;
        }
    }
}
