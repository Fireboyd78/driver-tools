using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.LogicExport
{
    public class Actors
    {
        public class ActorDefinition
        {
            public byte Bit1 { get; set; }
            public byte Sign { get; set; }
            public byte Bit2 { get; set; }
            public byte Reserved { get; set; }

            public uint Index { get; set; }

            public ushort Field1 { get; set; }
            public ushort Field2 { get; set; }

            public ushort Field3 { get; set; }
            public ushort Field4 { get; set; }
        }

        public class ActorProperties
        {
            public byte Bit1 { get; set; }
            public byte Sign { get; set; }
            public byte Bit2 { get; set; }
            public byte Reserved { get; set; }

            public uint Field1 { get; set; }

            public ushort Flag1 { get; set; }
            public ushort Flag2 { get; set; }
        }

        public class ActorPropertiesGroup
        {
            public uint Count { get; set; }
            public List<ActorProperties> Entries;
        }

        public List<ActorDefinition> Definitions { get; set; }
        public List<ActorPropertiesGroup> Properties { get; set; }

        public Actors()
        {
            Definitions = new List<ActorDefinition>();
            Properties = new List<ActorPropertiesGroup>();
        }
    }
}
