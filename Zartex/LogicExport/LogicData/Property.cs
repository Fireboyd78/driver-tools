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

namespace Zartex.LogicExport
{
    public abstract class LogicProperty
    {
        protected object _value;
        protected int _op;

        [Category("Type"), Description("Defines what type of property this is (Integer, Float, Boolean, etc.)")]
        [PropertyOrder(10)]
        public virtual int Opcode
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

        [Category("Properties"), Description("The absolute offset of this definition in the file (in hexadecimal).")]
        [PropertyOrder(100), ReadOnly(true), TypeConverter(typeof(HexStringConverter))]
        public uint Offset { get; set; }

        [Category("Properties"), Description("The ID that corresponds to an entry in the String Collection.")]
        [PropertyOrder(200)]
        public int StringId { get; set; }

        [Category("Properties"), Description("Related to the size of the value.")]
        [PropertyOrder(300), ReadOnly(true)]
        public int Reserved { get; set; }

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

        // Allow implementation
        protected LogicProperty() {}
    }

    public sealed class UnknownProperty : LogicProperty
    {
        private Type _type;

        public override int Opcode
        {
            get { return base.Opcode; }
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
            Opcode = opcode;
            Value = value;

            Type = value.GetType();
        }
    }

    // Opcode: 1
    public sealed class IntegerProperty : LogicProperty
    {
        public override int Opcode
        {
            get { return 1; }
        }

        public override Type Type
        {
            get { return typeof(int); }
        }

        public override string Notes
        {
            get { return "A 32-bit integer with various uses depending on the property type."; }
        }

        public IntegerProperty() : this(0) { }
        public IntegerProperty(int value)
        {
            Value = value;
        }
    }

    // Opcode: 2
    public sealed class FloatProperty : LogicProperty
    {
        public override int Opcode
        {
            get { return 2; }
        }

        public override Type Type
        {
            get { return typeof(float); }
        }

        public override string Notes
        {
            get { return "A floating-point value with various uses depending on the property.\r\npHealth/pFelony use it as 0% - 100% (0.0 - 1.0).\r\npInterval uses it as seconds (2.5 seconds)."; }
        }

        public FloatProperty() : this(0.0) { }
        public FloatProperty(double value) : this(Convert.ToSingle(value)) { }
        public FloatProperty(float value)
        {
            Value = value;
        }
    }

    // Opcode 3
    public sealed class FilenameProperty : LogicProperty
    {
        public override int Opcode
        {
            get { return 3; }
        }

        public override Type Type
        {
            get { return typeof(string); }
        }

        public override string Notes
        {
            get { return "The ID of a string in the String Collection that defines a filename for something (recordings, etc.)"; }
        }

        public FilenameProperty() : this(String.Empty) { }
        public FilenameProperty(string value)
        {
            Value = value;
        }
    }

    // Opcode: 4
    public sealed class BooleanProperty : LogicProperty
    {
        public override int Opcode
        {
            get { return 4; }
        }

        public override Type Type
        {
            get { return typeof(bool); }
        }

        public override string Notes
        {
            get { return "A boolean that could represent True/False or On/Off (cops on/off, etc.)"; }
        }

        [TypeConverter(typeof(BooleanConverter))]
        public override object Value
        {
            get { return base.Value; }
            set { base.Value = value; }
        }

        public BooleanProperty() : this(false) { }
        public BooleanProperty(bool value)
        {
            Value = value;
        }
    }

    // TODO: Opcode 6 (Enum) > pType
    // TODO: Opcode 7 (Actor) > pActor
    public sealed class ActorProperty : LogicProperty
    {
        public override int Opcode
        {
            get { return 7; }
        }

        public override Type Type
        {
            get { return typeof(int); }
        }

        public override string Notes
        {
            get { return "The ID of an actor in the Actors table."; }
        }

        public ActorProperty(int value)
        {
            Value = value;
        }
    }

    // Opcode 8
    public sealed class StringProperty : LogicProperty
    {
        public override int Opcode
        {
            get { return 8; }
        }

        public override Type Type
        {
            get { return typeof(string); }
        }

        public override string Notes
        {
            get { return "The ID for a generic string in the String Collection."; }
        }

        public StringProperty() : this(String.Empty) { }
        public StringProperty(string value)
        {
            Value = value;
        }
    }

    // TODO: Opcode 9 (Flags) > pFlags

    // TODO: Opcode 11 (Audio)
    public sealed class AudioProperty : LogicProperty
    {
        public override int Opcode
        {
            get { return 11; }
        }

        public override Type Type
        {
            get { return typeof(long); }
        }

        public override string Notes
        {
            get { return "Research needed. It's two UInt32s combined into one UInt64."; }
        }

        public AudioProperty(long value)
        {
            Value = value;
        }
    }

    // TODO: Opcode 17 (Float4)
    public sealed class Float4Property : LogicProperty
    {
        public override int Opcode
        {
            get { return 17; }
        }

        public override Type Type
        {
            get { return typeof(float[]); }
        }

        public override string Notes
        {
            get { return "Four floating-point values, maybe used to place objects?"; }
        }

        public Float4Property(float[] value)
        {
            Value = value;
        }
    }

    // TODO: Opcode 19 (Wire Collection) > pWireCollection
    public sealed class WireCollectionProperty : LogicProperty
    {
        public override int Opcode
        {
            get { return 19; }
        }

        public override Type Type
        {
            get { return typeof(int); }
        }

        public override string Notes
        {
            get { return "The ID of a Wire Collection entry."; }
        }

        public WireCollectionProperty(int value)
        {
            Value = value;
        }
    }


    // TODO: Opcode 20 (Locale)
    public sealed class LocalisedStringProperty : LogicProperty
    {
        public override int Opcode
        {
            get { return 20; }
        }

        public override Type Type
        {
            get { return typeof(int); }
        }

        public override string Notes
        {
            get { return "Loads a string with the assigned ID from the localised text."; }
        }

        public LocalisedStringProperty(int value)
        {
            Value = value;
        }
    }

    // Opcode 21
    public sealed class UnicodeStringProperty : LogicProperty
    {
        // the accessor to _value to ensure it stays a byte[] array
        private byte[] unicode
        {
            get { return (_value != null) ? (byte[])_value : new byte[]{}; }
            set { _value = value; }
        }

        public override int Opcode
        {
            get { return 21; }
        }

        public override Type Type
        {
            get { return typeof(byte[]); }
        }

        public override string Notes
        {
            get { return "The ID for a unicode string in the String Collection (rarely used)"; }
        }

        public override object Value
        {
            get
            {
                return Encoding.Unicode.GetString(unicode);
            }
            set
            {
                if (value.GetType() == typeof(byte[]))
                    unicode = (byte[])value;
                else if (value.GetType() == typeof(string))
                    unicode = Encoding.Unicode.GetBytes((string)value);
                else throw new Exception("Unicode strings accept either an array of bytes or a string.");
            }
        }

        public override string ToString()
        {
            return Encoding.Unicode.GetString(unicode);
        }

        public UnicodeStringProperty() {}
        public UnicodeStringProperty(string value)
        {
            Value = value;
        }
        public UnicodeStringProperty(byte[] value)
        {
            Value = value;
        }
    }

    // Opcode 22
    public sealed class RawDataProperty : LogicProperty
    {
        public override int Opcode
        {
            get { return 22; }
        }

        public override Type Type
        {
            get { return typeof(byte[]); }
        }

        public override string Notes
        {
            get { return "No idea. It's probably leftover or something."; }
        }

        public RawDataProperty(byte[] value)
        {
            Value = value;
        }
    }
}
