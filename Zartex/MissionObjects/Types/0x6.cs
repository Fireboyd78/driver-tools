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
    public class BlockType_0x6 : IMissionObject
    {
        public int ID
        {
            get { return 0x6; }
        }

        public int Size
        {
            get
            {
                return 0x1C;
            }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<Double> Floats { get; set; }

        public double UnkFloat { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public uint UnkID { get; set; }

        public BlockType_0x6(BinaryReader reader)
        {
            Floats = new List<Double>(3);

            for (int i = 0; i < Floats.Capacity; i++)
                Floats.Insert(i, BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadSingle()), 0));

            reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);

            UnkFloat = BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadSingle()), 0);

            UnkID = reader.ReadUInt32();
        }
    }
}
