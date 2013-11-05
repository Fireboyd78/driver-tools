using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0xC : IMissionObject
    {
        public int ID
        {
            get { return 0xC; }
        }

        public int Size
        {
            get
            {
                return 0x1C;
            }
        }

        public double VarFloat { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<Double> Floats { get; set; }

        public BlockType_0xC(BinaryReader reader)
        {
            reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);

            VarFloat = BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadSingle()), 0);

            reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);

            Floats = new List<Double>(3);

            for (int i = 0; i < Floats.Capacity; i++)
                Floats.Insert(i, BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadSingle()), 0));
        }
    }
}
