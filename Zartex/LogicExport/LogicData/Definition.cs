using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

using DSCript.IO;
using DSCript.Object;

using Zartex.Converters;

namespace Zartex.LogicExport
{
    public abstract class LogicDefinition
    {
        protected uint _reserved;

        [TypeConverter(typeof(HexStringConverter))]
        public uint Offset { get; set; }

        public int Opcode { get; set; }
        
        public byte OpBit1Flag1
        {
            get { return (byte)(Opcode & 0xF); }
        }
        
        public byte OpBit1Flag2
        {
            get { return (byte)((Opcode & 0xF0) >> 4); }
        }

        public int StringId { get; set; }

        [Browsable(false)]
        [ReadOnly(true)]
        public virtual uint Reserved
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public byte Byte1 { get; set; }
        public byte Byte2 { get; set; }
        public byte Byte3 { get; set; }
        public byte Byte4 { get; set; }

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
        }

        public ushort Unknown { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public ushort Flags { get; set; }

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
        }

        [Browsable(false)]
        public List<LogicProperty> Properties { get; set; }

        protected LogicDefinition()
        {
            Properties = new List<LogicProperty>();
        }
    }

    public sealed class ActorDefinition : LogicDefinition
    {
        [Browsable(true)]
        [ReadOnly(true)]
        public override uint Reserved
        {
            get { return _reserved; }
            set { _reserved = value; }
        }

        public ActorDefinition()
        {
            _reserved = 0;
        }
    }

    public sealed class LogicNodeDefinition : LogicDefinition
    {
        [Browsable(false)]
        [ReadOnly(true)]
        public override uint Reserved
        {
            get { return 0; }
            set { return; }
        }

        public LogicNodeDefinition()
        {
        }
    }
}
