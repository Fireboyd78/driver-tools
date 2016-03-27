using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0x3 : MissionObject
    {
        public override int Id
        {
            get { return 0x3; }
        }

        public override int Size
        {
            get { return (8 + (Floats.Count * 4)); }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats { get; set; }

        public BlockType_0x3(BinaryReader reader)
        {
            Offset = (int)reader.GetPosition();
            Floats = new List<double>(3);

            // skip padding
            reader.Seek(4, SeekOrigin.Current);

            for (int i = 0; i < Floats.Capacity; i++)
                Floats.Add((double)reader.ReadSingle());
        }
    }
}
