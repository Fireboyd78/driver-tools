using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Zartex;

namespace Zartex.MissionObjects
{
    public abstract class ContainerBlock : IMissionObject
    {
        private const byte _unk = 0x10;

        protected const uint _byteAlign = 16;
        protected uint _byteAlignSize = 0;

        protected byte[] _buffer;
        
        public virtual int ID
        {
            get { return -1; }
        }

        public int Size
        {
            get
            {
                if (_buffer == null)
                    throw new Exception("Cannot return the size of an unintialized block.");

                return (
                    (sizeof(uint) + sizeof(ushort) + sizeof(byte)*2) + // header
                    (int)_byteAlignSize + // byte alignment
                    (int)_buffer.Length // actual block size
                );
            }
        }

        public uint BlockSize { get; set; }

        public byte UnkByte { get; set; }

        public byte Reserved
        {
            get { return _unk; }
        }

        public byte[] Buffer
        {
            get
            {
                if (_buffer == null)
                    throw new Exception("Cannot return an uninitialized buffer.");

                return _buffer;
            }
        }
    }
}
