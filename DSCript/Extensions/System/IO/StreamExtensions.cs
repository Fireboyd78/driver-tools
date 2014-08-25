using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using DSCript;

namespace System.IO
{
    public static class StreamExtensions
    {
        public static long Seek(this Stream stream, long offset, long origin)
        {
            return stream.Seek((origin + offset), SeekOrigin.Begin);
        }

        public static long Align(this Stream stream, int alignment)
        {
            return (stream.Position = Memory.Align(stream.Position, alignment));
        }

        /// <summary>
        /// Fills the stream in its current position using the bytes from a specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="count">The number of bytes to fill the stream with.</param>
        public static void Fill(this Stream stream, byte[] buffer, int count)
        {
            // check buffer isn't null
            if (buffer == null)
                throw new ArgumentNullException("buffer", "The specified buffer is null and cannot be used to fill data into the stream.");

            // no negative numbers are allowed
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", count, "The number of bytes to copy from the buffer cannot be negative.");
            
            // skip zero-length requests
            if (count == 0)
                return;

            var bufLen = buffer.Length;
            var offset = 0;

            if (bufLen == 0)
                throw new ArgumentOutOfRangeException("buffer", buffer, "The specified buffer is empty and cannot be used to fill data into the stream.");
            
            while (offset < count)
            {
                if ((offset + bufLen) > count)
                {
                    if ((bufLen = (count - offset)) == 0)
                        break;
                }

                stream.Write(buffer, 0, bufLen);

                offset += bufLen;
            }
        }

        #region Peek methods
        public static int PeekByte(this Stream stream)
        {
            var b = stream.ReadByte();
            --stream.Position;

            return b;
        }

        public static int PeekInt16(this Stream stream)
        {
            int i = stream.ReadInt16();
            stream.Position -= 2;

            return i;
        }

        public static uint PeekUInt16(this Stream stream)
        {
            var i = stream.ReadUInt16();
            stream.Position -= 2;

            return i;
        }

        public static int PeekInt32(this Stream stream)
        {
            var i = stream.ReadInt32();
            stream.Position -= 4;

            return i;
        }

        public static uint PeekUInt32(this Stream stream)
        {
            var i = stream.ReadUInt32();
            stream.Position -= 4;

            return i;
        } 
        #endregion

        #region Read methods
        internal static int Read(this Stream stream, byte[] buffer)
        {
            stream.Read(buffer, 0, buffer.Length);
            return (buffer != null) ? 1 : -1;
        }

        public static byte[] ReadAllBytes(this Stream stream)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static byte[] ReadBytes(this Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            stream.Read(buffer);

            return buffer;
        }

        public static char ReadChar(this Stream stream)
        {
            return (char)stream.ReadByte();
        }

        public static char[] ReadChars(this Stream stream, int count)
        {
            char[] chars = new char[count];

            for (int i = 0; i < count; i++)
                chars[i] = stream.ReadChar();

            return chars;
        }

        public static short ReadInt16(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(short)];
            stream.Read(buffer);

            return BitConverter.ToInt16(buffer, 0);
        }

        public static ushort ReadUInt16(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(ushort)];
            stream.Read(buffer);

            return BitConverter.ToUInt16(buffer, 0);
        }

        public static int ReadInt32(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(int)];
            stream.Read(buffer);

            return BitConverter.ToInt32(buffer, 0);
        }

        public static uint ReadUInt32(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(uint)];
            stream.Read(buffer);

            return BitConverter.ToUInt32(buffer, 0);
        }

        public static long ReadInt64(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(long)];
            stream.Read(buffer);

            return BitConverter.ToInt64(buffer, 0);
        }

        public static ulong ReadUInt64(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(ulong)];
            stream.Read(buffer);

            return BitConverter.ToUInt64(buffer, 0);
        }

        public static float ReadHalf(this Stream stream)
        {
            var aShort = stream.ReadInt16();
            return (float)(((aShort & 0x8000) << 16) + ((aShort & 0x7FFF) << 13) + ((127 - 15) << 23));
        }

        public static double ReadFloat(this Stream stream)
        {
            var val = (double)stream.ReadSingle();

            return Math.Round(val, 3);
        }

        public static float ReadSingle(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(float)];
            stream.Read(buffer);

            return BitConverter.ToSingle(buffer, 0);
        }

        public static double ReadDouble(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(double)];
            stream.Read(buffer);

            return BitConverter.ToDouble(buffer, 0);
        }

        public static string ReadString(this Stream stream)
        {
            string str = "";

            while (stream.PeekByte() != 0)
                str += stream.ReadChar();

            return str;
        }

        public static string ReadString(this Stream stream, int length)
        {
            return Encoding.UTF8.GetString(stream.ReadBytes(length));
        }

        public static string ReadUnicodeString(this Stream stream, int length)
        {
            return Encoding.Unicode.GetString(stream.ReadBytes(length));
        }
        #endregion

        #region Write methods
        public static void WriteByte(this Stream stream, int value)
        {
            if (value > 255)
                stream.WriteByte(0xFF);

            stream.Write(BitConverter.GetBytes(value), 0, sizeof(byte));
        }

        public static void WriteFloat(this Stream stream, double value)
        {
            stream.Write((float)value);
        }

        public static void Write(this Stream stream, byte byte1, byte byte2)
        {
            stream.Write(byte1);
            stream.Write(byte2);
        }

        public static void Write(this Stream stream, params byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this Stream stream, params char[] values)
        {
            stream.Write(Encoding.UTF8.GetBytes(values), 0, values.Length);
        }

        public static void Write(this Stream stream, byte value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(byte));
        }

        public static void Write(this Stream stream, char value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(char));
        }

        public static void Write(this Stream stream, string value)
        {
            stream.Write(Encoding.UTF8.GetBytes(value), 0, value.Length);
        }

        public static void Write(this Stream stream, string value, Encoding encoding)
        {
            var buffer = encoding.GetBytes(value);

            stream.Write(buffer, 0, buffer.Length);
        }

        public static void Write(this Stream stream, short value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(short));
        }

        public static void Write(this Stream stream, ushort value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(ushort));
        }

        public static void Write(this Stream stream, int value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(int));
        }

        public static void Write(this Stream stream, uint value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(uint));
        }

        public static void Write(this Stream stream, long value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(long));
        }

        public static void Write(this Stream stream, ulong value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(ulong));
        }

        public static void Write(this Stream stream, float value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(float));
        }

        public static void Write(this Stream stream, double value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(double));
        }
        #endregion
    }
}
