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

using Zartex.Converters;

namespace Zartex
{
    public class NodeDefinition
    {
        public byte TypeId { get; set; }

        public short StringId { get; set; }

        public NodeColor Color { get; set; }

        public short Reserved { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public short Flags { get; set; }

        public List<NodeProperty> Properties { get; set; }

        public static T Create<T>(Stream stream)
            where T : NodeDefinition, new()
        {
            var typeId = (byte)(stream.ReadInt16() & 0xFF);
            var strId = stream.ReadInt16();

            var definition = new T() {
                TypeId = typeId,
                StringId = strId
            };

            definition.LoadData(stream);

            return definition;
        }
        
        public virtual void LoadData(Stream stream)
        {
            Color = new NodeColor() {
                R = (byte)stream.ReadByte(),
                G = (byte)stream.ReadByte(),
                B = (byte)stream.ReadByte(),
                A = (byte)stream.ReadByte()
            };

            Reserved = stream.ReadInt16();
            Flags = stream.ReadInt16();
        }

        public virtual void SaveData(Stream stream)
        {
            stream.WriteByte(Color.R);
            stream.WriteByte(Color.G);
            stream.WriteByte(Color.B);
            stream.WriteByte(Color.A);

            stream.Write(Reserved);
            stream.Write(Flags);
        }

        public virtual void WriteTo(Stream stream)
        {
            stream.WriteByte(TypeId);
            stream.WriteByte(0x3E);
            stream.Write(StringId);

            SaveData(stream);
        }
    }
}
