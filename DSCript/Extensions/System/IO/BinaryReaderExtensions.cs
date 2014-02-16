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
        public static long Align(this BinaryReader @this, long byteAlignment)
        {
            long offset = @this.BaseStream.Position;
            long align = (byteAlignment - (offset % byteAlignment)) % byteAlignment;

            return @this.BaseStream.Seek(align, SeekOrigin.Current);
        }

        public static byte PeekByte(this BinaryReader @this)
        {
            byte b = @this.ReadByte();

            --@this.BaseStream.Position;

            return b;
        }

        public static string ReadString(this BinaryReader @this, int length)
        {
            return Encoding.UTF8.GetString(@this.ReadBytes(length));
        }

        public static string ReadUnicodeString(this BinaryReader @this, int length)
        {
            return Encoding.Unicode.GetString(@this.ReadBytes(length));
        }

        public static long GetPosition(this BinaryReader @this)
        {
            return @this.BaseStream.Position;
        }

        public static long Seek(this BinaryReader @this, long offset, SeekOrigin origin)
        {
            return @this.BaseStream.Seek(offset, origin);
        }

        public static long SeekFromOrigin(this BinaryReader @this, long origin, long offset)
        {
            return @this.BaseStream.Seek(origin + offset, SeekOrigin.Begin);
        }

        public static string GetFilename(this BinaryReader @this)
        {
            if (@this.BaseStream is FileStream)
                return ((FileStream)@this.BaseStream).Name;

            return String.Empty;
        }
    }
}
