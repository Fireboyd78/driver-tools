using DSCript.Models;
using DSCript.Spooling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace DSCript.Menus
{
    public enum ElementType : int
    {
        Effect      = -1,
        Screen      =  0,
        Icon        =  1,
        Textbox     =  2,
        Button      =  3,
        Movie       =  4,
        AdvTextbox  =  5,
        Checkbox    =  6,
        Progress    =  7,
        Listbox     =  8,
    }

    public class MenuData
    {
        public static readonly int Padding32 = ~0x33333333;
        public static readonly int Padding24 = (Padding32 & 0xFFFFFF);
        public static readonly int Padding16 = (Padding32 & 0xFFFF);

        public static byte ReadByte32(Stream stream)
        {
            return (byte)(stream.ReadInt32() & 0xFF);
        }

        public static void WriteByte32(Stream stream, byte value)
        {
            stream.Write(value | (Padding24 << 8));
        }

        public static bool ReadBool32(Stream stream)
        {
            var value = ReadByte32(stream);

            if (value > 1)
                throw new InvalidOperationException("No booly!");

            return (value == 1);
        }

        public static void WriteBool32(Stream stream, bool value)
        {
            WriteByte32(stream, (byte)(value ? 1 : 0));
        }

        public static void WritePadding(Stream stream, int count)
        {
            var buffer = new byte[count];

            Memory.Fill(Padding32, buffer);

            stream.Write(buffer, 0, count);
        }

        public static void WriteString(Stream stream, string value, int count, bool padded = false)
        {
            var buffer = new byte[count];

            if (padded)
                Memory.Fill(Padding32, buffer);

            var length = value.Length;

            if ((length + 1) >= count)
                length = (count - 1);

            Encoding.UTF8.GetBytes(value, 0, length, buffer, 0);

            stream.Write(buffer, 0, count);
        }

        public static List<T> ReadArray<T>(Stream stream, IMenuProvider provider)
            where T : IMenuNode, IMenuDetail, new()
        {
            var count = stream.ReadInt32();
            var values = new List<T>(count);

            var factory = provider.GetFactory();

            for (int i = 0; i < count; i++)
            {
                var value = factory.Deserialize<T>(stream);

                value.Id = i;

                values.Add(value);
            }

            return values;
        }

        public static void WriteArray<T>(Stream stream, IMenuProvider provider, List<T> values)
            where T : IMenuNode, IMenuDetail
        {
            var count = values.Count;

            if (count > 0)
            {
                stream.Write(count);

                foreach (var value in values.OrderBy((e) => e.Id))
                    provider.Factory.Serialize(stream, value);
            }
        }

        public static MenuElementBase Create(ElementType type)
        {
            switch (type)
            {
            case ElementType.Screen:        return new MenuScreen();
            case ElementType.Icon:          return new MenuIcon();
            case ElementType.Textbox:       return new MenuTextbox();
            case ElementType.Button:        return new MenuButton();
            case ElementType.Movie:         return new MenuMovie();
            case ElementType.AdvTextbox:    return new MenuAdvTextbox();
            case ElementType.Checkbox:      return new MenuCheckbox();
            case ElementType.Progress:      return new MenuProgress();
            case ElementType.Listbox:       return new MenuListbox();
            }

            return null;
        }

        public static void Read(Stream stream, IMenuElement data, IMenuProvider provider)
        {
            data.Deserialize(stream, provider);
        }

        public static void Write(Stream stream, IMenuElement data, IMenuProvider provider)
        {
            data.Serialize(stream, provider);
        }

        public static MenuElementBase Parse(Stream stream, ElementType type, IMenuProvider provider)
        {
            var element = Create(type);

            if (element != null)
                Read(stream, element, provider);

            return element;
        }

        public static MenuElementBase Create(XElement node)
        {
            var name = node.Name.LocalName;

            var type = ElementType.Screen;

            if (Enum.TryParse(name, out type))
                return Create(type);

            return null;
        }

        public static void Read(XElement node, IMenuDetailXml data, IMenuProvider provider)
        {
            data.Deserialize(node, provider);
        }

        public static void Write(XElement node, IMenuDetailXml data, IMenuProvider provider)
        {
            data.Serialize(node, provider);
        }

        public static void Write(XElement node, IMenuDetailXml data, string elemName, IMenuProvider provider)
        {
            var name = XName.Get(elemName);
            var elem = new XElement(name);

            Write(elem, data, provider);

            node.Add(elem);
        }

        public static MenuElementBase Parse(XElement node, IMenuProvider provider)
        {
            var element = Create(node);

            if (element != null)
                Read(node, element, provider);

            return element;
        }

        public static T Parse<T>(XElement node, IMenuProvider provider)
            where T : IMenuDetailXml, new()
        {
            var element = new T();

            if (element != null)
                Read(node, element, provider);

            return element;
        }

        public static List<MenuElementBase> ReadArray(XElement node, IMenuProvider provider)
        {
            var elements = new List<MenuElementBase>();

            foreach (var e in node.Elements())
            {
                var element = Parse(node, provider);

                elements.Add(element);
            }

            return elements;
        }

        public static void WriteArray(XElement node, IMenuProvider provider, List<MenuElementBase> elements)
        {
            foreach (var element in elements.OrderBy((e) => e.Id))
                Write(node, element, provider);
        }

        public static string GetAttribute(XElement node, string name, string defaultValue = "")
        {
            var result = defaultValue;

            if (node != null)
            {
                var attr = node.Attribute(name);

                if (attr != null)
                    result = attr.Value;
            }

            return result;
        }

        public static T GetAttribute<T>(XElement node, string name, Func<string, T> fnConvert, T defaultValue = default(T))
        {
            T result = defaultValue;

            if (node != null)
            {
                var attr = node.Attribute(name);

                if (attr != null)
                    result = fnConvert(attr.Value);
            }

            return result;
        }
    }

    public class MenuDataSizes : IMenuDetail
    {
        int[] sizes = new int[10];

        public int SizeOf_Unknown1;

        public int SizeOf_Screen;
        public int SizeOf_Element;
        public int SizeOf_Textbox;
        public int SizeOf_Icon;
        public int SizeOf_Button;
        public int SizeOf_Movie;

        public int SizeOf_AdvTextbox;
        public int SizeOf_Checkbox;
        public int SizeOf_Progress;
        public int SizeOf_Listbox;

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            SizeOf_Unknown1 = stream.ReadInt32();

            SizeOf_Screen = stream.ReadInt32();
            SizeOf_Element = stream.ReadInt32();
            SizeOf_Textbox = stream.ReadInt32();
            SizeOf_Icon = stream.ReadInt32();
            SizeOf_Button = stream.ReadInt32();
            SizeOf_Movie = stream.ReadInt32();

            if (provider.Version > 401)
            {
                SizeOf_AdvTextbox = stream.ReadInt32();
                SizeOf_Checkbox = stream.ReadInt32();
                SizeOf_Progress = stream.ReadInt32();
                SizeOf_Listbox = stream.ReadInt32();
            }
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            stream.Write(SizeOf_Unknown1);

            stream.Write(SizeOf_Screen);
            stream.Write(SizeOf_Element);
            stream.Write(SizeOf_Textbox);
            stream.Write(SizeOf_Icon);
            stream.Write(SizeOf_Button);
            stream.Write(SizeOf_Movie);

            if (provider.Version > 401)
            {
                stream.Write(SizeOf_AdvTextbox);
                stream.Write(SizeOf_Checkbox);
                stream.Write(SizeOf_Progress);
                stream.Write(SizeOf_Listbox);
            }
        }

        public MenuDataSizes()
        {

        }
    }

    public interface IMenuProvider : IProvider
    {
        DetailFactory<IMenuProvider> Factory { get; }

        MenuDataSizes Sizes { get; }

        int GetTypeSize(ElementType type);
        int GetTypeMinVersion(ElementType type);
    }

    public interface IMenuNode
    {
        int Id { get; set; }
    }

    public interface IMenuDetail : IDetail<IMenuProvider> { }

    public interface IMenuElement : IMenuNode, IMenuDetail
    {
        ElementType Type { get; }
    }

    public interface IMenuNodeXml
    {
        string Name { get; }
    }

    public interface IMenuDetailXml
    {
        void Serialize(XElement node, IMenuProvider provider);
        void Deserialize(XElement node, IMenuProvider provider);
    }

    public interface IMenuDataXml : IMenuNodeXml, IMenuDetailXml
    {

    }

    public abstract class MenuDataXml : IMenuDataXml
    {
        public abstract string Name { get; }

        void IMenuDetailXml.Deserialize(XElement node, IMenuProvider provider)
        {
            Deserialize(node, provider);
        }

        void IMenuDetailXml.Serialize(XElement node, IMenuProvider provider)
        {
            var name = XName.Get(Name);
            var elem = new XElement(name);

            node.Add(elem);

            Serialize(elem, provider);
        }

        protected abstract void Deserialize(XElement node, IMenuProvider provider);
        protected abstract void Serialize(XElement node, IMenuProvider provider);
    }

    public abstract class MenuNodeXml : MenuDataXml, IMenuNode
    {
        public int Id { get; set; }

        protected override void Deserialize(XElement node, IMenuProvider provider)
        {
            Id = MenuData.GetAttribute(node, "Id", int.Parse);

            Read(node, provider);
        }

        protected override void Serialize(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("Id", Id);

            Write(node, provider);
        }

        protected abstract void Read(XElement node, IMenuProvider provider);
        protected abstract void Write(XElement node, IMenuProvider provider);
    }

    public abstract class MenuElementBase : MenuNodeXml, IMenuElement
    {
        public abstract ElementType Type { get; }

        public override string Name
        {
            get { return Type.ToString(); }
        }

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            switch (Type)
            {
            case ElementType.Screen:
            case ElementType.Button:
                Deserialize(stream, provider);
                break;
            default:
                //var size = provider.GetElementSize(Type);
                //var buffer = new byte[size];
                //
                //stream.Read(buffer, 0, size);
                //
                //using (var ms = new MemoryStream(buffer))
                //    Deserialize(ms, provider);

                Deserialize(stream, provider);
                break;
            }
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            switch (Type)
            {
            case ElementType.Screen:
            case ElementType.Button:
                Serialize(stream, provider);
                break;
            default:
                //var size = provider.GetElementSize(Type);
                //
                //byte[] buffer = null;
                //
                //using (var ms = new MemoryStream(size))
                //{
                //    Serialize(ms, provider);
                //    buffer = ms.ToArray();
                //}
                //
                //if (buffer.Length != size)
                //    throw new InvalidDataException("Menu data buffer size mismatch!");
                //
                //stream.Write(buffer, 0, size);

                Serialize(stream, provider);
                break;
            }
        }

        protected abstract void Deserialize(Stream stream, IMenuProvider provider);
        protected abstract void Serialize(Stream stream, IMenuProvider provider);
    }

    public struct MenuCallback : IMenuDetail, IMenuDetailXml
    {
        public string Value;

        public bool IsDisabled
        {
            get { return String.Equals(Value, "<NoAction>", StringComparison.Ordinal); }
        }

        public bool IsEmpty
        {
            get { return String.IsNullOrEmpty(Value); }
        }

        public static implicit operator MenuCallback(string callback)
        {
            return new MenuCallback(callback);
        }

        public static implicit operator string(MenuCallback callback)
        {
            return callback.Value;
        }

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            Value = stream.ReadString(32);
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            MenuData.WriteString(stream, Value, 32, true);
        }

        void IMenuDetailXml.Deserialize(XElement node, IMenuProvider provider)
        {
            var value = node.Value;

            if (MenuData.GetAttribute(node, "Disabled", bool.Parse, false))
                value = "<NoAction>";

            Value = value;
        }

        void IMenuDetailXml.Serialize(XElement node, IMenuProvider provider)
        {
            if (IsDisabled)
            {
                node.SetAttributeValue("Disabled", bool.TrueString);
            }
            else if (!IsEmpty)
            {
                node.SetValue(Value);
            }
        }

        public MenuCallback(string value)
        {
            Value = value;
        }
    }

    public abstract class StateHolder<T> : MenuNodeXml, IMenuDetail
        where T : MenuElementBase
    {
        public override string Name => "State";

        public List<T> Elements;

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            var count = stream.ReadInt32();

            var elements = new List<T>(count);

            for (int i = 0; i < count; i++)
            {
                var type = (ElementType)stream.ReadByte();

                SanityCheck(type, provider);

                var elem = MenuData.Parse(stream, type, provider) as T;

                if (elem == null)
                    throw new InvalidOperationException($"Could not parse element type {type}");

                elem.Id = i;

                elements.Add(elem);
            }

            Elements = elements;
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            var count = Elements.Count;

            stream.Write(count);

            foreach (var elem in Elements.OrderBy((e) => e.Id))
            {
                SanityCheck(elem.Type, provider);

                stream.WriteByte((byte)elem.Type);
                MenuData.Write(stream, elem, provider);
            }
        }

        protected override void Read(XElement node, IMenuProvider provider)
        {
            var elements = new List<T>();

            foreach (var e in node.Elements())
            {
                var elem = MenuData.Parse(e, provider) as T;

                if (elem == null)
                    throw new InvalidOperationException($"Could not parse element type {e.Name}");

                SanityCheck(elem.Type, provider);

                elements.Add(elem);
            }

            Elements = elements;
        }

        protected override void Write(XElement node, IMenuProvider provider)
        {
            foreach (var element in Elements.OrderBy((e) => e.Id))
            {
                SanityCheck(element.Type, provider);

                MenuData.Write(node, element, provider);
            }
        }

        protected virtual void SanityCheck(ElementType type, IMenuProvider provider)
        {
            var minVersion = provider.GetTypeMinVersion(type);

            if (provider.Version < provider.GetTypeMinVersion(type))
                throw new InvalidOperationException($"Element type {type} not available for version {provider.Version} -- must use {minVersion} or greater!");
        }
    }

    public class ScreenState : StateHolder<MenuElement>
    {
        protected override void SanityCheck(ElementType type, IMenuProvider provider)
        {
            switch (type)
            {
            case ElementType.Effect:
            case ElementType.Screen:
                throw new InvalidOperationException($"Unexpected element type {type} in Screen state!");
            }

            base.SanityCheck(type, provider);
        }

        public ScreenState()
        {
            Elements = new List<MenuElement>();
        }
    }

    public class ButtonState : StateHolder<MenuElement>
    {
        protected override void SanityCheck(ElementType type, IMenuProvider provider)
        {
            switch (type)
            {
            case ElementType.Effect:
            case ElementType.Screen:
            case ElementType.Button:
                throw new InvalidOperationException($"Unexpected element type {type} in Button state!");
            }

            base.SanityCheck(type, provider);
        }

        public ButtonState()
        {
            Elements = new List<MenuElement>();
        }
    }

    public class MenuScreen : MenuElementBase
    {
        public override ElementType Type => ElementType.Screen;

        public string Tag;

        public int ZDepth;
        public int ZOrder;

        public bool ClearAfter;         // can't return to this screen
        public bool ClearBefore;        // clear all previous screens

        public MenuCallback OnEnter;
        public MenuCallback OnLeave;

        public byte[] ExtraData;

        public List<ScreenState> States;

        const int FIELD_TAG         = 0;
        const int FIELD_ZDEPTH      = 1;
        const int FIELD_ZORDER      = 2;
        const int FIELD_CLEARAFTER  = 3;
        const int FIELD_CLEARBEFORE = 4;
        const int FIELD_ONENTER     = 5;
        const int FIELD_ONLEAVE     = 6;
        const int FIELD_COUNT       = FIELD_ONLEAVE + 1;

        protected delegate void SerializerDelegate(Stream stream, IMenuProvider provider);

        protected void ReadZDepth(Stream stream, IMenuProvider provider)  { ZDepth = stream.ReadInt32(); }
        protected void WriteZDepth(Stream stream, IMenuProvider provider) { stream.Write(ZDepth); }

        protected void ReadZOrder(Stream stream, IMenuProvider provider)  { ZOrder = stream.ReadInt32(); }
        protected void WriteZOrder(Stream stream, IMenuProvider provider) { stream.Write(ZOrder); }

        protected void ReadClear1(Stream stream, IMenuProvider provider)  { ClearAfter = stream.ReadBool(); }
        protected void WriteClear1(Stream stream, IMenuProvider provider) { stream.WriteBool(ClearAfter); }

        protected void ReadClear2(Stream stream, IMenuProvider provider)  { ClearBefore = stream.ReadBool(); }
        protected void WriteClear2(Stream stream, IMenuProvider provider) { stream.WriteBool(ClearBefore); }

        protected void ReadTag(Stream stream, IMenuProvider provider)     { Tag = stream.ReadString(8); }
        protected void WriteTag(Stream stream, IMenuProvider provider)    { MenuData.WriteString(stream, Tag, 8); }

        protected void ReadOnEnter(Stream stream, IMenuProvider provider) { provider.Factory.Deserialize(stream, ref OnEnter); }
        protected void WriteOnEnter(Stream stream, IMenuProvider provider){ provider.Factory.Serialize(stream, OnEnter); }

        protected void ReadOnLeave(Stream stream, IMenuProvider provider) { provider.Factory.Deserialize(stream, ref OnLeave); }
        protected void WriteOnLeave(Stream stream, IMenuProvider provider){ provider.Factory.Serialize(stream, OnLeave); }

        protected SerializerDelegate SelectFunc(SerializerDelegate fnRead, SerializerDelegate fnWrite, bool writer)
        {
            return (writer) ? fnWrite : fnRead;
        }

        protected int[] GetFields(IMenuProvider provider, ref SerializerDelegate[] funcs, bool writer)
        {
            funcs = new SerializerDelegate[FIELD_COUNT];

            funcs[FIELD_TAG]            = SelectFunc(ReadTag, WriteTag, writer);
            funcs[FIELD_ZDEPTH]         = SelectFunc(ReadZDepth, WriteZDepth, writer);
            funcs[FIELD_ZORDER]         = SelectFunc(ReadZOrder, WriteZOrder, writer);
            funcs[FIELD_CLEARAFTER]     = SelectFunc(ReadClear1, WriteClear1, writer);
            funcs[FIELD_CLEARBEFORE]    = SelectFunc(ReadClear2, WriteClear2, writer);
            funcs[FIELD_ONENTER]        = SelectFunc(ReadOnEnter, WriteOnEnter, writer);
            funcs[FIELD_ONLEAVE]        = SelectFunc(ReadOnLeave, WriteOnLeave, writer);

            switch (provider.Version)
            {
            case 310:
                return new[] {
                    FIELD_ZDEPTH,
                    FIELD_ZORDER,
                    FIELD_CLEARAFTER,
                    FIELD_ONLEAVE,
                    -3,
                    FIELD_CLEARBEFORE,
                    FIELD_TAG,
                    FIELD_ONENTER,
                    -3,
                };
            case 401:
                return new[] {
                    FIELD_TAG,
                    FIELD_ZDEPTH,
                    FIELD_ZORDER,
                    FIELD_CLEARAFTER,
                    FIELD_ONENTER,
                    FIELD_ONLEAVE,
                    FIELD_CLEARBEFORE,
                    -2,
                };
            case 5160:
            case 6000:
                return new[] {
                    FIELD_TAG,
                    FIELD_ZDEPTH,
                    FIELD_ZORDER,
                    FIELD_CLEARAFTER,
                    FIELD_ONENTER,
                    FIELD_ONLEAVE,
                    -11, // TODO: what are these?
                };
            case 7001:
                return new[] {
                    FIELD_TAG,
                    FIELD_ONENTER,
                    FIELD_ONLEAVE,
                    FIELD_ZDEPTH,
                    FIELD_ZORDER,
                    FIELD_CLEARAFTER,
                    -11, // TODO: what are these?
                };
            }

            throw new NotImplementedException($"field order version {provider.Version} not implemented");
        }

        protected override void Deserialize(Stream stream, IMenuProvider provider)
        {
            SerializerDelegate[] readers = null;

            var fields = GetFields(provider, ref readers, false);

            foreach (var field in fields)
            {
                if (field < 0)
                {
                    var length = -field;

                    ExtraData = new byte[length];
                    stream.Read(ExtraData, 0, length);
                }
                else
                {
                    // read data
                    readers[field](stream, provider);
                }
            }

            States = MenuData.ReadArray<ScreenState>(stream, provider);
        }

        protected override void Serialize(Stream stream, IMenuProvider provider)
        {
            SerializerDelegate[] writers = null;

            var fields = GetFields(provider, ref writers, true);

            foreach (var field in fields)
            {
                if (field < 0)
                {
                    var length = -field;
                    
                    if (length != ExtraData.Length)
                        throw new InvalidOperationException("Field extra data length mismatch!");

                    stream.Write(ExtraData, 0, length);
                }
                else
                {
                    // write data
                    writers[field](stream, provider);
                }
            }

            MenuData.WriteArray(stream, provider, States);
        }

        protected override void Read(XElement node, IMenuProvider provider)
        {
            Tag = MenuData.GetAttribute(node, "Tag");

            ZDepth = MenuData.GetAttribute(node, "ZDepth", int.Parse);
            ZOrder = MenuData.GetAttribute(node, "ZOrder", int.Parse);

            ClearAfter = MenuData.GetAttribute(node, "ClearAfter", bool.Parse);
            ClearBefore = MenuData.GetAttribute(node, "ClearBefore", bool.Parse);

            var states = new List<ScreenState>();

            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "OnEnter":
                    OnEnter = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnLeave":
                    OnLeave = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "State":
                    var state = MenuData.Parse<ScreenState>(elem, provider);

                    states.Add(state);
                    break;
                }
            }

            States = states;
        }

        protected override void Write(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("Tag", Tag);

            node.SetAttributeValue("ZDepth", ZDepth);
            node.SetAttributeValue("ZOrder", ZDepth);
            node.SetAttributeValue("ClearAfter", ZDepth);
            node.SetAttributeValue("ClearBefore", ZDepth);

            MenuData.Write(node, OnEnter, "OnEnter", provider);
            MenuData.Write(node, OnLeave, "OnLeave", provider);

            foreach (var state in States)
                MenuData.Write(node, state, provider);
        }

        public MenuScreen()
        {
            States = new List<ScreenState>();
        }
    }

    public class MenuEffect : MenuNodeXml, IMenuElement
    {
        public ElementType Type => ElementType.Effect;

        public override string Name => "Effect";

        public int EffectType;

        public Vector2 Position;
        public Vector2 Scale;

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            EffectType = stream.ReadInt32();

            Position = stream.Read<Vector2>();
            Scale = stream.Read<Vector2>();
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            stream.Write(Type);

            stream.Write(Position);
            stream.Write(Scale);
        }

        protected override void Read(XElement node, IMenuProvider provider)
        {
            EffectType = MenuData.GetAttribute(node, "Type", int.Parse);
            Position = MenuData.GetAttribute(node, "Position", Vector2.Parse);
            Scale = MenuData.GetAttribute(node, "Scale", Vector2.Parse);
        }

        protected override void Write(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("Type", Type);
            node.SetAttributeValue("Position", Position);
            node.SetAttributeValue("Scale", Scale);
        }
    }

    public abstract class MenuElement : MenuElementBase
    {
        public Vector2 Position;
        public Vector2 Size;
        public Vector4 Color;

        public float Foobar; // ???

        public short Unk1;
        public short Unk2;
        public short Unk3;

        protected sealed override void Deserialize(Stream stream, IMenuProvider provider)
        {
            Position = stream.Read<Vector2>();
            Size = stream.Read<Vector2>();
            Color = stream.Read<Vector4>();

            if (provider.Version > 401)
            {
                Foobar = stream.ReadSingle();

                Unk1 = stream.ReadInt16();
                Unk2 = stream.ReadInt16();
                Unk3 = stream.ReadInt16();

                stream.Position += 2;
            }

            Read(stream, provider);
        }

        protected sealed override void Serialize(Stream stream, IMenuProvider provider)
        {
            stream.Write(Position);
            stream.Write(Size);
            stream.Write(Color);

            if (provider.Version > 401)
            {
                stream.Write(Foobar);

                stream.Write(Unk1);
                stream.Write(Unk2);
                stream.Write(Unk3);

                stream.Write((short)~0x3333);
            }

            Write(stream, provider);
        }

        protected override void Read(XElement node, IMenuProvider provider)
        {
            Position = MenuData.GetAttribute(node, "Position", Vector2.Parse);
            Size = MenuData.GetAttribute(node, "Size", Vector2.Parse);
            Color = MenuData.GetAttribute(node, "Color", Vector4.Parse);

            if (provider.Version > 401)
            {
                var e = node.Element("Effect");

                Foobar = MenuData.GetAttribute(e, "_Foobar", float.Parse, 0.0f);
                Unk1 = MenuData.GetAttribute(e, "_Unk1", short.Parse, (short)-1);
                Unk2 = MenuData.GetAttribute(e, "_Unk2", short.Parse, (short)-1);
                Unk3 = MenuData.GetAttribute(e, "_Unk3", short.Parse, (short)-1);
            }

            ReadData(node, provider);
        }

        protected override void Write(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("Position", Position);
            node.SetAttributeValue("Size", Size);
            node.SetAttributeValue("Color", Color);

            if (provider.Version > 401)
            {
                var e = new XElement("Effect");

                e.SetAttributeValue("_Foobar", Foobar);
                e.SetAttributeValue("_Unk1", Unk1);
                e.SetAttributeValue("_Unk2", Unk2);
                e.SetAttributeValue("_Unk3", Unk3);

                node.Add(e);
            }

            WriteData(node, provider);
        }

        protected virtual void ReadData(XElement node, IMenuProvider provider) { }
        protected virtual void WriteData(XElement node, IMenuProvider provider) { }

        protected virtual void Read(Stream stream, IMenuProvider provider) { }
        protected virtual void Write(Stream stream, IMenuProvider provider) { }
    }

    public class MenuIcon : MenuElement
    {
        public override ElementType Type => ElementType.Icon;

        public Vector2 Offset;
        public Vector2 Scale;

        public int TextureId;

        public bool HasVectors;

        protected override void Read(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > 401)
            {
                TextureId = stream.ReadInt16();
            }
            else
            {
                Offset = stream.Read<Vector2>();
                Scale = stream.Read<Vector2>();

                TextureId = stream.ReadInt32();
                HasVectors = true;
            }
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > 401)
            {
                stream.Write((short)TextureId);
            }
            else
            {
                stream.Write(Offset);
                stream.Write(Scale);
                stream.Write(TextureId);
            }
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            TextureId = MenuData.GetAttribute(node, "TextureId", int.Parse);

            if (provider.Version < 5160)
            {
                Offset = MenuData.GetAttribute(node, "Offset", Vector2.Parse);
                Scale = MenuData.GetAttribute(node, "Scale", Vector2.Parse);
            }
        }

        protected override void WriteData(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("TextureId", TextureId);

            if (provider.Version < 5160)
            {
                node.SetAttributeValue("Offset", Offset);
                node.SetAttributeValue("Scale", Scale);
            }
        }
    }

    public class MenuTextbox : MenuElement
    {
        public override ElementType Type => ElementType.Textbox;

        public int TextId;

        public Vector2 Offset;
        public Vector2 Scale;

        public MenuCallback DataSource;
        public MenuCallback TextSource;

        protected override void Read(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > 401)
            {
                if (provider.Version > 5160)
                {
                    TextId = stream.ReadInt16();
                    stream.Position += 2;
                }
                else
                {
                    TextId = (stream.ReadInt32() & 0xFFFFFF);
                }
            }
            else
            {
                TextId = stream.ReadInt32();
            }

            Offset = stream.Read<Vector2>();
            Scale = stream.Read<Vector2>();

            provider.Factory.Deserialize(stream, ref DataSource);
            provider.Factory.Deserialize(stream, ref TextSource);
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > 401)
            {
                if (provider.Version > 5160)
                {
                    stream.Write((short)TextId);
                    stream.Write((short)2);
                }
                else
                {
                    stream.Write((TextId & 0xFFFFFF) | (2 << 24));
                }
            }
            else
            {
                stream.Write(TextId);
            }

            stream.Write(Offset);
            stream.Write(Scale);

            provider.Factory.Serialize(stream, DataSource);
            provider.Factory.Serialize(stream, TextSource);
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            TextId = MenuData.GetAttribute(node, "TextId", int.Parse);

            Offset = MenuData.GetAttribute(node, "Offset", Vector2.Parse);
            Scale = MenuData.GetAttribute(node, "Scale", Vector2.Parse);

            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "DataSource":
                    DataSource = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "TextSource":
                    TextSource = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                }
            }
        }

        protected override void WriteData(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("TextId", TextId);

            node.SetAttributeValue("Offset", Offset);
            node.SetAttributeValue("Scale", Scale);

            MenuData.Write(node, DataSource, "DataSource", provider);
            MenuData.Write(node, TextSource, "TextSource", provider);
        }
    }

    public struct ButtonAction : IMenuDetail, IMenuDetailXml
    {
        public static readonly ButtonAction Empty = new ButtonAction(255, -1);

        public byte Type;

        public int Id;

        public bool IsDisabled
        {
            get { return (Type == 255) && (Id == -1); }
        }

        public string TypeName
        {
            get
            {
                switch (Type)
                {
                case 0: return "Element";
                case 1: return "Screen";
                case 2: return "DropBack";

                case 254:
                    return "UserAction";

                case 255:
                    return "None";
                }

                throw new InvalidOperationException($"Unknown navigator type {Type}");
            }
        }

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > 310)
            {
                Type = MenuData.ReadByte32(stream);
                Id = stream.ReadInt32();
            }
            else
            {
                Id = stream.ReadInt32();
                Type = (byte)((Id == -1) ? 255 : 0);
            }
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > 310)
                MenuData.WriteByte32(stream, Type);

            stream.Write(Id);
        }

        void IMenuDetailXml.Serialize(XElement node, IMenuProvider provider)
        {
            if (!IsDisabled)
            {
                node.SetAttributeValue("Type", TypeName);

                if (Id != -1)
                    node.SetAttributeValue("Id", Id);
            }
        }

        void IMenuDetailXml.Deserialize(XElement node, IMenuProvider provider)
        {
            Type = MenuData.GetAttribute(node, "Type", byte.Parse, (byte)255);
            Id = MenuData.GetAttribute(node, "Id", int.Parse, -1);
        }
        
        public ButtonAction(byte type, int id)
        {
            Type = type;
            Id = id;
        }
    }

    public class ButtonNavigators : IMenuDetail, IMenuDetailXml
    {
        public bool MouseEnabled;

        public ButtonAction MenuUp = ButtonAction.Empty;
        public ButtonAction MenuDown = ButtonAction.Empty;
        public ButtonAction MenuLeft = ButtonAction.Empty;
        public ButtonAction MenuRight = ButtonAction.Empty;
        public ButtonAction MenuSelect = ButtonAction.Empty;
        public ButtonAction MenuCancel = ButtonAction.Empty;

        public ButtonAction MenuExtra1 = ButtonAction.Empty;
        public ButtonAction MenuExtra2 = ButtonAction.Empty;

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > 310)
                MouseEnabled = MenuData.ReadBool32(stream);

            provider.Factory.Deserialize(stream, ref MenuUp);
            provider.Factory.Deserialize(stream, ref MenuDown);
            provider.Factory.Deserialize(stream, ref MenuLeft);
            provider.Factory.Deserialize(stream, ref MenuRight);
            provider.Factory.Deserialize(stream, ref MenuSelect);

            if (provider.Version > 310)
            {
                provider.Factory.Deserialize(stream, ref MenuCancel);
                provider.Factory.Deserialize(stream, ref MenuExtra1);
                provider.Factory.Deserialize(stream, ref MenuExtra2);

                // reserved memory?
                if (provider.Version > 401)
                {
                    stream.Position += 16;

                    if (provider.Version == 7001)
                        stream.Position += 16;
                }
            }
            else if (provider.Version == 310)
            {
                // assume this opens a menu
                if (MenuSelect.Type == 0)
                    MenuSelect.Type = 1;
            }
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > 310)
                MenuData.WriteBool32(stream, MouseEnabled);

            provider.Factory.Serialize(stream, MenuUp);
            provider.Factory.Serialize(stream, MenuDown);
            provider.Factory.Serialize(stream, MenuLeft);
            provider.Factory.Serialize(stream, MenuRight);
            provider.Factory.Serialize(stream, MenuSelect);

            if (provider.Version > 310)
            {
                provider.Factory.Serialize(stream, MenuCancel);
                provider.Factory.Serialize(stream, MenuExtra1);
                provider.Factory.Serialize(stream, MenuExtra2);

                if (provider.Version > 401)
                {
                    MenuData.WritePadding(stream, 16);

                    if (provider.Version == 7001)
                        MenuData.WritePadding(stream, 16);
                }
            }
        }

        void IMenuDetailXml.Deserialize(XElement node, IMenuProvider provider)
        {
            MouseEnabled = MenuData.GetAttribute(node, "FocusEnabled", bool.Parse, (provider.Version > 310) ? true : false);

            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "MenuUp":
                    MenuUp = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "MenuDown":
                    MenuDown = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "MenuLeft":
                    MenuLeft = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "MenuRight":
                    MenuRight = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "MenuSelect":
                    MenuSelect = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "MenuCancel":
                    MenuCancel = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "MenuExtra1":
                    MenuExtra1 = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "MenuExtra2":
                    MenuExtra2 = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                }
            }
        }

        void IMenuDetailXml.Serialize(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("FocusEnabled", MouseEnabled);

            SerializeAction(node, "MenuUp", MenuUp, provider);
            SerializeAction(node, "MenuDown", MenuDown, provider);
            SerializeAction(node, "MenuLeft", MenuLeft, provider);
            SerializeAction(node, "MenuRight", MenuRight, provider);
            SerializeAction(node, "MenuSelect", MenuSelect, provider);

            if (provider.Version > 310)
            {
                SerializeAction(node, "MenuCancel", MenuCancel, provider);

                SerializeAction(node, "MenuExtra1", MenuExtra1, provider);
                SerializeAction(node, "MenuExtra2", MenuExtra2, provider);
            }
        }

        void SerializeAction(XElement node, string name, ButtonAction action, IMenuProvider provider)
        {
            if (!action.IsDisabled)
                MenuData.Write(node, action, "Navigate", provider);
        }
    }

    public class ButtonCallbacks : IMenuDetail, IMenuDetailXml
    {
        public int Version;

        public MenuCallback OnMenuUp;
        public MenuCallback OnMenuDown;
        public MenuCallback OnMenuLeft;
        public MenuCallback OnMenuRight;
        public MenuCallback OnMenuSelect;

        public MenuCallback OnMenuDraw;

        public MenuCallback OnMenuScrollUp;
        public MenuCallback OnMenuScrollDown;

        public MenuCallback OnMenuExtra1;
        public MenuCallback OnMenuExtra2;

        public MenuCallback OnExtraAction1;
        public MenuCallback OnExtraAction2;

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            Version = 1;

            provider.Factory.Deserialize(stream, ref OnMenuUp);
            provider.Factory.Deserialize(stream, ref OnMenuDown);
            provider.Factory.Deserialize(stream, ref OnMenuLeft);
            provider.Factory.Deserialize(stream, ref OnMenuRight);
            provider.Factory.Deserialize(stream, ref OnMenuSelect);
            provider.Factory.Deserialize(stream, ref OnMenuDraw);

            if (provider.Version > 310)
            {
                Version = 2;

                provider.Factory.Deserialize(stream, ref OnMenuScrollUp);
                provider.Factory.Deserialize(stream, ref OnMenuScrollDown);

                if (provider.Version > 401)
                {
                    Version = 3;

                    provider.Factory.Deserialize(stream, ref OnMenuExtra1);
                    provider.Factory.Deserialize(stream, ref OnMenuExtra2);

                    if (provider.Version == 7001)
                    {
                        Version = 4;

                        provider.Factory.Deserialize(stream, ref OnExtraAction1);
                        provider.Factory.Deserialize(stream, ref OnExtraAction2);
                    }

                    stream.Position += 4;
                }
            }
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            provider.Factory.Serialize(stream, OnMenuUp);
            provider.Factory.Serialize(stream, OnMenuDown);
            provider.Factory.Serialize(stream, OnMenuLeft);
            provider.Factory.Serialize(stream, OnMenuRight);
            provider.Factory.Serialize(stream, OnMenuSelect);
            provider.Factory.Serialize(stream, OnMenuDraw);

            if (provider.Version > 310)
            {
                provider.Factory.Serialize(stream, OnMenuScrollUp);
                provider.Factory.Serialize(stream, OnMenuScrollDown);

                if (provider.Version > 401)
                {
                    provider.Factory.Serialize(stream, OnMenuExtra1);
                    provider.Factory.Serialize(stream, OnMenuExtra2);

                    if (provider.Version == 7001)
                    {
                        provider.Factory.Serialize(stream, OnExtraAction1);
                        provider.Factory.Serialize(stream, OnExtraAction2);
                    }

                    MenuData.WritePadding(stream, 4);
                }
            }
        }

        void IMenuDetailXml.Deserialize(XElement node, IMenuProvider provider)
        {
            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "OnMenuUp":
                    OnMenuUp = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnMenuDown":
                    OnMenuDown = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnMenuLeft":
                    OnMenuLeft = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnMenuRight":
                    OnMenuRight = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnMenuSelect":
                    OnMenuSelect = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnMenuDraw":
                    OnMenuDraw = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnMenuScrollUp":
                    OnMenuScrollUp = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnMenuScrollDown":
                    OnMenuScrollDown = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnMenuExtra1":
                    OnMenuExtra1 = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnMenuExtra2":
                    OnMenuExtra2 = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnExtraAction1":
                    OnExtraAction1 = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "OnExtraAction2":
                    OnExtraAction1 = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                }
            }
        }

        void IMenuDetailXml.Serialize(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("Version", Version);

            SerializeCallback(node, "OnMenuUp", OnMenuUp, provider);
            SerializeCallback(node, "OnMenuDown", OnMenuDown, provider);
            SerializeCallback(node, "OnMenuLeft", OnMenuLeft, provider);
            SerializeCallback(node, "OnMenuRight", OnMenuRight, provider);
            SerializeCallback(node, "OnMenuSelect", OnMenuSelect, provider);

            SerializeCallback(node, "OnMenuDraw", OnMenuDraw, provider);

            if (Version > 1)
            {
                SerializeCallback(node, "OnMenuScrollUp", OnMenuScrollUp, provider);
                SerializeCallback(node, "OnMenuScrollDown", OnMenuScrollDown, provider);

                if (Version > 2)
                {
                    SerializeCallback(node, "OnMenuExtra1", OnMenuExtra1, provider);
                    SerializeCallback(node, "OnMenuExtra2", OnMenuExtra2, provider);

                    if (Version > 3)
                    {
                        SerializeCallback(node, "OnExtraAction1", OnExtraAction1, provider);
                        SerializeCallback(node, "OnExtraAction2", OnExtraAction2, provider);
                    }
                }
            }
        }


        void SerializeCallback(XElement node, string name, MenuCallback callback, IMenuProvider provider)
        {
            if (!callback.IsEmpty)
                MenuData.Write(node, callback, name, provider);
        }
    }

    public class MenuButton : MenuElement
    {
        public override ElementType Type => ElementType.Button;

        public List<ButtonState> States;

        public ButtonNavigators Navigators;
        public ButtonCallbacks Callbacks;
        
        protected override void Read(Stream stream, IMenuProvider provider)
        {
            States = MenuData.ReadArray<ButtonState>(stream, provider);

            provider.Factory.Deserialize(stream, ref Navigators);
            provider.Factory.Deserialize(stream, ref Callbacks);
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            MenuData.WriteArray(stream, provider, States);

            provider.Factory.Serialize(stream, Navigators);
            provider.Factory.Serialize(stream, Callbacks);
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "State":
                    var state = MenuData.Parse<ButtonState>(elem, provider);

                    States.Add(state);

                    break;
                case "Navigation":
                    Navigators = MenuData.Parse<ButtonNavigators>(elem, provider);
                    break;
                case "Callbacks":
                    Callbacks = MenuData.Parse<ButtonCallbacks>(elem, provider);
                    break;
                }
            }
        }

        protected override void WriteData(XElement node, IMenuProvider provider)
        {
            foreach (var state in States)
                MenuData.Write(node, state, "State", provider);

            MenuData.Write(node, Navigators, "Navigation", provider);
            MenuData.Write(node, Callbacks, "Callbacks", provider);
        }

        public MenuButton()
        {
            States = new List<ButtonState>();

            Navigators = new ButtonNavigators();
            Callbacks = new ButtonCallbacks();
        }
    }

    public class MenuMovie : MenuElement
    {
        public override ElementType Type => ElementType.Movie;
    }

    public class MenuAdvTextbox : MenuElement
    {
        public override ElementType Type => ElementType.AdvTextbox;

        public int Foo1;
        public int Foo2;
        public int Foo3;

        public MenuCallback Format;

        public int Foo4;

        protected override void Read(Stream stream, IMenuProvider provider)
        {
            Foo1 = stream.ReadByte();
            Foo2 = stream.ReadByte();

            Foo3 = stream.ReadInt16();

            provider.Factory.Deserialize(stream, ref Format);

            Foo4 = stream.ReadInt16();
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            stream.WriteByte(Foo1);
            stream.WriteByte(Foo2);

            stream.Write((short)Foo3);

            provider.Factory.Serialize(stream, Format);

            stream.Write((short)Foo4);
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            Foo1 = MenuData.GetAttribute(node, "_Foo1", byte.Parse);
            Foo2 = MenuData.GetAttribute(node, "_Foo2", byte.Parse);
            Foo3 = MenuData.GetAttribute(node, "_Foo3", short.Parse);
            
            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "Format":
                    Format = MenuData.Parse<MenuCallback>(node, provider);
                    break;
                }
            }

            Foo4 = MenuData.GetAttribute(node, "_Foo4", short.Parse);
        }

        protected override void WriteData(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("_Foo1", Foo1);
            node.SetAttributeValue("_Foo2", Foo2);
            node.SetAttributeValue("_Foo3", Foo3);

            MenuData.Write(node, Format, "Format", provider);

            node.SetAttributeValue("_Foo4", Foo4);
        }
    }

    public class MenuCheckbox : MenuElement
    {
        public override ElementType Type => ElementType.Checkbox;

        protected override void Read(Stream stream, IMenuProvider provider)
        {
            throw new NotImplementedException();
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            throw new NotImplementedException();
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            throw new NotImplementedException();
        }

        protected override void WriteData(XElement node, IMenuProvider provider)
        {
            throw new NotImplementedException();
        }
    }

    public class MenuProgress : MenuElement
    {
        public override ElementType Type => ElementType.Progress;

        public float Foo1;
        public float Foo2;
        public float Foo3;
        public float Foo4;

        public short Foo5;
        public short Foo6;

        public MenuCallback Format;

        protected override void Read(Stream stream, IMenuProvider provider)
        {
            Foo1 = stream.ReadSingle();
            Foo2 = stream.ReadSingle();
            Foo3 = stream.ReadSingle();
            Foo4 = stream.ReadSingle();

            Foo5 = stream.ReadInt16();
            Foo6 = stream.ReadInt16();

            provider.Factory.Deserialize(stream, ref Format);
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            stream.Write(Foo1);
            stream.Write(Foo2);
            stream.Write(Foo3);
            stream.Write(Foo4);

            stream.Write(Foo5);
            stream.Write(Foo6);

            provider.Factory.Serialize(stream, Format);
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            Foo1 = MenuData.GetAttribute(node, "_Foo1", float.Parse);
            Foo2 = MenuData.GetAttribute(node, "_Foo2", float.Parse);
            Foo3 = MenuData.GetAttribute(node, "_Foo3", float.Parse);
            Foo4 = MenuData.GetAttribute(node, "_Foo4", float.Parse);

            Foo5 = MenuData.GetAttribute(node, "_Foo5", short.Parse);
            Foo6 = MenuData.GetAttribute(node, "_Foo6", short.Parse);

            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "Format":
                    Format = MenuData.Parse<MenuCallback>(node, provider);
                    break;
                }
            }
        }

        protected override void WriteData(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("_Foo1", Foo1);
            node.SetAttributeValue("_Foo2", Foo2);
            node.SetAttributeValue("_Foo3", Foo3);
            node.SetAttributeValue("_Foo4", Foo4);

            node.SetAttributeValue("_Foo5", Foo5);
            node.SetAttributeValue("_Foo6", Foo6);

            MenuData.Write(node, Format, "Format", provider);
        }
    }

    public class MenuListbox : MenuElement
    {
        public override ElementType Type => ElementType.Listbox;

        public int TextId;

        public short[] Values;

        public Vector2 Offset;

        public float Scale;

        public MenuCallback DataSource;
        public MenuCallback TextSource;

        protected override void Read(Stream stream, IMenuProvider provider)
        {
            for (int i = 0; i < 11; i++)
                Values[i] = stream.ReadInt16();

            TextId = stream.ReadInt16();
            stream.Position += 4;

            Offset = stream.Read<Vector2>();
            Scale = stream.ReadSingle();

            provider.Factory.Deserialize(stream, ref DataSource);
            provider.Factory.Deserialize(stream, ref TextSource);
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            for (int i = 0; i < 11; i++)
                stream.Write(Values[i]);

            stream.Write(TextId);
            stream.Write((short)2);

            MenuData.WritePadding(stream, 2);

            stream.Write(Offset);
            stream.Write(Scale);

            provider.Factory.Serialize(stream, DataSource);
            provider.Factory.Serialize(stream, TextSource);
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            TextId = MenuData.GetAttribute(node, "TextId", int.Parse);
            Offset = MenuData.GetAttribute(node, "Offset", Vector2.Parse);
            Scale = MenuData.GetAttribute(node, "Scale", float.Parse);

            var choices = new short[11];

            var last = -1;
            var idx = 0;

            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "DataSource":
                    DataSource = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "TextSource":
                    TextSource = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                case "Choice":
                    var choice = MenuData.GetAttribute(elem, "TextId", short.Parse);

                    if (choice < 0)
                        throw new InvalidOperationException($"Bad choice text id {choice}; must be greater than zero");

                    if (choice == last)
                        continue;

                    choices[idx++] = choice;
                    last = choice;
                    break;
                }
            }

            if (idx > 0)
            {
                for (int i = idx; i < 11; i++)
                    choices[i] = (short)last;
            }
        }

        protected override void WriteData(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("TextId", TextId);

            node.SetAttributeValue("Offset", Offset);
            node.SetAttributeValue("Scale", Scale);

            MenuData.Write(node, DataSource, "DataSource", provider);
            MenuData.Write(node, TextSource, "TextSource", provider);

            var last = -1;

            foreach (var value in Values)
            {
                if (value == last)
                    break;

                var e = new XElement("Choice");
                e.SetAttributeValue("TextId", value);

                node.Add(e);

                last = value;
            }
        }

        public MenuListbox()
        {
            Values = new short[11];
        }
    }

    public class MenuGui : IDetail, IMenuProvider, IMenuDetail, IMenuDetailXml
    {
        DetailFactory<IMenuProvider> _factory;

        DetailFactory<IMenuProvider> IMenuProvider.Factory
        {
            get
            {
                if (_factory == null)
                    _factory = new DetailFactory<IMenuProvider>(this);

                return _factory;
            }
        }

        public PlatformType Platform { get; set; }

        public int Version { get; set; }
        public int Flags { get; set; }

        public int UID { get; set; }

        public MenuDataSizes Sizes { get; set; }

        public List<MenuEffect> Effects { get; set; }
        public List<MenuScreen> Screens { get; set; }

        int IMenuProvider.GetTypeSize(ElementType type)
        {
            switch (type)
            {
            case ElementType.Effect:        return 0x14;
            case ElementType.Screen:        return Sizes.SizeOf_Screen;
            case ElementType.Icon:          return Sizes.SizeOf_Element + Sizes.SizeOf_Icon;
            case ElementType.Textbox:       return Sizes.SizeOf_Element + Sizes.SizeOf_Textbox;
            case ElementType.Button:        return Sizes.SizeOf_Element + Sizes.SizeOf_Button;
            case ElementType.Movie:         return Sizes.SizeOf_Movie;
            case ElementType.AdvTextbox:    return Sizes.SizeOf_Element + Sizes.SizeOf_AdvTextbox;
            case ElementType.Checkbox:      return Sizes.SizeOf_Element + Sizes.SizeOf_Checkbox;
            case ElementType.Progress:      return Sizes.SizeOf_Progress + Sizes.SizeOf_Progress;
            case ElementType.Listbox:       return Sizes.SizeOf_Listbox + Sizes.SizeOf_Listbox;
            }

            throw new InvalidOperationException($"Unknown element size for type {type}");
        }

        int IMenuProvider.GetTypeMinVersion(ElementType type)
        {
            switch (type)
            {
            case ElementType.Effect:
            case ElementType.AdvTextbox:
            case ElementType.Checkbox:
            case ElementType.Progress:
            case ElementType.Listbox:
                return 5160;
            }

            return 310;
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            var me = (this as IMenuProvider);

            Deserialize(stream, me);
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            var me = (this as IMenuProvider);

            Serialize(stream, me);
        }

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            Deserialize(stream, provider);
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            Serialize(stream, provider);
        }

        void IMenuDetailXml.Deserialize(XElement node, IMenuProvider provider)
        {
            Deserialize(node, provider);
        }

        void IMenuDetailXml.Serialize(XElement node, IMenuProvider provider)
        {
            Serialize(node, provider);
        }

        protected void Deserialize(Stream stream, IMenuProvider provider)
        {
            Version = stream.ReadInt16();

            var nScreens = 0;
            var nUnks = 0;

            var factory = provider.GetFactory();

            // Known versions:
            //  310 - Driv3r: PS2, XBox
            //  401 - Driv3r: PC
            //  5160 - Driver Parallel Lines: PS2, XBox
            //  6000 - Driver Parallel Lines: Wii
            //  7001 - Driver Parallel Lines: PC
            if (Version > 401)
            {
                nScreens = stream.ReadByte();
                nUnks = stream.ReadByte();

                UID = stream.ReadInt16();

                var numEffects = stream.ReadInt16();

                if (Version == 7001)
                    stream.Position += 8;

                var effects = new List<MenuEffect>(numEffects);

                for (int i = 0; i < numEffects; i++)
                {
                    var effect = factory.Deserialize<MenuEffect>(stream);

                    effect.Id = i;

                    effects.Add(effect);
                }

                Effects = effects;
            }
            else
            {
                // highly unlikely, but better safe than sorry
                if (stream.ReadInt16() != 0)
                    throw new InvalidOperationException($"Menu version {Version} has unexpected data!");

                UID = -1;

                nScreens = stream.ReadInt32();
            }

            Sizes = factory.Deserialize<MenuDataSizes>(stream);

            var screens = new List<MenuScreen>(nScreens);

            for (int i = 0; i < nScreens; i++)
            {
                var screen = factory.Deserialize<MenuScreen>(stream);

                screen.Id = i;

                screens.Add(screen);
            }

            Screens = screens;
        }

        protected void Serialize(Stream stream, IMenuProvider provider)
        {

        }

        protected void Deserialize(XElement node, IMenuProvider provider)
        {
            Version = MenuData.GetAttribute(node, "Version", int.Parse);
            UID = MenuData.GetAttribute(node, "UID", int.Parse);

            switch (Version)
            {
            case 310:
                Sizes = new MenuDataSizes() {
                    SizeOf_Unknown1     = 0x0C,
                    SizeOf_Screen       = 0x58,
                    SizeOf_Element      = 0x20,
                    SizeOf_Textbox      = 0x54,
                    SizeOf_Icon         = 0x14,
                    SizeOf_Button       = 0xD4,
                    SizeOf_Movie        = 0x20,
                };
                break;
            case 401:
                Sizes = new MenuDataSizes() {
                    SizeOf_Unknown1     = 0x0C,
                    SizeOf_Screen       = 0x54,
                    SizeOf_Element      = 0x20,
                    SizeOf_Textbox      = 0x54,
                    SizeOf_Icon         = 0x14,
                    SizeOf_Button       = 0x144,
                    SizeOf_Movie        = 0x20,
                };
                break;
            case 5160:
            case 6000:
                Sizes = new MenuDataSizes()
                {
                    SizeOf_Unknown1     = 0x08,
                    SizeOf_Screen       = 0x5C,
                    SizeOf_Element      = 0x2C,
                    SizeOf_Textbox      = 0x54,
                    SizeOf_Icon         = 0x02,
                    SizeOf_Button       = 0x198,
                    SizeOf_Movie        = 0x2C,
                    SizeOf_AdvTextbox   = 0x26,
                    SizeOf_Checkbox     = 0x28,
                    SizeOf_Progress     = 0x34,
                    SizeOf_Listbox      = 0x68,
                };
                break;
            case 7001:
                Sizes = new MenuDataSizes()
                {
                    SizeOf_Unknown1     = 0x10,
                    SizeOf_Screen       = 0x5C,
                    SizeOf_Element      = 0x2C,
                    SizeOf_Textbox      = 0x54,
                    SizeOf_Icon         = 0x02,
                    SizeOf_Button       = 0x1E8,
                    SizeOf_Movie        = 0x2C,
                    SizeOf_AdvTextbox   = 0x26,
                    SizeOf_Checkbox     = 0x28,
                    SizeOf_Progress     = 0x34,
                    SizeOf_Listbox      = 0x68,
                };
                break;
            default:
                throw new InvalidOperationException($"Unknown menu gui version {Version}");
            }

            // self-provider? ;)
            if (provider == null)
                provider = this;

            if (provider.Version > 401)
            {
                var effects = new List<MenuEffect>();

                foreach (var elem in node.Elements("Effect"))
                {
                    var fx = MenuData.Parse<MenuEffect>(node, provider);

                    effects.Add(fx);
                }

                Effects = effects.OrderBy((e) => e.Id).ToList();
            }

            var screens = new List<MenuScreen>();

            foreach (var elem in node.Elements("Screen"))
            {
                var screen = MenuData.Parse<MenuScreen>(node, provider);

                screens.Add(screen);
            }

            Screens = screens.OrderBy((s) => s.Id).ToList();
        }

        protected void Serialize(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("Version", Version);
            node.SetAttributeValue("UID", UID);

            if (Version > 401)
            {
                foreach (var fx in Effects)
                    MenuData.Write(node, fx, this);
            }

            foreach (var screen in Screens)
                MenuData.Write(node, screen, this);
        }

        public MenuGui()
        {
            Sizes = new MenuDataSizes();

            Effects = new List<MenuEffect>();
            Screens = new List<MenuScreen>();
        }

        public MenuGui(XElement node)
            : this()
        {
            Deserialize(node, null);
        }
    }

    public class MenuPackage : SpoolableResource<SpoolableBuffer>, IDetailProvider, IXmlDetail
    {
        public PlatformType Platform { get; set; }

        public int Version { get; set; }
        public int Flags { get; set; }

        public MenuGui Data { get; set; }

        protected override void Load()
        {
            using (var stream = Spooler.GetMemoryStream())
            {
                Data = this.Deserialize<MenuGui>(stream);
            }
        }

        protected override void Save()
        {
            byte[] buffer = null;

            using (var stream = new MemoryStream())
            {
                this.Serialize(stream, Data);

                buffer = stream.ToArray();
            }

            Spooler.SetBuffer(buffer);
        }

        void IXmlDetail.Deserialize(XElement node, IDetailProvider provider)
        {
            Deserialize(node, provider);
        }

        void IXmlDetail.Serialize(XElement node, IDetailProvider provider)
        {
            Serialize(node, provider);
        }

        protected void Deserialize(XElement node, IDetailProvider provider)
        {
            Version = MenuData.GetAttribute(node, "Version", int.Parse, 3);

            var platform = PlatformType.Generic;

            Enum.TryParse(MenuData.GetAttribute(node, "Platform", "Generic"), out platform);

            Platform = platform;

            var gui = node.Element("MenuGui");

            if (gui != null)
                Data = new MenuGui(gui);
        }

        protected void Serialize(XElement node, IDetailProvider provider)
        {
            node.SetAttributeValue("Version", Version);
            node.SetAttributeValue("Platform", Platform);

            MenuData.Write(node, Data, "MenuGui", Data);
        }

        public void WriteTo(string filename)
        {
            var doc = new XDocument();

            var root = new XElement("MenuPackage");
            root.SetAttributeValue("Version", Version);

            doc.Add(root);

            Serialize(root, this);

            doc.Save(filename);
        }
    }

    public class MenuPackageFile : FileChunker, IModelFile, IDetailProvider
    {
        public PlatformType Platform { get; set; }

        public int Version { get; set; }
        public int Flags { get; set; }

        public MenuPackage MenuData { get; set; }
        public ModelPackage MaterialData { get; set; }

        List<ModelPackage> IModelFile.Packages
        {
            get
            {
                var packages = new List<ModelPackage>();

                if (MaterialData != null)
                    packages.Add(MaterialData);

                return packages;
            }
        }

        bool IModelFile.HasModels => (MaterialData != null);

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            switch ((ChunkType)sender.Context)
            {
            case ChunkType.ReflectionsMenuDataChunk:
                MenuData = sender.AsResource<MenuPackage>();
                MenuData.Version = sender.Version;
                break;

            case ChunkType.ModelPackagePC:
            case ChunkType.ModelPackagePC_X:
            case ChunkType.ModelPackagePS2:
            case ChunkType.ModelPackageXbox:
            case ChunkType.ModelPackageWii:
                Platform = ModelPackage.GetPlatformType(sender.Context);
                MaterialData = sender.AsResource<ModelPackage>();
                break;

            case ChunkType.ReflectionsMenuDataPackage:
                if (MenuData == null)
                    throw new InvalidOperationException("Menu data chunk not processed!");

                // setup version
                Version = sender.Version;

                if (Version != 3)
                    throw new InvalidOperationException($"Unsupported menu data package version: {Version}");

                // setup menu data platform
                MenuData.Platform = Platform;

                if (MaterialData != null)
                {
                    // load the material package
                    PackageManager.Load(MaterialData);
                }

                // load the menu data
                SpoolableResourceFactory.Load(MenuData);
                break;
            }
        }

        public override void Dispose()
        {
            // unregister the material package
            if (MaterialData != null)
                PackageManager.UnRegister(MaterialData);

            base.Dispose();
        }

        public MenuPackageFile() { }
        public MenuPackageFile(string filename) : base(filename) { }
    }
}
