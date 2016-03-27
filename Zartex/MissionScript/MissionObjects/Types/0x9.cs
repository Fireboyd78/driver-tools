using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0x9 : MissionObject
    {
        public override int Id
        {
            get { return 0x9; }
        }

        public override int Size
        {
            get { return 0x24; }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats1 { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats2 { get; set; }

        public BlockType_0x9(BinaryReader reader)
        {
            Offset = (int)reader.GetPosition();

            Floats1 = new List<double>(4);
            Floats2 = new List<double>(3);

            for (int i = 0; i < Floats1.Capacity; i++)
                Floats1.Add((double)reader.ReadSingle());

            reader.BaseStream.Seek(4, SeekOrigin.Current);

            for (int i = 0; i < Floats2.Capacity; i++)
                Floats2.Add((double)reader.ReadSingle());
        }
    }
}
