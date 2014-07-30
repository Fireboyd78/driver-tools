using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0xA : MissionObject
    {
        public override int Id
        {
            get { return 0xA; }
        }

        public override int Size
        {
            get { return 0x18; }
        }

        public int Type { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats { get; set; }

        public BlockType_0xA(BinaryReader reader)
        {
            Offset = (int)reader.GetPosition();

            Type = reader.ReadInt32();

            if (Type == 0x14)
                reader.BaseStream.Seek(4, SeekOrigin.Current);

            Floats = new List<double>((Type != 0x14) ? 4 : 3);

            for (int i = 0; i < Floats.Capacity; i++)
                Floats.Add((double)reader.ReadSingle());
        }
    }
}
