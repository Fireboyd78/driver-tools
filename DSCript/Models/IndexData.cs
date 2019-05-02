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

namespace DSCript.Models
{
    public class IndexBuffer
    {
        short[] m_Buffer = null;

        public int Length
        {
            get { return 2; }
        }

        public short[] Indices
        {
            get { return m_Buffer; }
            set
            {
                m_Buffer = value;
            }
        }

        public short this[int id]
        {
            get { return Indices[id]; }
            set { Indices[id] = value; }
        }

        public IndexBuffer(int count)
        {
            Indices = new short[count];
        }

        public IndexBuffer(byte[] buffer, int count)
        {
            var size = (count * Length);

            if (size > buffer.Length)
                throw new InvalidOperationException("Can't create index buffer -- not enough data!");
            
            m_Buffer = new short[count];

            var handle = GCHandle.Alloc(m_Buffer, GCHandleType.Pinned);

            Marshal.Copy(buffer, 0, handle.AddrOfPinnedObject(), size);
        }
    }
}
