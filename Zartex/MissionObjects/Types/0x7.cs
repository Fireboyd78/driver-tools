using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0x7 : MissionObject
    {
        public override int Id
        {
            get { return 0x7; }
        }

        public override int Size
        {
            get
            {
                return (8 + (Floats.Count * 4));
            }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats { get; set; }

        public BlockType_0x7(BinaryReader reader)
        {
            Offset = (int)reader.GetPosition();

            Floats = new List<double>(12);

            for (int i = 0; i < Floats.Capacity; i++)
            {
                Floats.Add((double)reader.ReadSingle());
            }

            // skip padding
            reader.BaseStream.Seek(4, SeekOrigin.Current);
        }
    }
}
