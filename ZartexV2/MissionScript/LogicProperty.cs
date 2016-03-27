using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using System.Text;

namespace Zartex
{
    public interface ILogicProperty
    {
        int OpCode { get; }
        short StringId { get; set; }

        int SizeOf { get; }

        object Value { get; }
    }

    public interface IEditProperty
    {
        string Notes { get; }
        Type PropertyType { get; }
    }

    public abstract class LogicProperty<T> : ILogicProperty, IEditProperty
    {
        object ILogicProperty.Value
        {
            get { return (object)Value; }
        }

        [Category("Information"), Description("The opcode describing this property.")]
        [ReadOnly(true)]
        public abstract int OpCode { get; }
        
        [Category("Information"), Description("Notes about this format (if any).")]
        [ReadOnly(true)]
        public virtual string Notes
        {
            get { return ""; }
        }

        [Category("Information"), Description("The size (in bytes) of this property's value.")]
        [ReadOnly(true)]
        public abstract int SizeOf { get; }

        [Category("Information"), Description("The System.Type of this property.")]
        [ReadOnly(true)]
        public Type PropertyType
        {
            get { return typeof(T); }
        }

        [Category("Properties"), Description("The ID that corresponds to an entry in the String Collection.")]
        public short StringId { get; set; }

        [Category("Properties"), Description("The value assigned to this node.")]
        public T Value { get; set; }
    }

    public class IntegerProperty : LogicProperty<int>
    {
        public override int OpCode
        {
            get { return 1; }
        }

        public override string Notes
        {
            get { return "Represents a 32-bit integer."; }
        }

        public override int SizeOf
        {
            get { return sizeof(int); }
        }

        public IntegerProperty(int value)
        {
            Value = value;
        }
    }

    public sealed class FloatProperty : LogicProperty<float>
    {
        public override int OpCode
        {
            get { return 2; }
        }

        public override string Notes
        {
            get { return "Represents a single-precision float value."; }
        }

        public override int SizeOf
        {
            get { return sizeof(float); }
        }

        public FloatProperty(float value)
        {
            Value = value;
        }
    }

    public sealed class FilenameProperty : IntegerProperty
    {
        public override int OpCode
        {
            get { return 3; }
        }

        public override string Notes
        {
            get { return "Represents the ID of a string that defines a filename for something (recordings, etc.)"; }
        }

        public FilenameProperty(int value) : base(value) { }
    }

    public sealed class BooleanProperty : LogicProperty<bool>
    {
        public override int OpCode
        {
            get { return 4; }
        }

        public override string Notes
        {
            get { return "Represents a boolean (true/false, cops on/off, etc.)"; }
        }

        public override int SizeOf
        {
            get { return sizeof(bool); }
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

    public sealed class StringProperty : IntegerProperty
    {
        public override int OpCode
        {
            get { return 8; }
        }

        public override string Notes
        {
            get { return "Represents the ID of a string."; }
        }

        public StringProperty(int value) : base(value) { }
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

    public sealed class AudioProperty : LogicProperty<long>
    {
        public override int OpCode
        {
            get { return 11; }
        }

        public override string Notes
        {
            get { return "Research required."; }
        }

        public override int SizeOf
        {
            get { return sizeof(long); }
        }

        public AudioProperty(long value)
        {
            Value = value;
        }
    }

    public sealed class Float4Property : LogicProperty<Vector4>
    {
        public override int OpCode
        {
            get { return 17; }
        }

        public override string Notes
        {
            get { return "Represents a set of XYZW coordinates."; }
        }

        public override int SizeOf
        {
            get { return (sizeof(float) * 4); }
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

    public sealed class UnicodeStringProperty : LogicProperty<string>
    {
        public override int OpCode
        {
            get { return 21; }
        }

        public override string Notes
        {
            get { return "Represents a raw Unicode string."; }
        }

        public override int SizeOf
        {
            get
            {
                if (Value != null)
                    return (Value.Length * 2);

                return 0;
            }
        }

        public UnicodeStringProperty(string value)
        {
            Value = value;
        }
    }

    public sealed class RawDataProperty : LogicProperty<byte[]>
    {
        public override int OpCode
        {
            get { return 22; }
        }

        public override string Notes
        {
            get { return "Represents a raw-data buffer."; }
        }

        public override int SizeOf
        {
            get
            {
                if (Value != null)
                    return Value.Length;

                return 0;
            }
        }

        public RawDataProperty(byte[] value)
        {
            Value = value;
        }
    }
}
