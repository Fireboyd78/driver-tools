using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Zartex
{
    public static class BinaryReaderExtensions
    {
        public static uint ByteAlignPadding(this BinaryReader f, uint byteAlign)
        {
            uint offset = (uint)f.BaseStream.Position;

            return (byteAlign - (offset % byteAlign)) % byteAlign;
        }

        public static byte PeekByte(this BinaryReader f)
        {
            byte b = f.ReadByte();

            --f.BaseStream.Position;

            return b;
        }

        public static string ReadCString(this BinaryReader f)
        {   
            if (f.PeekByte() == 0x0) return "";

            StringBuilder str = new StringBuilder();

            while (f.PeekByte() != 0x0)
            {
                str.Append(f.ReadChar());
            }

            ++f.BaseStream.Position;

            return str.ToString();
        }

        public static long GetPosition(this BinaryReader f)
        {
            return f.BaseStream.Position;
        }

        public static long Seek(this BinaryReader f, long offset, SeekOrigin origin)
        {
            return f.BaseStream.Seek(offset, origin);
        }
    }
}
