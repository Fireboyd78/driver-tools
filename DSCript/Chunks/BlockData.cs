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
using DSCript.IO;
using DSCript.Methods;

namespace DSCript
{
    public sealed class BlockData
    {
        private byte[] _buffer;

        public ChunkFile File { get; set; }
        public ChunkEntry Block { get; set; }

        public byte[] Data
        {
            get
            {
                if (_buffer != null)
                    return _buffer;

                byte[] buffer = new byte[Block.Size];

                using (FileStream fs = File.GetStream())
                using (BinaryReader f = new BinaryReader(fs))
                {
                    f.Seek(FileOffset, SeekOrigin.Begin);
                    f.Read(buffer, 0, buffer.Length);
                }

                return buffer;
            }
            set
            {
                _buffer = value;
                Block.Size = (uint)_buffer.Length;
            }
        }

        public uint FileOffset { get; private set; }

        public uint Size
        {
            get { return (_buffer != null) ? (uint)_buffer.Length : Block.Size; }
        }

        public int ClearBuffer()
        {
            _buffer = null;
            return (_buffer == null) ? 1 : -1;
        }

        ~BlockData()
        {
            ClearBuffer();
        }

        public BlockData(ChunkFile file, ChunkEntry block)
        {
            File = file;
            Block = block;

            FileOffset = Block.BaseOffset;
        }
    }
}
