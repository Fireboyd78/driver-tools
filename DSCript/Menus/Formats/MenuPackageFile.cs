using DSCript.Models;
using DSCript.Spooling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;
using System.Runtime.InteropServices;
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
        Progress    =  5,
        ListBox     =  6,
        CheckBox    =  7,
        AdvTextbox  =  8,
    }

    // Known versions:
    //  3.1.0 - Driv3r: PS2, XBox
    //  4.0.1 - Driv3r: PC
    //  5.1.6.0 - Driver Parallel Lines: PS2, XBox
    //  6.0.0.0 - Driver Parallel Lines: Wii
    //  7.0.0.1 - Driver Parallel Lines: PC
    public struct MenuVersion
    {
        public const int Driver3_Console    = 310;      // Driv3r: PS2, XBox
        public const int Driver3_PC         = 401;      // Driv3r: PC
        public const int Driver4_Console    = 5160;     // Driver Parallel Lines: PS2, XBox
        public const int Driver4_Wii        = 6000;     // Driver Parallel Lines: Wii
        public const int Driver4_PC         = 7001;     // Driver Parallel Lines: PC
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

            if (length > 0)
            {
                if ((length + 1) >= count)
                    length = (count - 1);

                Encoding.UTF8.GetBytes(value, 0, length, buffer, 0);
            }

            // IMPORTANT: null-terminator!
            buffer[length] = 0;

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
            case ElementType.Progress:      return new MenuProgress();
            case ElementType.ListBox:       return new MenuListBox();
            case ElementType.CheckBox:      return new MenuCheckBox();
            case ElementType.AdvTextbox:    return new MenuAdvTextbox();
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
                element.Deserialize(node, provider);

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

        public int SizeOf_Project;

        public int SizeOf_Screen;
        public int SizeOf_Element;
        public int SizeOf_Textbox;
        public int SizeOf_Icon;
        public int SizeOf_Button;
        public int SizeOf_Movie;

        public int SizeOf_Progress;
        public int SizeOf_ListBox;
        public int SizeOf_CheckBox;
        public int SizeOf_AdvTextbox;

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            SizeOf_Project = stream.ReadInt32();

            SizeOf_Screen = stream.ReadInt32();
            SizeOf_Element = stream.ReadInt32();
            SizeOf_Textbox = stream.ReadInt32();
            SizeOf_Icon = stream.ReadInt32();
            SizeOf_Button = stream.ReadInt32();
            SizeOf_Movie = stream.ReadInt32();

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                SizeOf_Progress = stream.ReadInt32();
                SizeOf_ListBox = stream.ReadInt32();
                SizeOf_CheckBox = stream.ReadInt32();
                SizeOf_AdvTextbox = stream.ReadInt32();
            }
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            stream.Write(SizeOf_Project);

            stream.Write(SizeOf_Screen);
            stream.Write(SizeOf_Element);
            stream.Write(SizeOf_Textbox);
            stream.Write(SizeOf_Icon);
            stream.Write(SizeOf_Button);
            stream.Write(SizeOf_Movie);

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                stream.Write(SizeOf_Progress);
                stream.Write(SizeOf_ListBox);
                stream.Write(SizeOf_CheckBox);
                stream.Write(SizeOf_AdvTextbox);
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
            MenuData.WriteString(stream, Value ?? String.Empty, 32, true);
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

        public int BackgroundTexture;
        public int DefaultButtonId;

        public bool KeepOffStack;           // can't return to this screen
        public bool ClearStack;             // clear all previous screens

        public bool DisableDropBack;
        public bool NoDropBack2;

        public bool RotateItems;

        public int RotateStepsX;
        public int RotateStepsY;
        public int RotateMode;

        public int ActivateEffectsFlag;

        public short MaterialChunkIndex;

        public MenuCallback OnEnter;
        public MenuCallback OnLeave;

        //public byte[] ExtraData;

        public List<ScreenState> States;

        const int FIELD_TAG             = 0;
        const int FIELD_BGTEXTURE       = 1;
        const int FIELD_DEFAULTBTN      = 2;
        const int FIELD_KEEPOFFSTACK    = 3;
        const int FIELD_CLEARSTACK      = 4;
        const int FIELD_ONENTER         = 5;
        const int FIELD_ONLEAVE         = 6;
        const int FIELD_DROPBACK1       = 7;
        const int FIELD_COUNT           = FIELD_DROPBACK1 + 1;
        const int FIELD_PADDING         = FIELD_COUNT + 1;
        const int FIELD_ENDOFLIST       = FIELD_PADDING + 1;

        protected delegate void SerializerDelegate(Stream stream, IMenuProvider provider);

        protected void ReadBackgroundTexture(Stream stream, IMenuProvider provider)  { BackgroundTexture = stream.ReadInt32(); }
        protected void WriteBackgroundTexture(Stream stream, IMenuProvider provider) { stream.Write(BackgroundTexture); }

        protected void ReadDefaultButtonId(Stream stream, IMenuProvider provider)  { DefaultButtonId = stream.ReadInt32(); }
        protected void WriteDefaultButtonId(Stream stream, IMenuProvider provider) { stream.Write(DefaultButtonId); }

        protected void ReadClear1(Stream stream, IMenuProvider provider)  { KeepOffStack = stream.ReadBool(); }
        protected void WriteClear1(Stream stream, IMenuProvider provider) { stream.WriteBool(KeepOffStack); }

        protected void ReadClear2(Stream stream, IMenuProvider provider)  { ClearStack = stream.ReadBool(); }
        protected void WriteClear2(Stream stream, IMenuProvider provider) { stream.WriteBool(ClearStack); }

        protected void ReadNoDropBack1(Stream stream, IMenuProvider provider) { DisableDropBack = stream.ReadBool(); }
        protected void WriteNoDropBack1(Stream stream, IMenuProvider provider) { stream.WriteBool(DisableDropBack); }

        protected void ReadNoDropBack2(Stream stream, IMenuProvider provider) { NoDropBack2 = stream.ReadBool(); }
        protected void WriteNoDropBack2(Stream stream, IMenuProvider provider) { stream.WriteBool(NoDropBack2); }

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
            funcs[FIELD_BGTEXTURE]      = SelectFunc(ReadBackgroundTexture, WriteBackgroundTexture, writer);
            funcs[FIELD_DEFAULTBTN]     = SelectFunc(ReadDefaultButtonId, WriteDefaultButtonId, writer);
            funcs[FIELD_KEEPOFFSTACK]   = SelectFunc(ReadClear1, WriteClear1, writer);
            funcs[FIELD_CLEARSTACK]     = SelectFunc(ReadClear2, WriteClear2, writer);
            funcs[FIELD_ONENTER]        = SelectFunc(ReadOnEnter, WriteOnEnter, writer);
            funcs[FIELD_ONLEAVE]        = SelectFunc(ReadOnLeave, WriteOnLeave, writer);
            funcs[FIELD_DROPBACK1]      = SelectFunc(ReadNoDropBack1, WriteNoDropBack1, writer);

            switch (provider.Version)
            {
            case MenuVersion.Driver3_Console:
                return new[] {
                    FIELD_BGTEXTURE,
                    FIELD_DEFAULTBTN,
                    FIELD_KEEPOFFSTACK,
                    FIELD_ONLEAVE,
                    FIELD_PADDING, 3,
                    FIELD_CLEARSTACK,
                    FIELD_TAG,
                    FIELD_ONENTER,
                    FIELD_PADDING, 3,

                    FIELD_ENDOFLIST, 0,
                };
            case MenuVersion.Driver3_PC:
                return new[] {
                    FIELD_TAG,
                    FIELD_BGTEXTURE,
                    FIELD_DEFAULTBTN,
                    FIELD_KEEPOFFSTACK,
                    FIELD_ONENTER,
                    FIELD_ONLEAVE,
                    FIELD_CLEARSTACK,
                    FIELD_PADDING, 2,

                    FIELD_ENDOFLIST, 0,
                };
            case MenuVersion.Driver4_Console:
            case MenuVersion.Driver4_Wii:
                return new[] {
                    FIELD_TAG,
                    FIELD_BGTEXTURE,
                    FIELD_DEFAULTBTN,
                    FIELD_KEEPOFFSTACK,
                    FIELD_ONENTER,
                    FIELD_ONLEAVE,
                    FIELD_CLEARSTACK,
                    FIELD_DROPBACK1,

                    FIELD_ENDOFLIST, 9,
                };
            case MenuVersion.Driver4_PC:
                return new[] {
                    FIELD_TAG,
                    FIELD_ONENTER,
                    FIELD_ONLEAVE,
                    FIELD_BGTEXTURE,
                    FIELD_DEFAULTBTN,
                    FIELD_KEEPOFFSTACK,
                    FIELD_CLEARSTACK,
                    FIELD_DROPBACK1,

                    FIELD_ENDOFLIST, 9,
                };
            }

            throw new NotImplementedException($"field order version {provider.Version} not implemented");
        }

        protected override void Deserialize(Stream stream, IMenuProvider provider)
        {
            SerializerDelegate[] readers = null;

            var fields = GetFields(provider, ref readers, false);

            var idx = 0;

            while (idx < fields.Length)
            {
                var field = fields[idx++];

                if (field == FIELD_PADDING)
                {
                    stream.Position += fields[idx++];
                    continue;
                }
                else if (field == FIELD_ENDOFLIST)
                {
                    if (fields[idx++] != 0)
                    {
                        //
                        // !!! deserialize the rest of the data !!!
                        //
                        if (provider.Version >= MenuVersion.Driver4_Console)
                        {
                            if (provider.Version == MenuVersion.Driver4_PC)
                                NoDropBack2 = stream.ReadBool();

                            RotateItems = stream.ReadBool();

                            RotateStepsX = stream.ReadByte();
                            RotateStepsY = stream.ReadByte();

                            RotateMode = stream.ReadByte();

                            ActivateEffectsFlag = stream.ReadByte();

                            if (provider.Version == MenuVersion.Driver4_PC)
                            {
                                // juuuuuuuuuust in case
                                var pad = stream.ReadByte();

                                if (pad != 0)
                                    DSC.Log($"D4 menu GUI unknown pad byte {pad:X2}({pad})");
                            }

                            MaterialChunkIndex = stream.ReadInt16();

                            // /definitely/ padding, safely skip
                            if (provider.Version != MenuVersion.Driver4_PC)
                                stream.Position += 2;
                        }
                    }

                    break;
                }
                //if (field < 0)
                //{
                //    var length = -field;
                //
                //    ExtraData = new byte[length];
                //    stream.Read(ExtraData, 0, length);
                //
                //    System.Diagnostics.Debug.WriteLine($"{Tag,-8} extra data - {String.Join(", ", ExtraData.Select((b) => b.ToString("X2")))}");
                //}
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

            var idx = 0;

            while (idx < fields.Length)
            {
                var field = fields[idx++];

                if (field == FIELD_PADDING)
                {
                    MenuData.WritePadding(stream, fields[idx++]);
                    continue;
                }
                else if (field == FIELD_ENDOFLIST)
                {
                    if (fields[idx++] != 0)
                    {
                        //
                        // !!! serialize the rest of the data !!!
                        //
                        if (provider.Version >= MenuVersion.Driver4_Console)
                        {
                            if (provider.Version == MenuVersion.Driver4_PC)
                                stream.WriteBool(NoDropBack2);

                            stream.WriteBool(RotateItems);

                            stream.WriteByte(RotateStepsX);
                            stream.WriteByte(RotateStepsY);

                            stream.WriteByte(RotateMode);

                            stream.WriteByte(ActivateEffectsFlag);

                            if (provider.Version == MenuVersion.Driver4_PC)
                                stream.WriteByte(0);

                            stream.Write(MaterialChunkIndex);
                            
                            if (provider.Version != MenuVersion.Driver4_PC)
                                MenuData.WritePadding(stream, 2);
                        }
                    }

                    break;
                }
                //if (field < 0)
                //{
                //    var length = -field;
                //    
                //    if (length != ExtraData.Length)
                //        throw new InvalidOperationException("Field extra data length mismatch!");
                //
                //    stream.Write(ExtraData, 0, length);
                //}
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

            BackgroundTexture = MenuData.GetAttribute(node, "BackgroundTexture", int.Parse);
            DefaultButtonId = MenuData.GetAttribute(node, "DefaultButtonId", int.Parse);

            KeepOffStack = MenuData.GetAttribute(node, "KeepOffStack", bool.Parse);
            ClearStack = MenuData.GetAttribute(node, "ClearStack", bool.Parse);

            if (provider.Version >= MenuVersion.Driver4_Console)
            {
                DisableDropBack = MenuData.GetAttribute(node, "DisableDropBack", bool.Parse);
                NoDropBack2 = MenuData.GetAttribute(node, "_NoDropBack2", bool.Parse);

                RotateItems = MenuData.GetAttribute(node, "RotateItems", bool.Parse);
                RotateStepsX = MenuData.GetAttribute(node, "RotateStepsX", int.Parse);
                RotateStepsY = MenuData.GetAttribute(node, "RotateStepsY", int.Parse);
                RotateMode = MenuData.GetAttribute(node, "RotateMode", int.Parse);

                ActivateEffectsFlag = MenuData.GetAttribute(node, "ActivateEffectsFlag", int.Parse);

                MaterialChunkIndex = MenuData.GetAttribute(node, "MaterialChunkIndex", short.Parse, (short)-1);
            }

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

            node.SetAttributeValue("BackgroundTexture", BackgroundTexture);
            node.SetAttributeValue("DefaultButtonId", DefaultButtonId);
            node.SetAttributeValue("KeepOffStack", KeepOffStack);
            node.SetAttributeValue("ClearStack", ClearStack);

            if (provider.Version >= MenuVersion.Driver4_Console)
            {
                node.SetAttributeValue("DisableDropBack", DisableDropBack);

                if (provider.Version == MenuVersion.Driver4_PC)
                    node.SetAttributeValue("_NoDropBack2", NoDropBack2);

                node.SetAttributeValue("RotateItems", RotateItems);
                node.SetAttributeValue("RotateStepsX", RotateStepsX);
                node.SetAttributeValue("RotateStepsY", RotateStepsY);
                node.SetAttributeValue("RotateMode", RotateMode);

                node.SetAttributeValue("ActivateEffectsFlag", ActivateEffectsFlag);

                if (MaterialChunkIndex != -1)
                    node.SetAttributeValue("MaterialChunkIndex", MaterialChunkIndex);
            }

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

#if WORKS_FINE
    public class EffectData<T> : IMenuDetail, IMenuDetailXml
        where T : struct
    {
        Type m_Type;

        T Start;
        T End;

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            Start = stream.Read<T>();
            End = stream.Read<T>();
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            stream.Write(Start);
            stream.Write(End);
        }

        void IMenuDetailXml.Deserialize(XElement node, IMenuProvider provider)
        {
            Start = MenuData.GetAttribute(node, "Start", Parse);
            End = MenuData.GetAttribute(node, "End", Parse);
        }

        void IMenuDetailXml.Serialize(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("Start", Start);
            node.SetAttributeValue("End", End);
        }

        T ConvertType(object value)
        {
            return (T)Convert.ChangeType(value, m_Type);
        }

        delegate TParseType ParseDelegate<TParseType>(string value);
        delegate bool TryParseDelegate<TParseType>(string value, out TParseType result);

        T Parse<TParseType>(string value, ParseDelegate<TParseType> fnParse)
        {
            TParseType result = fnParse(value);

            return (T)Convert.ChangeType(result, m_Type);
        }

        bool TryParseObject(string value, ref T result)
        {
            if (m_Type == typeof(Vector2))
            {
                result = Parse(value, Vector2.Parse);
                return true;
            }
            else if (m_Type == typeof(Vector3))
            {
                result = Parse(value, Vector3.Parse);
                return true;
            }
            else if (m_Type == typeof(Vector4))
            {
                result = Parse(value, Vector4.Parse);
                return true;
            }

            return false;
        }

        bool TryParse<TParseType>(string value, TryParseDelegate<TParseType> fnTryParse, ref T result)
        {
            TParseType val = default(TParseType);

            if (fnTryParse(value, out val))
            {
                result = (T)Convert.ChangeType(val, m_Type);
                return true;
            }

            return false;
        }

        bool TryParse(string value, ref T result)
        {
            var typecode = Type.GetTypeCode(m_Type);

            switch (typecode)
            {
            case TypeCode.Empty:
            case TypeCode.DBNull:
                throw new InvalidCastException();

            case TypeCode.Boolean:  return TryParse<bool>(value, bool.TryParse, ref result);
            case TypeCode.Char:     return TryParse<char>(value, char.TryParse, ref result);
            case TypeCode.SByte:    return TryParse<sbyte>(value, sbyte.TryParse, ref result);
            case TypeCode.Byte:     return TryParse<byte>(value, byte.TryParse, ref result);
            case TypeCode.Int16:    return TryParse<short>(value, short.TryParse, ref result);
            case TypeCode.UInt16:   return TryParse<ushort>(value, ushort.TryParse, ref result);
            case TypeCode.Int32:    return TryParse<int>(value, int.TryParse, ref result);
            case TypeCode.UInt32:   return TryParse<uint>(value, uint.TryParse, ref result);
            case TypeCode.Int64:    return TryParse<long>(value, long.TryParse, ref result);
            case TypeCode.UInt64:   return TryParse<ulong>(value, ulong.TryParse, ref result);
            case TypeCode.Single:   return TryParse<float>(value, float.TryParse, ref result);
            case TypeCode.Double:   return TryParse<double>(value, double.TryParse, ref result);
            case TypeCode.Decimal:  return TryParse<decimal>(value, decimal.TryParse, ref result);
            case TypeCode.DateTime: return TryParse<DateTime>(value, DateTime.TryParse, ref result);

            case TypeCode.String:
                throw new InvalidOperationException("String-to-string conversion!?");

            case TypeCode.Object:   return TryParseObject(value, ref result);
            }

            return false;
        }

        T Parse(string value)
        {
            T result = default(T);

            if (TryParse(value, ref result))
                return result;

            throw new InvalidOperationException("Can't parse type");
        }

        public EffectData()
        {
            m_Type = typeof(T);
        }
    }
#endif

    public class MenuEffect : MenuNodeXml, IMenuElement
    {
        public ElementType Type => ElementType.Effect;

        public override string Name => "Effect";

#if !WORKS_FINE
        public int EffectTypeA;
        public int EffectTypeB;

        public Vector2 Time;
        public Vector2 Data;

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            EffectTypeA = stream.ReadUInt16();
            EffectTypeB = stream.ReadUInt16();

            Time = stream.Read<Vector2>();
            Data = stream.Read<Vector2>();
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            stream.Write((ushort)EffectTypeA);
            stream.Write((ushort)EffectTypeB);

            stream.Write(Time);
            stream.Write(Data);
        }

        protected override void Read(XElement node, IMenuProvider provider)
        {
            EffectTypeA = MenuData.GetAttribute(node, "TypeA", int.Parse);
            EffectTypeB = MenuData.GetAttribute(node, "TypeB", int.Parse);

            Time = MenuData.GetAttribute(node, "Time", Vector2.Parse);
            Data = MenuData.GetAttribute(node, "Data", Vector2.Parse);
        }

        protected override void Write(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("TypeA", EffectTypeA);
            node.SetAttributeValue("TypeB", EffectTypeB);

            node.SetAttributeValue("Time", Time);
            node.SetAttributeValue("Data", Data);
        }
#else
        public EffectData<ushort> EffectType;
        public EffectData<float> Time;
        public EffectData<float> Data;

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            provider.Factory.Deserialize(stream, ref EffectType);
            provider.Factory.Deserialize(stream, ref Time);
            provider.Factory.Deserialize(stream, ref Data);
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            provider.Factory.Serialize(stream, ref EffectType);
            provider.Factory.Serialize(stream, ref Time);
            provider.Factory.Serialize(stream, ref Data);
        }

        protected override void Read(XElement node, IMenuProvider provider)
        {
            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "Type":
                    MenuData.Read(elem, EffectType, provider);
                    break;
                case "Time":
                    MenuData.Read(elem, Time, provider);
                    break;
                case "Data":
                    MenuData.Read(elem, Data, provider);
                    break;
                }
            }
        }

        protected override void Write(XElement node, IMenuProvider provider)
        {
            MenuData.Write(node, EffectType, "Type", provider);
            MenuData.Write(node, Time, "Time", provider);
            MenuData.Write(node, Data, "Data", provider);
        }

        public MenuEffect()
        {
            EffectType = new EffectData<ushort>();
            Time = new EffectData<float>();
            Data = new EffectData<float>();
        }
