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
        public static void WriteByteAlignment(this Stream stream, uint byteAlignment)
        {
            uint offset = (uint)stream.Position;
            int align = (int)Chunk.GetByteAlignment(offset, byteAlignment);

            stream.Write(Chunk.PaddingBytes, (Chunk.PaddingBytes.Length - align), align);
        }

        #region Read methods
        internal static int Read(this Stream stream, byte[] buffer)
        {
            stream.Read(buffer, 0, buffer.Length);
            return (buffer != null) ? 1 : -1;
        }

        public static char ReadChar(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(char)];
            stream.Read(buffer);

            return BitConverter.ToChar(buffer, 0);
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
        #endregion

        #region Write methods
        public static void WriteByte(this Stream stream, int value)
        {
            if (value > 255)
                stream.WriteByte(0xFF);

            stream.Write(BitConverter.GetBytes(value), 0, sizeof(byte));
        }

        public static void Write(this Stream stream, params byte[] values)
        {
            stream.Write(values, 0, values.Length);
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
