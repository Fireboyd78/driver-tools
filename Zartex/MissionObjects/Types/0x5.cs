using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0x5 : ContainerBlock
    {
        public override int ID
        {
            get { return 0x5; }
        }

        public BlockType_0x5(BinaryReader reader)
        {
            BlockSize = reader.ReadUInt16();
            UnkByte = reader.ReadByte();

            if (reader.ReadByte() != Reserved)
                throw new Exception("The unknown constant is incorrect, this may or may not be a developer error.");

            _byteAlignSize = reader.ByteAlignPadding(_byteAlign);
            reader.BaseStream.Seek(_byteAlignSize, SeekOrigin.Current);

            _buffer = new byte[BlockSize];
            reader.Read(_buffer, 0, _buffer.Length);
        }
    }
}
