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

namespace DSCript
{
    public class BlockData : IDisposable
    {
        private byte[] _buffer;

        public ChunkFile ChunkFile { get; protected set; }
        public ChunkEntry Block { get; protected set; }

        public byte[] Buffer
        {
            get
            {
                if (_buffer != null)
                    return _buffer;

                byte[] buffer = new byte[Block.Size];

                ChunkFile.Reader.Seek(FileOffset, SeekOrigin.Begin);
                ChunkFile.Reader.Read(buffer, 0, buffer.Length);

                return buffer;
            }
            set
            {
                _buffer = value;
                Block.Size = (uint)_buffer.Length;
            }
        }

        protected internal uint FileOffset { get; private set; }

        public uint Size
        {
            get { return (_buffer != null) ? (uint)_buffer.Length : Block.Size; }
        }

        public void Dispose()
        {
            if (_buffer != null)
                _buffer = null;
        }

        public BlockData(ChunkFile file, ChunkEntry block)
        {
            ChunkFile = file;
            Block = block;

            FileOffset = Block.BaseOffset;
        }
    }
}
