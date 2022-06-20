using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public byte[] GetVerticesFrom(VertexDeclaration other, List<VertexData> vertices, out int numVertices)
        {
            var count = vertices.Count;
            var length = other.SizeOf;
            var fails = 0;

            var dstBuffer = new byte[length * count];
            var dstOffset = 0;

            var remapper = new List<Tuple<
                // dst offset   src offset/size
                int,            int, int
            >>();

            var luDestSizes = new Dictionary<int, int>();

            // build the remapper using their declaration
            foreach (var entry in other.Entries)
            {
                var dataType = entry.DataType;
                var usageType = entry.UsageType;
                var usageIndex = entry.UsageIndex;

                var dstSize = entry.SizeOf;

                luDestSizes.Add(dstOffset, dstSize);

                var srcOffset = 0;
                var srcSize = 0;

                if (GetOffsetAndSize(usageType, usageIndex, out srcOffset, out srcSize))
                {
                    if (srcSize <= dstSize)
                    {
                        remapper.Add(new Tuple<int, int, int>(dstOffset, srcOffset, srcSize));
                    }
                    else
                    {
                        // different data type; conversion needed?
                        fails++;
                    }
                }
                else
                {
                    fails++;
                }

                dstOffset += dstSize;
            }

            var quickResolve = false;

            if (fails > 0)
                Debug.WriteLine($"Could not resolve {fails} entries to the new declaration.");
            else
            {
                var srcSize = 0;
                var dstSize = dstOffset;
                
                // can we just copy the buffers straight through?
                foreach (var remap in remapper)
                {
                    var srcOffset = srcSize;

                    // non-continuous offsets?
                    if (srcOffset != remap.Item1 || remap.Item2 != srcOffset)
                        break;

                    // add size
                    srcSize += remap.Item3;

                    var size = (srcSize - srcOffset);

                    // src buffer isn't sequential like dst buffer?
                    if (size != luDestSizes[remap.Item1])
                    {
                        // fail the check below
                        srcSize = 0;
                        break;
                    }
                }

                if (srcSize == dstSize)
                {
                    Debug.WriteLine($"Quick resolve succeeded!");
                    quickResolve = true;
                }
            }

            var dstIndex = 0;

            // process all vertices
            if (quickResolve)
            {
                // dstOffset hasn't been fiddled with, so we can stil use it
                var dstSize = dstOffset;

                // copy straight through :)
                foreach (var vertex in vertices)
                {
                    var srcBuffer = vertex.Buffer;
                    var offset = (dstIndex++ * length);

                    // can copy the entire buffer straight-through
                    Buffer.BlockCopy(srcBuffer, 0, dstBuffer, offset, dstSize);
                }
            }
            else
            {   
                // remap all vertices to their new location
                foreach (var vertex in vertices)
                {
                    var srcBuffer = vertex.Buffer;

                    // start offset of dst vertex
                    var offset = (dstIndex++ * length);

                    foreach (var remap in remapper)
                    {
                        // offset into dst vertex
                        dstOffset = offset + remap.Item1;

                        var srcOffset = remap.Item2;
                        var srcSize = remap.Item3;

                        // copy partial data from buffer into new buffer
                        Buffer.BlockCopy(srcBuffer, srcOffset, dstBuffer, dstOffset, srcSize);
                    }
                }
            }

            numVertices = dstIndex;

            return dstBuffer;
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

        public bool GetOffsetAndSize(VertexUsageType usageType, short usageIndex, out int offset, out int size)
        {
            offset = 0;
            size = -1;

            foreach (var entry in m_entries)
            {
                if ((entry.UsageType == usageType) && (entry.UsageIndex == usageIndex))
                {
                    size = entry.SizeOf;
                    break;
                }

                offset += entry.SizeOf;
            }

            // failed?
            if (size == -1)
                return false;

            return true;
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

        public static VertexDeclaration CreateFromStream(Stream stream)
        {
            var checkSize = stream.ReadInt16();
            var reserved = stream.ReadInt16();

            var count = stream.ReadInt32();
            var length = stream.ReadInt32();

            var decl = new VertexDeclaration(count);

            for (int i = 0; i < count; i++)
            {
                decl.m_entries[i].DataType = (VertexDataType)stream.ReadByte();
                decl.m_entries[i].UsageType = (VertexUsageType)stream.ReadByte();
                decl.m_entries[i].UsageIndex = stream.ReadInt16();
            }

            var declSize = decl.SizeOf;

            if (declSize != checkSize)
                throw new Exception($"Vertex declaration expected a size of {checkSize:X} but got {declSize:X} instead!");

            return decl;
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

        protected VertexDeclaration(int count)
        {
            m_entries = new VertexDeclInfo[count];
        }

        public VertexDeclaration(params VertexDeclInfo[] entries)
        {
            m_entries = entries;
        }
    }
}
