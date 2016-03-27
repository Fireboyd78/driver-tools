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
    public class BlockType_0x2 : MissionObject
    {
        private short _type = 0;

        public override int Id
        {
            get { return 0x2; }
        }

        public override int Size
        {
            get
            {
                if (_type == 0) throw new Exception("Cannot retrieve size of uninitalized block");

                return (_type != 0x14) ? 0x38 : 0x30;
            }
        }

        public short Reserved { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public int Flags1 { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats1 { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats2 { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public int GUID { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public int Flags2 { get; set; }

        public BlockType_0x2(BinaryReader reader)
        {
            Offset = (int)reader.GetPosition();

            var offset = reader.ReadInt16();

            Reserved = reader.ReadInt16();

            Flags1 = reader.ReadInt32();

            if (offset == 0x14)
            {
                reader.Seek(4, SeekOrigin.Current);
                Floats1 = new List<double>(2);
            }
            else if (offset == 0x1C)
            {
                Floats1 = new List<double>(4);
            }
            else throw new Exception("Unknown offset type, cannot continue operation");

            for (int i = 0; i < Floats1.Capacity; i++)
                Floats1.Add((double)reader.ReadSingle());

            reader.Seek(Offset + offset, SeekOrigin.Begin);

            if (offset == 0x14)
            {
                reader.Seek(4, SeekOrigin.Current);

                Floats2 = new List<double>(3);
            }
            else if (offset == 0x1C)
            {
                Floats2 = new List<double>(4);
            }
            else throw new Exception("Unknown offset type, cannot continue operation");

            for (int i = 0; i < Floats2.Capacity; i++)
                Floats2.Add((double)reader.ReadSingle());

            GUID = reader.ReadInt32();
            Flags2 = reader.ReadInt32();
        }
    }
}
