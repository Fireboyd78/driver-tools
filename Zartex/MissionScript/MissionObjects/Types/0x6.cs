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
    public class BlockType_0x6 : MissionObject
    {
        public override int Id
        {
            get { return 0x6; }
        }

        public override int Size
        {
            get { return 0x1C; }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats { get; set; }

        public double UnkFloat { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public int UnkID { get; set; }

        public BlockType_0x6(BinaryReader reader)
        {
            Offset = (int)reader.GetPosition();

            Floats = new List<double>(3);

            for (int i = 0; i < Floats.Capacity; i++)
                Floats.Add((double)reader.ReadSingle());

            reader.BaseStream.Seek(4, SeekOrigin.Current);

            UnkFloat = (double)reader.ReadSingle();

            UnkID = reader.ReadInt32();
        }
    }
}
