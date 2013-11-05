using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.LogicExport
{
    public class LogicData
    {
        public class DefinitionTable
        {
            public byte Opcode { get; set; }
            public ushort OpBit { get; set; }

            public uint Index { get; set; }

            public ushort Field1 { get; set; }
            public ushort Field2 { get; set; }

            public ushort Field3 { get; set; }
            public ushort Field4 { get; set; }
        }

        public class PropertiesTable
        {
            public byte Opcode { get; set; }
            public ushort StrId { get; set; }

            public uint Field1 { get; set; }

            public object Flag1 { get; set; }
            public object Flag2 { get; set; }

            public uint __offset { get; set; }
        }

        public class PropertiesTableGroup
        {
            public List<PropertiesTable> Entries { get; set; }
        }

        public List<DefinitionTable> Definitions { get; set; }
        public List<PropertiesTableGroup> Properties { get; set; }

        public LogicData()
        {
            Definitions = new List<DefinitionTable>();
            Properties = new List<PropertiesTableGroup>();
        }
    }
}
