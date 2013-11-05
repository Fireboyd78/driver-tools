using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Zartex.Converters;

namespace Zartex.LogicExport
{
    public class WireCollectionEntry
    {
        public byte Unk { get; set; }
        public byte Opcode { get; set; }

        // public byte OpCodeBit1
        // {
        //     get { return (byte)(OpCode & 0xF); }
        // }
        // 
        // public byte OpCodeBit2
        // {
        //     get { return (byte)((OpCode & 0xF0) >> 4); }
        // }

        public short NodeId { get; set; }

        public WireCollectionEntry()
        {

        }
    }

    public class WireCollectionGroup
    {
        [TypeConverter(typeof(HexStringConverter))]
        public uint Offset { get; set; }

        public int Count { get; set; }
        public IList<WireCollectionEntry> Entries { get; set; }

        public WireCollectionGroup()
        {
            Entries = new List<WireCollectionEntry>();
        }

        public WireCollectionGroup(int count)
        {
            Count = count;
            Entries = new List<WireCollectionEntry>(count);
        }
    }
}
