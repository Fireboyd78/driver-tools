using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

using Zartex.Converters;

namespace Zartex.MissionObjects
{
    public class BlockType_0x2 : IMissionObject
    {
        private ushort _type = 0;

        public int ID
        {
            get { return 0x2; }
        }

        public int Size
        {
            get
            {
                if (_type == 0) throw new Exception("Cannot retrieve size of uninitalized block");

                return (_type != 0x14) ? 0x38 : 0x30;
            }
        }

        public ushort Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public ushort Reserved { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public uint Flags1 { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<Double> Floats1 { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<Double> Floats2 { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public uint GUID { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public uint Flags2 { get; set; }

        public BlockType_0x2(BinaryReader reader)
        {
            long baseOffset = reader.BaseStream.Position;

            Type = reader.ReadUInt16();

            Reserved = reader.ReadUInt16();

            Flags1 = reader.ReadUInt32();

            if (Type == 0x14)
            {
                reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);
                Floats1 = new List<Double>(2);
            }
            else if (Type == 0x1C)
            {
                Floats1 = new List<Double>(4);
            }
            else throw new Exception("Unknown type parameter, cannot continue operation");

            for (int i = 0; i < Floats1.Capacity; i++)
                Floats1.Insert(i, BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadSingle()), 0));

            reader.BaseStream.Seek(baseOffset + Type, SeekOrigin.Begin);

            if (Type == 0x14)
            {
                reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);

                Floats2 = new List<Double>(3);
            }
            else if (Type == 0x1C)
            {
                Floats2 = new List<Double>(4);
            }
            else throw new Exception("Unknown type parameter, cannot continue operation");

            for (int i = 0; i < Floats2.Capacity; i++)
                Floats2.Insert(i, BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadSingle()), 0));

            GUID = reader.ReadUInt32();
            Flags2 = reader.ReadUInt32();
        }
    }
}
