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

namespace System.IO
{
    public sealed class BinaryReaderEx : BinaryReader
    {
        public long Position
        {
            get { return BaseStream.Position; }
            set { BaseStream.Position = value; }
        }

        public long Seek(long offset)
        {
            return BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public BinaryReaderEx(Stream input) : base(input) { }
        public BinaryReaderEx(Stream input, Encoding encoding) : base(input, encoding) { }
        public BinaryReaderEx(string path) : this(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) { }
    }
}
