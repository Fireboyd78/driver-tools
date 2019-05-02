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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DSCript.Models
{
    public enum VertexDataType : byte
    {
        None = 0,

        Float,

        Vector2,
        Vector3,
        Vector4,

        Color,
    }

    public enum VertexUsageType : byte
    {
        Unused = 0,

        Position,
        Normal,
        TextureCoordinate,

        BlendWeight,
        Tangent,

        Color,

        BiNormal,
        BlendIndices,
    }

    public struct VertexDeclInfo
    {
        public static readonly VertexDeclInfo Empty = new VertexDeclInfo();

        public VertexDataType DataType;
        public VertexUsageType UsageType;
        
        public short UsageIndex;
        
        public override int GetHashCode()
        {
            return DataType.GetHashCode() * 131
                ^ UsageType.GetHashCode() * 223
                ^ UsageIndex.GetHashCode() * 95;
        }

        public bool Equals(VertexDeclInfo decl)
        {
            return decl.DataType == DataType
                && decl.UsageType == UsageType
                && decl.UsageIndex == UsageIndex;
        }

        public override bool Equals(object obj)
        {
            if (obj is VertexDeclInfo)
                return Equals((VertexDeclInfo)obj);

            return false;
        }

        public int SizeOf
        {
            get { return GetDataSize(DataType); }
        }

        public Type TypeOf
        {
            get { return GetDataType(DataType); }
        }
        
        public static int GetDataSize(VertexDataType type)
        {
            switch (type)
            {
            case VertexDataType.None:       return 0;

            case VertexDataType.Float:
            case VertexDataType.Color:      return 4;

            case VertexDataType.Vector2:    return 8;
            case VertexDataType.Vector3:    return 12;
            case VertexDataType.Vector4:    return 16;
            }

            throw new InvalidEnumArgumentException("Invalid vertex data type.", (int)type, typeof(VertexUsageType));
        }

        public static Type GetDataType(VertexDataType type)
        {
            switch (type)
            {
            case VertexDataType.None:       return null;
            case VertexDataType.Float:      return typeof(float);
            case VertexDataType.Color:      return typeof(ColorRGBA);
            case VertexDataType.Vector2:    return typeof(Vector2);
            case VertexDataType.Vector3:    return typeof(Vector3);
            case VertexDataType.Vector4:    return typeof(Vector4);
            }

            throw new InvalidEnumArgumentException("Invalid vertex data type.", (int)type, typeof(VertexUsageType));
        }
        
        public VertexDeclInfo(VertexDataType dataType, VertexUsageType usageType, short usageIndex = 0)
        {
            DataType = dataType;
            UsageType = usageType;
            UsageIndex = usageIndex;
        }
    }
    
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
                    if (entry.TypeOf != typeof(T))
                        throw new InvalidCastException("Vertex data cannot be cast to the specified type.");
                    
                    var ptr = Marshal.AllocHGlobal(size);
                    
                    try
                    {
                        Marshal.Copy(buffer, offset, ptr, size);

                        return (T)Marshal.PtrToStructure(ptr, typeof(T));
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
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
    
    public struct ColorRGBA
    {
        byte R;
        byte G;
        byte B;
        byte A;

        public static implicit operator Color(ColorRGBA color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static implicit operator ColorRGBA(Color color)
        {
            return new ColorRGBA(color.R, color.G, color.B, color.A);
        }

        public ColorRGBA(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }
    
    public struct Vertex
    {
        /// <summary>Gets or sets the position of the vertex.</summary>
        public Vector3 Position;

        /// <summary>Gets or sets the normals of the vertex.</summary>
        public Vector3 Normal;

        /// <summary>Gets or sets the UV mapping of the vertex.</summary>
        public Vector2 UV;

        /// <summary>Gets or sets the RGBA diffuse color of the vertex.</summary>
        public Color Color;

        /// <summary>Gets or sets the blending weights of the vertex.</summary>
        public Vector4 BlendWeight;

        /// <summary>Gets or sets the tangent of the vertex.</summary>
        public float Tangent;

        public Vector4 TangentVector;

        public Vector3 PositionW;
        public Vector3 NormalW;

        public void ApplyScale(Vector3 scale)
        {
            Position *= scale;
            PositionW *= scale;
        }

        public void FixDirection()
        {
            Position = new Vector3(-Position.X, Position.Z, Position.Y);
            Normal = new Vector3(-Normal.X, Normal.Z, Normal.Y);

            PositionW = new Vector3(-PositionW.X, PositionW.Z, PositionW.Y);
            NormalW = new Vector3(-NormalW.X, NormalW.Z, NormalW.Y);
        }

        public void Reset()
        {
            Position = new Vector3();
            Normal = new Vector3();
            UV = new Vector2();
            Color = Color.FromArgb(255, 0, 0, 0);

            BlendWeight = new Vector4();

            Tangent = 1.0f;
        }
    }
}
