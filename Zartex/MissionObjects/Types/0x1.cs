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
    public class BlockType_0x1 : MissionObject
    {
        public class FieldData
        {
            public int Offset { get; set; }

            public byte Type { get; set; }

            [TypeConverter(typeof(CollectionConverter))]
            public List<double> Floats { get; set; }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<FieldData> Fields { get; set; }

        [Browsable(false)]
        protected int FieldSize { get; set; }

        public override int Id
        {
            get { return 0x1; }
        }

        public override int Size
        {
            get
            {
                if (Fields == null)
                    throw new Exception("Cannot retrieve size from an uninitialized block.");

                return (32 + FieldSize);
            }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats { get; set; }

        public short Reserved { get; set; }
        public short Unknown { get; set; }

        public int VehicleID { get; set; }

        public BlockType_0x1(BinaryReader reader)
        {
            Offset = (int)reader.GetPosition();

            FieldSize = reader.ReadInt16();
            Reserved = reader.ReadInt16();

            Unknown = reader.ReadInt16();

            long baseOffset = Offset + 4;

            Fields = new List<FieldData>(3);

            for (int i = 0; i < Fields.Capacity; i++)
            {
                reader.Seek(baseOffset + 2 + (i * 2), SeekOrigin.Begin);

                var field = new FieldData();

                field.Offset = reader.ReadInt16();

                reader.Seek(baseOffset + field.Offset, SeekOrigin.Begin);

                field.Type = reader.ReadByte();

                var size = reader.ReadByte() - 4;
                var nFloats = (size > 0) ? size / 4 : 0;

                reader.Seek(2, SeekOrigin.Current);

                field.Floats = new List<double>(nFloats);
                
                if (nFloats > 0)
                {
                    for (int k = 0; k < field.Floats.Capacity; k++)
                        field.Floats.Add((double)reader.ReadSingle());
                }

                Fields.Add(field);
            }

            Floats = new List<double>(3);

            for (int v = 0; v < Floats.Capacity; v++)
                Floats.Add((double)reader.ReadSingle());

            VehicleID = reader.ReadInt32();
        }
    }
}
