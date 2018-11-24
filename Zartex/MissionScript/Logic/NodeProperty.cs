using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using System.Text;

using DSCript;

using Zartex.Converters;

namespace Zartex
{
    public abstract class NodeProperty
    {
        protected object _value;
        protected int _op;

        [Category("Type"), Description("Defines what type of property this is (Integer, Float, Boolean, etc.)")]
        [PropertyOrder(10)]
        public virtual int OpCode
        {
            get { return _op; }
            protected set { _op = value; }
        }

        [Category("Type"), Description("The System.Type of the Value (for debugging purposes only)")]
        [PropertyOrder(20), ReadOnly(true)]
        public virtual Type Type
        {
            get { return typeof(object); }
            protected set { return; }
        }

        [Category("Type"), Description("Notes about this format (if any)")]
        [PropertyOrder(30), ReadOnly(true)]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public virtual string Notes
        {
            get { return ""; }
        }

        [Category("Properties"), Description("The ID that corresponds to an entry in the String Collection.")]
        [PropertyOrder(200)]
        public short StringId { get; set; }

        [Category("Properties"), Description("The value assigned to this node.")]
        [PropertyOrder(400)]
        public virtual object Value
        {
            get
            {
                if (_value == null) throw new NullReferenceException();
                return Convert.ChangeType(_value, this.Type);
            }
            set { _value = value; }
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }

    public sealed class UnknownProperty : NodeProperty
    {
        private Type _type;

        public override int OpCode
        {
            get { return base.OpCode; }
        }

        public override Type Type
        {
            get { return _type; }
            protected set { _type = value; }
        }

        public override string Notes
        {
            get { return "Research needed. Please inform the lead programmer of any notes you may have regarding this format."; }
        }

        public UnknownProperty(int opcode, object value)
        {
            OpCode = opcode;
            Value = value;

            Type = value.GetType();
        }
    }

    public class IntegerProperty : NodeProperty
    {
        public override int OpCode
        {
            get { return 1; }
        }

        public override Type Type
        {
            get { return typeof(int); }
        }

        public override string Notes
        {
            get { return "Represents a 32-bit integer."; }
        }

        public new int Value
        {
            get { return (int)base.Value; }
            set { base.Value = value; }
        }
        
        public IntegerProperty(int value)
        {
            Value = value;
        }
    }

    public sealed class FloatProperty : NodeProperty
    {
        public override int OpCode
        {
            get { return 2; }
        }

        public override Type Type
        {
            get { return typeof(float); }
        }

        public override string Notes
        {
            get { return "Represents a single-precision float value."; }
        }

        public new float Value
        {
            get { return (float)base.Value; }
            set { base.Value = value; }
        }

        public FloatProperty(float value)
        {
            Value = value;
        }
    }
    
    public sealed class BooleanProperty : NodeProperty
    {
        public override int OpCode
        {
            get { return 4; }
        }

        public override Type Type
        {
            get { return typeof(bool); }
        }

        public override string Notes
        {
            get { return "Represents a boolean (true/false, cops on/off, etc.)"; }
        }
        
        public new bool Value
        {
            get { return (bool)base.Value; }
            set { base.Value = value; }
        }

        public BooleanProperty(bool value)
        {
            Value = value;
        }
    }

    public sealed class EnumProperty : IntegerProperty
    {
        public override int OpCode
        {
            get { return 6; }
        }

        public override string Notes
        {
            get { return "Represents the value of an enumeration."; }
        }

        public EnumProperty(int value) : base(value) { }
    }

    public sealed class ActorProperty : IntegerProperty
    {
        public override int OpCode
        {
            get { return 7; }
        }

        public override string Notes
        {
            get { return "Represents the ID of an actor in the Actors table."; }
        }

        public ActorProperty(int value) : base(value) { }
    }

    public class StringProperty : NodeProperty
    {
        public override int OpCode
        {
            get { return 3; }
        }

