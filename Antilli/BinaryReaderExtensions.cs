using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace System.IO
{
    public static class BinaryReaderExtensions
    {
        /// <summary>Aligns the cursor's current position to the specified byte-alignment</summary>
        /// <param name="byteAlignment">The byte-alignment to calculate (e.x. 1024)</param>
        /// <returns>Cursor position after byte-alignment</returns>
        public static long Align(this BinaryReader f, long byteAlignment)
        {
            long offset = f.BaseStream.Position;
            long align = (byteAlignment - (offset % byteAlignment)) % byteAlignment;

            return f.BaseStream.Seek(align, SeekOrigin.Current);
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
