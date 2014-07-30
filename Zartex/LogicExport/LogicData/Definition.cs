using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

using DSCript.Legacy;
using DSCript.Object;

using Zartex.Converters;

namespace Zartex.LogicExport
{
    public class LogicDefinition
    {
        [TypeConverter(typeof(HexStringConverter))]
        public int Offset { get; set; }

        public byte Opcode { get; set; }
        
        /*
        public byte OpBit1Flag1
        {
            get { return (byte)(Opcode & 0xF); }
        }
        
        public byte OpBit1Flag2
        {
            get { return (byte)((Opcode & 0xF0) >> 4); }
        }*/

        public short StringId { get; set; }

        [Browsable(false)]
        [ReadOnly(true)]
        public virtual int Reserved { get; set; }

        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        /*
        public byte Byte1Bit1
        {
            get { return (byte)(Byte1 & 0xF); }
        }

        public byte Byte1Bit2
        {
            get { return (byte)((Byte1 & 0xF0) >> 4); }
        }

        public byte Byte2Bit1
        {
            get { return (byte)(Byte2 & 0xF); }
        }

        public byte Byte2Bit2
        {
            get { return (byte)((Byte2 & 0xF0) >> 4); }
        }

        public byte Byte3Bit1
        {
            get { return (byte)(Byte3 & 0xF); }
        }

        public byte Byte3Bit2
        {
            get { return (byte)((Byte3 & 0xF0) >> 4); }
        }

        public byte Byte4Bit1
        {
            get { return (byte)(Byte4 & 0xF); }
        }

        public byte Byte4Bit2
        {
            get { return (byte)((Byte4 & 0xF0) >> 4); }
        }*/

        [Browsable(false)]
        public short Unknown { get; set; }

        [Browsable(false)]
        public short Flags { get; set; }

        /*
        public byte FlagByte1
        {
            get { return (byte)(Flags & 0xFF); }
        }

        public byte FlagByte2
        {
            get { return (byte)((Flags & 0xFF00) >> 8); }
        }

        
        public byte FlagByte1Bit1
        {
            get { return (byte)(Flags & 0xF); }
        }

        public byte FlagByte1Bit2
        {
            get { return (byte)((Flags & 0xF0) >> 4); }
        }

        public byte FlagByte2Bit1
        {
            get { return (byte)((Flags & 0xF00) >> 8); }
        }

        public byte FlagByte2Bit2
        {
            get { return (byte)((Flags & 0xF000) >> 12); }
        }*/

        [Browsable(false)]
        public List<LogicProperty> Properties { get; set; }

        public LogicDefinition()
        {
            Properties = new List<LogicProperty>();
        }
    }

    public sealed class ActorDefinition : LogicDefinition
    {
        [Browsable(true)]
        [ReadOnly(true)]
        public override int Reserved
        {
            get { return base.Reserved; }
            set { base.Reserved = value; }
        }

        public ActorDefinition()
        {
        }
    }

    public sealed class LogicNodeDefinition : LogicDefinition
    {
        public LogicNodeDefinition()
        {
        }
    }
}
