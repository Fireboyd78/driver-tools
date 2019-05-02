using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.IO;
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
        protected int _typeId;

        [Category("Type"), Description("Defines what type of property this is (Integer, Float, Boolean, etc.)")]
        [PropertyOrder(10)]
        public virtual int TypeId
        {
            get { return _typeId; }
            internal set { _typeId = value; }
        }
        
        [Category("Type"), Description("The size of this type, in number of bytes.")]
        [PropertyOrder(40), ReadOnly(true)]
        public abstract int Size { get; }

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
            get { return _value; }
            set { _value = value; }
        }
        
        public override string ToString()
        {
            return _value.ToString();
        }

        public abstract void LoadData(Stream stream);
        public abstract void SaveData(Stream stream);

        public static Type GetPropertyTypeById(int type)
        {
            switch (type)
            {
            case 1: return typeof(IntegerProperty);
            case 2: return typeof(FloatProperty);
            case 3: return typeof(StringProperty);
            case 4: return typeof(BooleanProperty);
            case 6: return typeof(EnumProperty);
            case 9: return typeof(FlagsProperty);
            case 7: return typeof(ActorProperty);
            case 8: return typeof(TextFileItemProperty);
            case 11: return typeof(AudioProperty);
            case 17: return typeof(Float3Property);
            case 19: return typeof(WireCollectionProperty);
            case 20: return typeof(LocalisedStringProperty);
            case 21: return typeof(UnicodeStringProperty);
            case 22: return typeof(RawDataProperty);
            }

            // nothing found :(
            return null;
        }

        public static NodeProperty Create(Stream stream)
        {
            // ignore padding
            var typeId = (stream.ReadInt16() & 0xFF);
            var strId = stream.ReadInt16();

            var type = GetPropertyTypeById(typeId);

            if (type == null)
                throw new InvalidOperationException($"Unsupported property type '{typeId}'!");

            var result = Activator.CreateInstance(type, true) as NodeProperty;

            result.StringId = strId;
            result.LoadData(stream);
            
            return result;
        }

        public void WriteTo(Stream stream)
        {
            stream.WriteByte(TypeId);
            stream.WriteByte(0x3E);
            stream.Write(StringId);

            SaveData(stream);
        }
    }

    public class IntegerProperty : NodeProperty
    {
        public override int TypeId
        {
            get { return 1; }
        }
        
        public override int Size
        {
            get { return 4; }
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

        public override void LoadData(Stream stream)
        {
            var size = stream.ReadInt32();

            if (size != Size)
                throw new InvalidOperationException("Invalid integer property!");

            Value = stream.ReadInt32();
        }

        public override void SaveData(Stream stream)
        {
            stream.Write(Size);
            stream.Write(Value);
        }

        public IntegerProperty() { }
        public IntegerProperty(int value)
        {
            Value = value;
        }
    }

    public sealed class FloatProperty : NodeProperty
    {
        public override int TypeId
        {
            get { return 2; }
        }

        public override int Size
        {
            get { return 4; }
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

        public override void LoadData(Stream stream)
        {
            var size = stream.ReadInt32();

            if (size != Size)
                throw new InvalidOperationException("Invalid float property!");

            Value = stream.ReadSingle();
        }

        public override void SaveData(Stream stream)
        {
            stream.Write(Size);
            stream.Write(Value);
        }

        public FloatProperty() { }
        public FloatProperty(float value)
        {
            Value = value;
        }
    }
    
    public sealed class BooleanProperty : NodeProperty
    {
        public override int TypeId
        {
            get { return 4; }
        }
        
        public override int Size
        {
            get { return 1; }
        }

        public override string Notes
        {
            get { return "Represents a boolean (true/false) value."; }
        }
        
        public new bool Value
        {
            get { return (bool)base.Value; }
            set { base.Value = value; }
        }

        public override void LoadData(Stream stream)
        {
            var size = stream.ReadInt32();

            if (size == Size)
            {
                Value = (stream.ReadByte() != 0);
            }
            else
            {
                // explicitly false
                Value = false;
            }
        }

        public override void SaveData(Stream stream)
        {
            stream.Write(Size);
            stream.WriteByte(Value ? 255 : 0);
        }

        public BooleanProperty() { }
        public BooleanProperty(bool value)
        {
            Value = value;
        }
    }

    public sealed class EnumProperty : IntegerProperty
    {
        public override int TypeId
        {
            get { return 6; }
        }
        
        public override string Notes
        {
            get { return "Represents the value of an enumeration."; }
        }

        public EnumProperty() { }
        public EnumProperty(int value) : base(value) { }
    }

    public sealed class ActorProperty : IntegerProperty
    {
        public override int TypeId
        {
            get { return 7; }
        }

        public override string Notes
        {
            get { return "Represents the ID of an actor in the Actors table."; }
        }

        public ActorProperty() { }
        public ActorProperty(int value) : base(value) { }
    }

    public class StringProperty : NodeProperty
    {
        public override int TypeId
        {
            get { return 3; }
        }

        public override int Size
        {
            get { return 2; }
        }

        public override string Notes
        {
            get { return "Represents the ID of a string."; }
        }

        public new short Value
        {
            get { return (short)base.Value; }
            set { base.Value = value; }
        }

        public override void LoadData(Stream stream)
        {
            var size = stream.ReadInt32();

            if (size != Size)
                throw new InvalidOperationException("Invalid string property!");

            Value = stream.ReadInt16();
        }

        public override void SaveData(Stream stream)
        {
            stream.Write(Size);
            stream.Write(Value);
        }

        public StringProperty() { }
        public StringProperty(short value)
        {
            Value = value;
        }
    }

    public class TextFileItemProperty : NodeProperty
    {
        public override int TypeId
        {
            get { return 8; }
        }

        public override int Size
        {
            get { return 4; }
        }

        public override string Notes
        {
            get { return "Represents the ID of a string that defines a filename, as well as an index."; }
        }

        public new short Value
        {
            get { return (short)base.Value; }
            set { base.Value = value; }
        }

        [Category("Properties")]
        [PropertyOrder(500)]
        public short Index { get; set; }

        public override void LoadData(Stream stream)
        {
            var size = stream.ReadInt32();

            if (size != Size)
                throw new InvalidOperationException("Invalid TextFile item property!");

            Value = stream.ReadInt16();
            Index = stream.ReadInt16();
        }

        public override void SaveData(Stream stream)
        {
            stream.Write(Size);

            stream.Write(Value);
            stream.Write(Index);
        }

        public TextFileItemProperty() { }
        public TextFileItemProperty(short value, short index)
        {
            Value = value;
            Index = index;
        }
    }
    
    public sealed class FlagsProperty : IntegerProperty
    {
        public override int TypeId
        {
            get { return 9; }
        }

        public override string Notes
        {
            get { return "Represents a set of flags."; }
        }

        public FlagsProperty() { }
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
        public override int TypeId
        {
            get { return 11; }
        }
        
        public override int Size
        {
            get { return 8; }
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

        public override void LoadData(Stream stream)
        {
            var size = stream.ReadInt32();

            if (size != Size)
                throw new InvalidOperationException("Invalid audio property!");

            Value = stream.Read<AudioInfo>(size);
        }

        public override void SaveData(Stream stream)
        {
            stream.Write(Size);
            stream.Write(Value);
        }

        public AudioProperty(int bank, int sample)
        {
            Value = new AudioInfo(bank, sample);
        }

        public AudioProperty() { }
        public AudioProperty(AudioInfo value)
        {
            Value = value;
        }
    }

    public abstract class VectorProperty : NodeProperty
    {
        public override int Size
        {
            get { return 16; }
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

        public override void LoadData(Stream stream)
        {
            var size = stream.ReadInt32();

            if (size != Size)
                throw new InvalidOperationException("Invalid vector property!");

            Value = stream.Read<Vector4>(size);
        }

        public override void SaveData(Stream stream)
        {
            stream.Write(Size);
            stream.Write(Value);
        }

        protected VectorProperty() { }
        protected VectorProperty(Vector4 value)
        {
            Value = value;
        }
    }

    public sealed class Float4Property : VectorProperty
    {
        public override int TypeId
        {
            get { return 10; }
        }

        public override string Notes
        {
            get { return "Represents a set of XYZW coordinates."; }
        }

        public Float4Property(Vector4 value)
            : base(value) { }
    }

    public sealed class Float3Property : VectorProperty
    {
        public override int TypeId
        {
            get { return 17; }
        }
        
        public override string Notes
        {
            get { return "Represents a set of XYZ coordinates."; }
        }

        public Float3Property() { }
        public Float3Property(Vector4 value)
            : base(value) { }
    }

    public sealed class WireCollectionProperty : IntegerProperty
    {
        public override int TypeId
        {
            get { return 19; }
        }

        public override string Notes
        {
            get { return "Represents the ID of a Wire Collection."; }
        }

        public WireCollectionProperty() { }
        public WireCollectionProperty(int value) : base(value) { }
    }

    public sealed class LocalisedStringProperty : IntegerProperty
    {
        public override int TypeId
        {
            get { return 20; }
        }

        public override string Notes
        {
            get { return "Represents the ID of a localised string."; }
        }

        public LocalisedStringProperty() { }
        public LocalisedStringProperty(int value) : base(value) { }
    }

    public sealed class UnicodeStringProperty : NodeProperty
    {
        public override int TypeId
        {
            get { return 21; }
        }
        
        public override int Size
        {
            get { return Encoding.Unicode.GetByteCount(Value); }
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

        public override void LoadData(Stream stream)
        {
            var size = stream.ReadInt32();
            var buf = stream.ReadBytes(size);

            Value = Encoding.Unicode.GetString(buf);
        }

        public override void SaveData(Stream stream)
        {
            var buf = Encoding.Unicode.GetBytes(Value);

            stream.Write(buf.Length);
            stream.Write(buf);
        }

        public UnicodeStringProperty() { }
        public UnicodeStringProperty(string value)
        {
            Value = value;
        }
    }

    public sealed class RawDataProperty : NodeProperty
    {
        public override int TypeId
        {
            get { return 22; }
        }
        
        public override int Size
        {
            get { return Value.Length; }
        }

        public override string Notes
        {
            get { return "Represents a raw-data buffer."; }
        }

        public new byte[] Value
        {
            get { return (byte[])base.Value; }
            set { base.Value = value; }
        }

        public override string ToString()
        {
            return $"byte[{Value.Length}]";
        }

        public override void LoadData(Stream stream)
        {
            var size = stream.ReadInt32();
            var buffer = new byte[size];

            stream.Read(buffer, 0, size);

            Value = buffer;
        }

        public override void SaveData(Stream stream)
        {
            stream.Write(Size);
            stream.Write(Value);
        }

        public RawDataProperty() { }

        public RawDataProperty(int length)
        {
            Value = new byte[length];
        }

        public RawDataProperty(Stream stream)
        {
            LoadData(stream);
        }
        
        public RawDataProperty(byte[] value)
        {
            Value = value;
        }
    }
}
