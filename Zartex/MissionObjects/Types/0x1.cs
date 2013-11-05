using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0x1 : IMissionObject
    {
        public class FieldData
        {
            public ushort Offset = 0;
            public byte[] Buffer = null;
        }

        private ushort _vOffset = 0;

        private FieldData Field1;
        private FieldData Field2;
        private FieldData Field3;

        public int ID
        {
            get { return 0x1; }
        }

        public int Size
        {
            get
            {
                if (_vOffset == 0 || Field1.Buffer == null || Field2.Buffer == null || Field3.Buffer == null)
                    throw new Exception("Cannot retrieve size from an uninitialized block.");

                return (
                    sizeof(uint) + sizeof(ushort) * 2 + // add header
                    _vOffset + Field2.Offset + // calculate size of data
                    sizeof(uint) // include voffset
                );
            }
        }

        public ushort VOffset
        {
            get { return _vOffset; }
            set { _vOffset = value; }
        }

        public ushort Reserved { get; set; }
        public ushort Unknown { get; set; }

        public uint VehicleID { get; set; }

        public BlockType_0x1(BinaryReader reader)
        {
            VOffset = reader.ReadUInt16();

            Reserved = reader.ReadUInt16();

            long baseOffset = reader.BaseStream.Position;

            Unknown = reader.ReadUInt16();

            Field1 = new FieldData();
            Field2 = new FieldData();
            Field3 = new FieldData();

            Field1.Offset = reader.ReadUInt16();
            Field2.Offset = reader.ReadUInt16();
            Field3.Offset = reader.ReadUInt16();

            // Just put this crap in a buffer
            reader.BaseStream.Seek(baseOffset + Field1.Offset, SeekOrigin.Begin);

            Field1.Buffer = new byte[Field1.Offset - Field2.Offset];
            reader.Read(Field1.Buffer, 0, Field1.Buffer.Length);

            reader.BaseStream.Seek(baseOffset + Field2.Offset, SeekOrigin.Begin);

            Field2.Buffer = new byte[Field3.Offset - Field1.Offset];
            reader.Read(Field2.Buffer, 0, Field2.Buffer.Length);

            reader.BaseStream.Seek(baseOffset + Field3.Offset, SeekOrigin.Begin);

            Field3.Buffer = new byte[VOffset - Field3.Offset];
            reader.Read(Field3.Buffer, 0, Field3.Buffer.Length);

            // Finally, read the vehicle ID
            reader.BaseStream.Seek(baseOffset + (VOffset + Field2.Offset), SeekOrigin.Begin);
            VehicleID = reader.ReadUInt32();
        }
    }
}