        public override string Notes
        {
            get { return "Represents the ID of a string that defines a filename for something (recordings, etc.)"; }
        }

        public StringProperty(short value)
        {
            Value = value;
        }
    }

    public class AIPersonalityProperty : NodeProperty
    {
        public override int OpCode
        {
            get { return 8; }
        }

        public override string Notes
        {
            get { return "Represents the ID of a string that defines a character personality as well as its corresponding index."; }
        }

        [Category("Properties"), Description("The personality index.")]
        [PropertyOrder(500)]
        public short PersonalityIndex { get; set; }

        public AIPersonalityProperty(short value, short reserved)
        {
            Value = value;
            PersonalityIndex = reserved;
        }
    }
    
    public sealed class FlagsProperty : IntegerProperty
    {
        public override int OpCode
        {
            get { return 9; }
        }

        public override string Notes
        {
            get { return "Represents a flag."; }
        }

        public FlagsProperty(int value) : base(value) { }
    }

    public struct AudioInfo
    {
        public int Bank;
        public int Sample;

        public override string ToString()
        {
            return $"({Bank}, {Sample})";
        }

        public AudioInfo(int bank, int sample)
        {
            Bank = bank;
            Sample = sample;
        }
    }

    public sealed class AudioProperty : NodeProperty
    {
        public override int OpCode
        {
            get { return 11; }
        }

        public override Type Type
        {
            get { return typeof(AudioInfo); }
        }

        public override string Notes
        {
            get { return "Represents an audio bank and sound sample index."; }
        }
        
        public new AudioInfo Value
        {
            get { return (AudioInfo)base.Value; }
            set { base.Value = value; }
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public AudioProperty(int bank, int sample)
        {
            Value = new AudioInfo(bank, sample);
        }

        public AudioProperty(AudioInfo value)
        {
            Value = value;
        }
    }

    public sealed class Float4Property : NodeProperty
    {
        public override int OpCode
        {
            get { return 17; }
        }

        public override Type Type
        {
            get { return typeof(Vector4); }
        }

        public override string Notes
        {
            get { return "Represents a set of XYZW coordinates."; }
        }

        public new Vector4 Value
        {
            get { return (Vector4)base.Value; }
            set { base.Value = value; }
        }

        public override string ToString()
        {
            return $"[{Value.X:F}, {Value.Y:F}, {Value.Z:F}, {Value.W:F}]";
        }

        public Float4Property(Vector4 value)
        {
            Value = value;
        }
    }

    public sealed class WireCollectionProperty : IntegerProperty
    {
        public override int OpCode
        {
            get { return 19; }
        }

        public override string Notes
        {
            get { return "Represents the ID of a Wire Collection."; }
        }

        public WireCollectionProperty(int value) : base(value) { }
    }

    public sealed class LocalisedStringProperty : IntegerProperty
    {
        public override int OpCode
        {
            get { return 20; }
        }

        public override string Notes
        {
            get { return "Represents the ID of a localised string."; }
        }

        public LocalisedStringProperty(int value) : base(value) { }
    }

    public sealed class UnicodeStringProperty : NodeProperty
    {
        public override int OpCode
        {
            get { return 21; }
        }

        public override Type Type
        {
            get { return typeof(string); }
        }

        public override string Notes
        {
            get { return "Represents a raw Unicode string."; }
        }

        public new string Value
        {
            get { return (string)base.Value; }
            set { base.Value = value; }
        }

        public UnicodeStringProperty(string value)
        {
            Value = value;
        }
    }

    public sealed class RawDataProperty : NodeProperty
    {
        public override int OpCode
        {
            get { return 22; }
        }

        public override Type Type
        {
            get { return typeof(byte[]); }
        }

        public override string Notes
        {
            get { return "Represents a raw-data buffer."; }
        }

        public override string ToString()
        {
            return $"byte[{((byte[])Value).Length}]";
        }

        public RawDataProperty(byte[] value)
        {
            Value = value;
        }
    }
}
