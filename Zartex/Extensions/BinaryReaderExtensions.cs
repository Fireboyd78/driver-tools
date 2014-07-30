using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Zartex
{
    public static class BinaryReaderExtensions
    {
        public static long Align(this BinaryReader f, int byteAlign)
        {
            return f.Seek(Memory.Align(f.BaseStream.Position, byteAlign), SeekOrigin.Begin);
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
