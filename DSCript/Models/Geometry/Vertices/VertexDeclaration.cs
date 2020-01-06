using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DSCript.Models
{
    public class VertexDeclaration
    {
        public static readonly VertexDeclaration Empty = new VertexDeclaration(VertexDeclInfo.Empty);

        VertexDeclInfo[] m_entries;

        public VertexDeclInfo[] Entries
        {
            get { return m_entries; }
        }
        
        public int SizeOf
        {
            get
            {
                var size = 0;

                foreach (var entry in m_entries)
                    size += entry.SizeOf;

                return size;
            }
        }
        
        public VertexDataType GetType(VertexUsageType usageType, short usageIndex)
        {
            foreach (var entry in m_entries)
            {
                if ((entry.UsageType == usageType) && (entry.UsageIndex == usageIndex))
                    return entry.DataType;
            }

            return VertexDataType.None;
        }

        public bool HasType(VertexUsageType usageType, short usageIndex)
        {
            foreach (var entry in m_entries)
            {
                if ((entry.UsageType == usageType) && (entry.UsageIndex == usageIndex))
                    return true;
            }

            return false;
        }

        public bool HasType<T>(VertexUsageType usageType, short usageIndex)
            where T : struct
        {
            foreach (var entry in m_entries)
            {
                if ((entry.UsageType == usageType) && (entry.UsageIndex == usageIndex))
                    return (entry.TypeOf == typeof(T));
            }

            return false;
        }
        
        public int GetOffset(VertexUsageType usageType, short usageIndex)
        {
            var offset = 0;
            
            foreach (var entry in m_entries)
            {
                if ((entry.UsageType == usageType) && (entry.UsageIndex == usageIndex))
                    return offset;

                offset += entry.SizeOf;
            }

            return -1;
        }
        
        public byte[] GetData(VertexUsageType usageType, short usageIndex, byte[] buffer, int startIndex, out VertexDataType dataType)
        {
            var offset = (startIndex * SizeOf);
            var size = 0;

            if ((buffer == null) || (buffer.Length == 0))
                throw new ArgumentException("Buffer cannot be null or empty.", nameof(buffer));
            
            // setup incase we don't find anything
            dataType = VertexDataType.None;

            foreach (var entry in m_entries)
            {
                if ((entry.UsageType == usageType) && (entry.UsageIndex == usageIndex))
                {
                    dataType = entry.DataType;
                    size = entry.SizeOf;
                    
                    break;
                }

                offset += entry.SizeOf;
            }

            // nothing to return?
            if (size == 0)
                return new byte[0];

            if ((offset + size) > buffer.Length)
                throw new ArgumentException("Buffer is not large enough to retrieve vertex data.", nameof(buffer));

            var buf = new byte[size];
            Buffer.BlockCopy(buffer, offset, buf, 0, size);

            return buf;
        }

        public T GetData<T>(VertexUsageType usageType, short usageIndex, byte[] buffer)
            where T : struct
        {
            var offset = 0;

            foreach (var entry in m_entries)
            {
                var size = entry.SizeOf;

                if ((entry.UsageType == usageType) && (entry.UsageIndex == usageIndex))
                {
                    var type = typeof(T);

                    if (entry.TypeOf != type)
                        throw new InvalidCastException("Vertex data cannot be cast to the specified type.");

                    // one buffer to rule them all
                    var ptr = MemoryCache.Alloc(16, 0xF00, false);

                    Marshal.Copy(buffer, offset, ptr, size);

                    return (T)Marshal.PtrToStructure(ptr, typeof(T));
                }

                offset += size;
            }

            return default(T);
        }

        public bool SetData<T>(VertexUsageType usageType, short usageIndex, byte[] buffer, T data)
        {
            var offset = 0;

            foreach (var entry in m_entries)
            {
                var size = entry.SizeOf;

                if ((entry.UsageType == usageType) && (entry.UsageIndex == usageIndex))
                {
                    if (entry.TypeOf != typeof(T))
                        throw new InvalidCastException("Vertex data cannot be cast to the specified type.");

                    var gc = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                    try
                    {
                        Marshal.StructureToPtr(data, gc.AddrOfPinnedObject() + offset, false);
                        return true;
                    }
                    finally
                    {
                        gc.Free();
                    }
                }

                offset += size;
            }

            return false;
        }

        public static implicit operator VertexDeclaration(VertexDeclInfo[] entries)
        {
            return new VertexDeclaration(entries);
        }

        public void WriteTo(Stream stream)
        {
            stream.Write((short)SizeOf);
            stream.Write((short)0);

            stream.Write(m_entries.Length);
            stream.Write(m_entries.Length * 4);

            foreach (var info in m_entries)
            {
                stream.Write((byte)info.DataType);
                stream.Write((byte)info.UsageType);

                stream.Write(info.UsageIndex);
            }
        }

        public VertexDeclaration(params VertexDeclInfo[] entries)
        {
            m_entries = entries;
        }
    }
}
