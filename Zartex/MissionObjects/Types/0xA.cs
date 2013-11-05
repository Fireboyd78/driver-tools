using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0xA : IMissionObject
    {
        private uint _type = 0;

        public int ID
        {
            get { return 0xA; }
        }

        public int Size
        {
            get
            {
                return 0x18;
            }
        }

        public uint Type
        {
            get { return _type; }
            set { _type = value; }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<Double> Floats { get; set; }

        public BlockType_0xA(BinaryReader reader)
        {
            Type = reader.ReadUInt32();

            if (Type == 0x14)
                reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);

            Floats = new List<Double>((Type != 0x14) ? 4 : 3);

            for (int i = 0; i < Floats.Capacity; i++)
                Floats.Insert(i, BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadSingle()), 0));
        }
    }
}