#endif
    }

    public abstract class MenuElement : MenuElementBase
    {
        public Vector2 Position;
        public Vector2 Size;
        public Vector4 Color;

        public float Scale; // ???

        public short OnActivate;
        public short OnStepping;
        public short OnDeactivate;

        protected sealed override void Deserialize(Stream stream, IMenuProvider provider)
        {
            Position = stream.Read<Vector2>();
            Size = stream.Read<Vector2>();
            Color = stream.Read<Vector4>();

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                Scale = stream.ReadSingle();

                OnActivate = stream.ReadInt16();
                OnStepping = stream.ReadInt16();
                OnDeactivate = stream.ReadInt16();

                stream.Position += 2;
            }

            Read(stream, provider);
        }

        protected sealed override void Serialize(Stream stream, IMenuProvider provider)
        {
            stream.Write(Position);
            stream.Write(Size);
            stream.Write(Color);

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                stream.Write(Scale);

                stream.Write(OnActivate);
                stream.Write(OnStepping);
                stream.Write(OnDeactivate);

                stream.Write((short)~0x3333);
            }

            Write(stream, provider);
        }

        protected override void Read(XElement node, IMenuProvider provider)
        {
            Position = MenuData.GetAttribute(node, "Position", Vector2.Parse);
            Size = MenuData.GetAttribute(node, "Size", Vector2.Parse);
            Color = MenuData.GetAttribute(node, "Color", Vector4.Parse);

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                Scale = MenuData.GetAttribute(node, "Scale", float.Parse, 0.0f);
                OnActivate = MenuData.GetAttribute(node, "OnActivate", short.Parse, (short)-1);
                OnStepping = MenuData.GetAttribute(node, "OnStepping", short.Parse, (short)-1);
                OnDeactivate = MenuData.GetAttribute(node, "OnDeactivate", short.Parse, (short)-1);
            }

            ReadData(node, provider);
        }

        protected override void Write(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("Position", Position);
            node.SetAttributeValue("Size", Size);
            node.SetAttributeValue("Color", Color);

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                node.SetAttributeValue("Scale", Scale);

                if (OnActivate != -1)
                    node.SetAttributeValue("OnActivate", OnActivate);
                if (OnStepping != -1)
                    node.SetAttributeValue("OnStepping", OnStepping);
                if (OnDeactivate != -1)
                    node.SetAttributeValue("OnDeactivate", OnDeactivate);
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

        public int TextureId;

        public Vector2 IconOffset;
        public Vector2 IconScale;

        public bool HasVectors;

        protected override void Read(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > MenuVersion.Driver3_PC)
            {
                TextureId = stream.ReadInt16();
            }
            else
            {
                IconOffset = stream.Read<Vector2>();
                IconScale = stream.Read<Vector2>();

                TextureId = stream.ReadInt32();
                HasVectors = true;
            }
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > MenuVersion.Driver3_PC)
            {
                stream.Write((short)TextureId);
            }
            else
            {
                stream.Write(IconOffset);
                stream.Write(IconScale);
                stream.Write(TextureId);
            }
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            TextureId = MenuData.GetAttribute(node, "TextureId", int.Parse);

            if (provider.Version < MenuVersion.Driver4_Console)
            {
                IconOffset = MenuData.GetAttribute(node, "IconOffset", Vector2.Parse);
                IconScale = MenuData.GetAttribute(node, "IconScale", Vector2.Parse);
            }
        }

        protected override void WriteData(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("TextureId", TextureId);

            if (provider.Version < MenuVersion.Driver4_Console)
            {
                node.SetAttributeValue("IconOffset", IconOffset);
                node.SetAttributeValue("IconScale", IconScale);
            }
        }
    }

    public class MenuTextbox : MenuElement
    {
        public override ElementType Type => ElementType.Textbox;

        public TextSettings Settings;

        public Vector2 TextOffset;  // > Driver3_PC
        public Vector2 TextScale;   // <= Driver3_PC

        public float Spacing;

        public short ScrollType;
        public short ScrollSpeed;

        public MenuCallback DataSource;
        public MenuCallback TextSource;

        protected override void Read(Stream stream, IMenuProvider provider)
        {
            provider.Factory.Deserialize(stream, ref Settings);

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                TextOffset = stream.Read<Vector2>();
                Spacing = stream.ReadSingle();

                ScrollType = stream.ReadInt16();
                ScrollSpeed = stream.ReadInt16();
            }
            else
            {
                Spacing = stream.ReadSingle();

                // sneaky sneaky
                Settings.Justify = stream.ReadInt32();

                TextScale = stream.Read<Vector2>();
            }

            provider.Factory.Deserialize(stream, ref DataSource);
            provider.Factory.Deserialize(stream, ref TextSource);
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            provider.Factory.Serialize(stream, ref Settings);

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                stream.Write(TextOffset);
                stream.Write(Spacing);

                stream.Write(ScrollType);
                stream.Write(ScrollSpeed);
            }
            else
            {
                stream.Write(Spacing);

                stream.Write(Settings.Justify);

                stream.Write(TextScale);
            }

            provider.Factory.Serialize(stream, DataSource);
            provider.Factory.Serialize(stream, TextSource);
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            Settings = MenuData.Parse<TextSettings>(node, provider);

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                TextOffset = MenuData.GetAttribute(node, "TextOffset", Vector2.Parse);
                Spacing = MenuData.GetAttribute(node, "Spacing", float.Parse);
                ScrollType = MenuData.GetAttribute(node, "ScrollType", short.Parse);
                ScrollSpeed = MenuData.GetAttribute(node, "ScrollSpeed", short.Parse);
            }
            else
            {
                Spacing = MenuData.GetAttribute(node, "Spacing", float.Parse);
                TextScale = MenuData.GetAttribute(node, "TextScale", Vector2.Parse);
            }

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
            MenuData.Write(node, Settings, provider);

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                node.SetAttributeValue("TextOffset", TextOffset);
                node.SetAttributeValue("Spacing", Spacing);
                node.SetAttributeValue("ScrollType", ScrollType);
                node.SetAttributeValue("ScrollSpeed", ScrollSpeed);
            }
            else
            {
                // unused, but let's include it for debugging purposes
                if (Spacing != 0)
                    node.SetAttributeValue("Spacing", Spacing);

                node.SetAttributeValue("TextScale", TextScale);
            }

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
            if (provider.Version > MenuVersion.Driver3_Console)
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
            if (provider.Version > MenuVersion.Driver3_Console)
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

        byte GetTypeFromName(string typename)
        {
            if (!String.IsNullOrEmpty(typename))
            {
                switch (typename)
                {
                case "Element":     return 0;
                case "Screen":      return 1;
                case "DropBack":    return 2;
                case "UserAction":  return 254;
                case "None":        return 255;
                default:            throw new InvalidOperationException($"Unknown navigator type '{typename}'");
                }
            }

            // default to none
            return 255;
        }

        void IMenuDetailXml.Deserialize(XElement node, IMenuProvider provider)
        {
            var typename = MenuData.GetAttribute(node, "Type");

            Type = GetTypeFromName(typename);
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
        public bool MouseAllowed;

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
            if (provider.Version > MenuVersion.Driver3_Console)
                MouseAllowed = MenuData.ReadBool32(stream);

            provider.Factory.Deserialize(stream, ref MenuUp);
            provider.Factory.Deserialize(stream, ref MenuDown);
            provider.Factory.Deserialize(stream, ref MenuLeft);
            provider.Factory.Deserialize(stream, ref MenuRight);
            provider.Factory.Deserialize(stream, ref MenuSelect);

            if (provider.Version > MenuVersion.Driver3_Console)
            {
                provider.Factory.Deserialize(stream, ref MenuCancel);
                provider.Factory.Deserialize(stream, ref MenuExtra1);
                provider.Factory.Deserialize(stream, ref MenuExtra2);

                // reserved memory?
                if (provider.Version > MenuVersion.Driver3_PC)
                {
                    stream.Position += 16;

                    if (provider.Version == MenuVersion.Driver4_PC)
                        stream.Position += 16;
                }
            }
            else if (provider.Version == MenuVersion.Driver3_Console)
            {
                // assume this opens a menu
                if (MenuSelect.Type == 0)
                    MenuSelect.Type = 1;
            }
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > MenuVersion.Driver3_Console)
                MenuData.WriteBool32(stream, MouseAllowed);

            provider.Factory.Serialize(stream, MenuUp);
            provider.Factory.Serialize(stream, MenuDown);
            provider.Factory.Serialize(stream, MenuLeft);
            provider.Factory.Serialize(stream, MenuRight);
            provider.Factory.Serialize(stream, MenuSelect);

            if (provider.Version > MenuVersion.Driver3_Console)
            {
                provider.Factory.Serialize(stream, MenuCancel);
                provider.Factory.Serialize(stream, MenuExtra1);
                provider.Factory.Serialize(stream, MenuExtra2);

                if (provider.Version > MenuVersion.Driver3_PC)
                {
                    MenuData.WritePadding(stream, 16);

                    if (provider.Version == MenuVersion.Driver4_PC)
                        MenuData.WritePadding(stream, 16);
                }
            }
        }

        void IMenuDetailXml.Deserialize(XElement node, IMenuProvider provider)
        {
            MouseAllowed = MenuData.GetAttribute(node, "MouseAllowed", bool.Parse, (provider.Version > MenuVersion.Driver3_Console) ? true : false);

            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "Navigate_MenuUp":
                    MenuUp = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "Navigate_MenuDown":
                    MenuDown = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "Navigate_MenuLeft":
                    MenuLeft = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "Navigate_MenuRight":
                    MenuRight = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "Navigate_MenuSelect":
                    MenuSelect = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "Navigate_MenuCancel":
                    MenuCancel = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "Navigate_MenuExtra1":
                    MenuExtra1 = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                case "Navigate_MenuExtra2":
                    MenuExtra2 = MenuData.Parse<ButtonAction>(elem, provider);
                    break;
                }
            }
        }

        void IMenuDetailXml.Serialize(XElement node, IMenuProvider provider)
        {
            if (provider.Version > MenuVersion.Driver3_Console)
                node.SetAttributeValue("MouseAllowed", MouseAllowed);

            SerializeAction(node, "MenuUp", MenuUp, provider);
            SerializeAction(node, "MenuDown", MenuDown, provider);
            SerializeAction(node, "MenuLeft", MenuLeft, provider);
            SerializeAction(node, "MenuRight", MenuRight, provider);
            SerializeAction(node, "MenuSelect", MenuSelect, provider);

            if (provider.Version > MenuVersion.Driver3_Console)
            {
                SerializeAction(node, "MenuCancel", MenuCancel, provider);

                SerializeAction(node, "MenuExtra1", MenuExtra1, provider);
                SerializeAction(node, "MenuExtra2", MenuExtra2, provider);
            }
        }

        void SerializeAction(XElement node, string name, ButtonAction action, IMenuProvider provider)
        {
            if (!action.IsDisabled)
                MenuData.Write(node, action, $"Navigate_{name}", provider);
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

            if (provider.Version > MenuVersion.Driver3_Console)
            {
                Version = 2;

                provider.Factory.Deserialize(stream, ref OnMenuScrollUp);
                provider.Factory.Deserialize(stream, ref OnMenuScrollDown);

                if (provider.Version > MenuVersion.Driver3_PC)
                {
                    Version = 3;

                    provider.Factory.Deserialize(stream, ref OnMenuExtra1);
                    provider.Factory.Deserialize(stream, ref OnMenuExtra2);

                    if (provider.Version == MenuVersion.Driver4_PC)
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

            if (provider.Version > MenuVersion.Driver3_Console)
            {
                provider.Factory.Serialize(stream, OnMenuScrollUp);
                provider.Factory.Serialize(stream, OnMenuScrollDown);

                if (provider.Version > MenuVersion.Driver3_PC)
                {
                    provider.Factory.Serialize(stream, OnMenuExtra1);
                    provider.Factory.Serialize(stream, OnMenuExtra2);

                    if (provider.Version == MenuVersion.Driver4_PC)
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
            Version = MenuData.GetAttribute(node, "Version", int.Parse);

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
                MenuData.Write(node, state, provider);

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

    public class MenuProgress : MenuElement
    {
        public override ElementType Type => ElementType.Progress;

        public int Direction;
        public int Parts;
        public int Gap;

        public int BackAlpha;

        public MenuCallback Format;

        public int Texture;

        protected override void Read(Stream stream, IMenuProvider provider)
        {
            Direction = stream.ReadByte();
            Parts = stream.ReadByte();
            Gap = stream.ReadByte();
            BackAlpha = stream.ReadByte();

            provider.Factory.Deserialize(stream, ref Format);

            Texture = stream.ReadInt16();
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            stream.WriteByte(Direction);
            stream.WriteByte(Parts);
            stream.WriteByte(Gap);
            stream.WriteByte(BackAlpha);

            provider.Factory.Serialize(stream, Format);

            stream.Write((short)Texture);
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            Direction = MenuData.GetAttribute(node, "Direction", byte.Parse);
            Parts = MenuData.GetAttribute(node, "Parts", byte.Parse);
            Gap = MenuData.GetAttribute(node, "Gap", byte.Parse);
            BackAlpha = MenuData.GetAttribute(node, "BackAlpha", byte.Parse);

            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "Format":
                    Format = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                }
            }

            Texture = MenuData.GetAttribute(node, "Texture", short.Parse);
        }

        protected override void WriteData(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("Direction", Direction);
            node.SetAttributeValue("Parts", Parts);
            node.SetAttributeValue("Gap", Gap);
            node.SetAttributeValue("BackAlpha", BackAlpha);

            MenuData.Write(node, Format, "Format", provider);

            node.SetAttributeValue("Texture", Texture);
        }
    }
#if _CPP
    struct SMenuListboxExpData
    /*
    	size: 0x30
    */
    {
    	/* 0x00 */MAv4 v4SelColour;
    	/* 0x10 */char strName[8];
    	/* 0x18 */uint8 nNumToDisplay;
    	/* 0x19 */uint8 nFontIndex;
    	/* 0x1A */uint8 nJustify;
    	/* 0x1B */uint8 pad;
    	/* 0x1C */MAreal fXScale;
    	/* 0x20 */MAreal fYScale;
    	/* 0x24 */MAreal fSpacing;
    	public: SMenuListboxExpData & operator=(const SMenuListboxExpData &);
    	public: SMenuListboxExpData * SMenuListboxExpData(const SMenuListboxExpData &);
    	public: SMenuListboxExpData * SMenuListboxExpData(void);
    };
#endif
    public class MenuListBox : MenuElement
    {
        public override ElementType Type => ElementType.ListBox;

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

    public class MenuCheckBox : MenuElement
    {
        public override ElementType Type => ElementType.CheckBox;

        public Vector4 Color2;
        
        public short Texture1;
        public short Texture2;

        public MenuCallback Format;

        protected override void Read(Stream stream, IMenuProvider provider)
        {
            Color2 = stream.Read<Vector4>();

            Texture1 = stream.ReadInt16();
            Texture2 = stream.ReadInt16();

            provider.Factory.Deserialize(stream, ref Format);
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            stream.Write(Color2);

            stream.Write(Texture1);
            stream.Write(Texture2);

            provider.Factory.Serialize(stream, Format);
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            Color2 = MenuData.GetAttribute(node, "Color2", Vector4.Parse);

            Texture1 = MenuData.GetAttribute(node, "Texture1", short.Parse);
            Texture2 = MenuData.GetAttribute(node, "Texture2", short.Parse);

            foreach (var elem in node.Elements())
            {
                var name = elem.Name;

                switch (name.LocalName)
                {
                case "Format":
                    Format = MenuData.Parse<MenuCallback>(elem, provider);
                    break;
                }
            }
        }

        protected override void WriteData(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("Color2", Color2);

            node.SetAttributeValue("Texture1", Texture1);
            node.SetAttributeValue("Texture2", Texture2);

            MenuData.Write(node, Format, "Format", provider);
        }
    }

    public struct TextJustify
    {
        public const int Left   = 0;
        public const int Right  = 1;
        public const int Center = 2;

        public static string ToString(int value)
        {
            switch (value)
            {
            case 0: return "Left";
            case 1: return "Right";
            case 2: return "Center";
            default:
                throw new InvalidDataException($"Unknown justify value '{value}'");
            }
        }

        public static int Parse(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                switch (value)
                {
                case "Left":    return Left;
                case "Right":   return Right;
                case "Center":  return Center;
                default:
                    try
                    {
                        return int.Parse(value);
                    }
                    catch (Exception) {
                        throw new InvalidDataException($"Unknown justify type '{value}'");
                    }
                }
            }

            return Left;
        }
    }

    public struct TextSettings : IMenuDetail, IMenuDetailXml
    {
        public int TextId;
        public int Font;
        public int Justify;

        void IDetail<IMenuProvider>.Deserialize(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > MenuVersion.Driver3_PC)
            {
                if (provider.Version > MenuVersion.Driver4_Console)
                {
                    TextId = stream.ReadInt16();
                    Font = stream.ReadByte();
                    Justify = stream.ReadByte();
                }
                else
                {
                    var data = stream.ReadInt32();
                    
                    // TODO: verify
                    TextId = data & 0xFFFFFF;
                    Font = (data >> 24) & 0xFF;
                    Justify = -1;
                }
            }
            else
            {
                TextId = stream.ReadInt32();
                Justify = -1;
            }
        }

        void IDetail<IMenuProvider>.Serialize(Stream stream, IMenuProvider provider)
        {
            if (provider.Version > MenuVersion.Driver3_PC)
            {
                if (provider.Version > MenuVersion.Driver4_Console)
                {
                    stream.Write((short)TextId);
                    stream.WriteByte(Font);
                    stream.WriteByte(Justify);
                }
                else
                {
                    var data = TextId | ((Font & 0xFF) << 24);

                    stream.Write(data);
                }
            }
            else
            {
                stream.Write(TextId);
            }
        }

        void IMenuDetailXml.Serialize(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("TextId", TextId);

            if (Justify != -1)
                node.SetAttributeValue("Justify", TextJustify.ToString(Justify));

            if (provider.Version > MenuVersion.Driver3_PC)
                node.SetAttributeValue("Font", Font);
        }

        void IMenuDetailXml.Deserialize(XElement node, IMenuProvider provider)
        {
            TextId = MenuData.GetAttribute(node, "TextId", int.Parse);

            Justify = MenuData.GetAttribute(node, "Justify", TextJustify.Parse, -1);

            if (provider.Version > MenuVersion.Driver3_PC)
                Font = MenuData.GetAttribute(node, "Font", int.Parse);
        }
    }

    public class MenuAdvTextbox : MenuElement
    {
        public override ElementType Type => ElementType.AdvTextbox;

        public TextSettings Settings;

        public short[] Values;

        public Vector2 TextScale;

        public float Spacing;

        public MenuCallback DataSource;
        public MenuCallback TextSource;

        protected override void Read(Stream stream, IMenuProvider provider)
        {
            for (int i = 0; i < 11; i++)
                Values[i] = stream.ReadInt16();

            provider.Factory.Deserialize(stream, ref Settings);
            stream.Position += 2;

            TextScale = stream.Read<Vector2>();
            Spacing = stream.ReadSingle();

            provider.Factory.Deserialize(stream, ref DataSource);
            provider.Factory.Deserialize(stream, ref TextSource);
        }

        protected override void Write(Stream stream, IMenuProvider provider)
        {
            for (int i = 0; i < 11; i++)
                stream.Write(Values[i]);

            provider.Factory.Serialize(stream, Settings);
            MenuData.WritePadding(stream, 2);

            stream.Write(TextScale);
            stream.Write(Spacing);

            provider.Factory.Serialize(stream, DataSource);
            provider.Factory.Serialize(stream, TextSource);
        }

        protected override void ReadData(XElement node, IMenuProvider provider)
        {
            Settings = MenuData.Parse<TextSettings>(node, provider);

            TextScale = MenuData.GetAttribute(node, "TextScale", Vector2.Parse);
            Spacing = MenuData.GetAttribute(node, "Spacing", float.Parse);

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
            MenuData.Write(node, Settings, provider);

            node.SetAttributeValue("TextScale", TextScale);
            node.SetAttributeValue("Spacing", Spacing);

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

        public MenuAdvTextbox()
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

        public int DefaultScreen { get; set; }

        public bool Spooled { get; set; }

        public short Width { get; set; }
        public short Height { get; set; }

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
            case ElementType.Progress:      return Sizes.SizeOf_Element + Sizes.SizeOf_Progress;
            case ElementType.ListBox:       return Sizes.SizeOf_Element + Sizes.SizeOf_ListBox;
            case ElementType.CheckBox:      return Sizes.SizeOf_Element + Sizes.SizeOf_CheckBox;
            case ElementType.AdvTextbox:    return Sizes.SizeOf_Element + Sizes.SizeOf_AdvTextbox;
            }

            throw new InvalidOperationException($"Unknown element size for type {type}");
        }

        int IMenuProvider.GetTypeMinVersion(ElementType type)
        {
            switch (type)
            {
            case ElementType.Effect:
            case ElementType.Progress:
            case ElementType.ListBox:
            case ElementType.CheckBox:
            case ElementType.AdvTextbox:
                return MenuVersion.Driver4_Console;
            }

            return MenuVersion.Driver3_Console;
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
            // menu project data
            Version = stream.ReadInt16();

            var nScreens = 0;

            var factory = provider.GetFactory();

            if (Version > MenuVersion.Driver3_PC)
            {
                nScreens = stream.ReadByte();

                DefaultScreen = stream.ReadByte();
                Spooled = stream.ReadInt16() == 1;

                var numEffects = stream.ReadInt16();

                if (Version == MenuVersion.Driver4_PC)
                {
                    Width = stream.ReadInt16();
                    Height = stream.ReadInt16();

                    // what is this?
                    stream.Position += 4;
                }

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

                Spooled = false;

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
            stream.Write((short)Version);

            var nScreens = Screens.Count;

            if (Version > MenuVersion.Driver3_PC)
            {
                var nEffects = Effects.Count;

                stream.WriteByte(nScreens);
                stream.WriteByte(DefaultScreen);

                stream.Write((short)(Spooled ? 1 : 0));
                stream.Write((short)nEffects);

                if (Version == MenuVersion.Driver4_PC)
                {
                    stream.Write(Width);
                    stream.Write(Height);

                    MenuData.WritePadding(stream, 4);
                }

                //
                // Effects
                //
                foreach (var effect in Effects)
                    provider.Factory.Serialize(stream, effect);
            }
            else
            {
                stream.Write((short)0);
                stream.Write(nScreens);
            }

            //
            // Data sizes
            //
            provider.Factory.Serialize(stream, Sizes);

            //
            // Screens
            //
            foreach (var screen in Screens)
                provider.Factory.Serialize(stream, screen);
        }

        protected void Deserialize(XElement node, IMenuProvider provider)
        {
            Version = MenuData.GetAttribute(node, "Version", int.Parse);

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                Spooled = MenuData.GetAttribute(node, "Spooled", bool.Parse);
                DefaultScreen = MenuData.GetAttribute(node, "DefaultScreen", int.Parse);
            }

            switch (Version)
            {
            case MenuVersion.Driver3_Console:
                Sizes = new MenuDataSizes() {
                    SizeOf_Project      = 0x0C,
                    SizeOf_Screen       = 0x58,
                    SizeOf_Element      = 0x20,
                    SizeOf_Textbox      = 0x54,
                    SizeOf_Icon         = 0x14,
                    SizeOf_Button       = 0xD4,
                    SizeOf_Movie        = 0x20,
                };
                break;
            case MenuVersion.Driver3_PC:
                Sizes = new MenuDataSizes() {
                    SizeOf_Project      = 0x0C,
                    SizeOf_Screen       = 0x54,
                    SizeOf_Element      = 0x20,
                    SizeOf_Textbox      = 0x54,
                    SizeOf_Icon         = 0x14,
                    SizeOf_Button       = 0x144,
                    SizeOf_Movie        = 0x20,
                };
                break;
            case MenuVersion.Driver4_Console:
            case MenuVersion.Driver4_Wii:
                Sizes = new MenuDataSizes()
                {
                    SizeOf_Project      = 0x08,
                    SizeOf_Screen       = 0x5C,
                    SizeOf_Element      = 0x2C,
                    SizeOf_Textbox      = 0x54,
                    SizeOf_Icon         = 0x02,
                    SizeOf_Button       = 0x198,
                    SizeOf_Movie        = 0x2C,
                    SizeOf_Progress     = 0x26,
                    SizeOf_ListBox      = 0x28,
                    SizeOf_CheckBox     = 0x34,
                    SizeOf_AdvTextbox   = 0x68,
                };
                break;
            case MenuVersion.Driver4_PC:
                Sizes = new MenuDataSizes()
                {
                    SizeOf_Project      = 0x10,
                    SizeOf_Screen       = 0x5C,
                    SizeOf_Element      = 0x2C,
                    SizeOf_Textbox      = 0x54,
                    SizeOf_Icon         = 0x02,
                    SizeOf_Button       = 0x1E8,
                    SizeOf_Movie        = 0x2C,
                    SizeOf_Progress     = 0x26,
                    SizeOf_ListBox      = 0x28,
                    SizeOf_CheckBox     = 0x34,
                    SizeOf_AdvTextbox   = 0x68,
                };

                Width = MenuData.GetAttribute(node, "Width", short.Parse, (short)1280);
                Height = MenuData.GetAttribute(node, "Height", short.Parse, (short)720);
                break;
            default:
                throw new InvalidOperationException($"Unknown menu gui version {Version}");
            }

            // self-provider? ;)
            if (provider == null)
                provider = this;

            if (provider.Version > MenuVersion.Driver3_PC)
            {
                var effects = new List<MenuEffect>();

                foreach (var elem in node.Elements("Effect"))
                {
                    var fx = MenuData.Parse<MenuEffect>(elem, provider);

                    effects.Add(fx);
                }

                Effects = effects.OrderBy((e) => e.Id).ToList();
            }

            var screens = new List<MenuScreen>();

            foreach (var elem in node.Elements("Screen"))
            {
                var screen = MenuData.Parse<MenuScreen>(elem, provider);

                screens.Add(screen);
            }

            Screens = screens.OrderBy((s) => s.Id).ToList();
        }

        protected void Serialize(XElement node, IMenuProvider provider)
        {
            node.SetAttributeValue("Version", Version);

            if (Version > MenuVersion.Driver3_PC)
            {
                node.SetAttributeValue("Spooled", Spooled);
                node.SetAttributeValue("DefaultScreen", DefaultScreen);

                if (Version == MenuVersion.Driver4_PC)
                {
                    node.SetAttributeValue("Width", Width);
                    node.SetAttributeValue("Height", Height);
                }

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

        public void LoadXml(string filename)
        {
            var doc = XDocument.Load(filename);
            var root = doc.Root;

            Deserialize(root, null);
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

            base.OnSpoolerLoaded(sender, e);
        }

        protected override void OnFileSaveBegin()
        {
            MenuData.CommitChanges();

            base.OnFileSaveBegin();
        }

        protected override void OnFileLoadEnd()
        {
            var package = MaterialData;

            if (MaterialData != null)
                package.DisplayName = $"{package.UID:X8} : {package.Handle:X4}";

            base.OnFileLoadEnd();
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
