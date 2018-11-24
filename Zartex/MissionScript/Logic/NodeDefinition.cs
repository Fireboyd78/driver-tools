using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using System.Text;

using Zartex.Converters;

namespace Zartex
{
    public class NodeDefinition
    {
        public byte OpCode { get; set; }

        public short StringId { get; set; }

        public NodeColor Color { get; set; }

        public short Reserved { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public short Flags { get; set; }

        public List<NodeProperty> Properties { get; set; }
    }
}
