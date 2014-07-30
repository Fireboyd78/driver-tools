using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0xC : MissionObject
    {
        public override int Id
        {
            get { return 0xC; }
        }

        public override int Size
        {
            get { return 0x1C; }
        }

        public double VarFloat { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats { get; set; }

        public BlockType_0xC(BinaryReader reader)
        {
            Offset = (int)reader.GetPosition();

            reader.BaseStream.Seek(4, SeekOrigin.Current);

            VarFloat = (double)reader.ReadSingle();

            reader.BaseStream.Seek(4, SeekOrigin.Current);

            Floats = new List<double>(3);

            for (int i = 0; i < Floats.Capacity; i++)
                Floats.Add((double)reader.ReadSingle());
        }
    }
}
