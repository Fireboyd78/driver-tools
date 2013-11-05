using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0x9 : IMissionObject
    {
        public int ID
        {
            get { return 0x9; }
        }

        public int Size
        {
            get
            {
                return 0x24;
            }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<Double> Floats1 { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<Double> Floats2 { get; set; }

        public BlockType_0x9(BinaryReader reader)
        {
            Floats1 = new List<Double>(4);

            for (int i = 0; i < Floats1.Capacity; i++)
                Floats1.Insert(i, BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadSingle()), 0));

            reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);

            Floats2 = new List<Double>(3);

            for (int i = 0; i < Floats2.Capacity; i++)
                Floats2.Insert(i, BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadSingle()), 0));
        }
    }
}
