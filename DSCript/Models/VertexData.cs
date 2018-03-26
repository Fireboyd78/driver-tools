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
using System.Windows.Media;

namespace DSCript.Models
{
    public struct VertexData
    {
        VertexDeclaration m_decl;
        byte[] m_buffer;

        public VertexDeclaration Declaration
        {
            get { return m_decl; }
        }

        public byte[] Buffer
        {
            get { return m_buffer; }
        }

        // this is some of the dirtiest code I've ever written...
        public unsafe bool PossiblyEqual(ref VertexData other)
        {
            try
            {
                fixed (byte* pBuf1 = Buffer)
                fixed (byte* pBuf2 = other.Buffer)
                {
                    for (int i = 0; i < Buffer.Length; i += 4)
                    {
                        if (*(float*)(pBuf1 + i) != *(float*)(pBuf2 + i))
                            return false;
                    }

                    return true;
                }
            }
            catch (AccessViolationException)
            {
                return false;
            }
        }
        
        public T GetData<T>(VertexUsageType usageType, short usageIndex)
            where T : struct
        {
            return m_decl.GetData<T>(usageType, usageIndex, m_buffer);
        }

        public bool SetData<T>(VertexUsageType usageType, short usageIndex, T value)
            where T : struct
        {
            return m_decl.SetData(usageType, usageIndex, m_buffer, value);
        }

        public Vertex ToVertex(bool adjustVertices = false)
        {
            var vertex = new Vertex();

            // Positions
            if (m_decl.HasType<Vector3>(VertexUsageType.Position, 0))
            {
                vertex.Position = GetData<Vector3>(VertexUsageType.Position, 0);

                if (m_decl.HasType<Vector3>(VertexUsageType.Position, 1))
                    vertex.PositionW = GetData<Vector3>(VertexUsageType.Position, 1);
            }
            // Normals
            if (m_decl.HasType<Vector3>(VertexUsageType.Normal, 0))
            {
                vertex.Normal = GetData<Vector3>(VertexUsageType.Normal, 0);

                if (m_decl.HasType<Vector3>(VertexUsageType.Normal, 1))
                    vertex.NormalW = GetData<Vector3>(VertexUsageType.Normal, 1);
            }
            // Texture coordinates
            if (m_decl.HasType<Vector2>(VertexUsageType.TextureCoordinate, 0))
                vertex.UV = GetData<Vector2>(VertexUsageType.TextureCoordinate, 0);
            // Blend weights
            if (m_decl.HasType<Vector4>(VertexUsageType.BlendWeight, 0))
                vertex.BlendWeight = GetData<Vector4>(VertexUsageType.BlendWeight, 0);
            // Colors
            if (m_decl.HasType<ColorRGBA>(VertexUsageType.Color, 0))
                vertex.Color = GetData<ColorRGBA>(VertexUsageType.Color, 0);
            else if (m_decl.HasType<Vector4>(VertexUsageType.Color, 0))
                vertex.Color = GetData<Vector4>(VertexUsageType.Color, 0);
            // Tangents
            switch (m_decl.GetType(VertexUsageType.Tangent, 0))
            {
            case VertexDataType.Float:
                vertex.Tangent = GetData<float>(VertexUsageType.Tangent, 0);
                break;
            case VertexDataType.Vector3:
                vertex.TangentVector = GetData<Vector3>(VertexUsageType.Tangent, 0);
                break;
            case VertexDataType.Vector4:
                vertex.TangentVector = GetData<Vector4>(VertexUsageType.Tangent, 0);
                break;
            }

            if (adjustVertices)
                vertex.FixDirection();
            
            return vertex;
        }

        public VertexData(VertexDeclaration decl)
        {
            m_decl = decl;
            m_buffer = new byte[m_decl.SizeOf];
        }

        public VertexData(VertexDeclaration decl, byte[] buffer, int offset)
            : this(decl)
        {
            Array.Copy(buffer, offset, m_buffer, 0, m_buffer.Length);
        }
    }

    public class VertexBuffer
    {
        static VertexDeclaration[] D3VertexDecls = {
            new[] {
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Normal,                 0),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.TextureCoordinate,      0),
                new VertexDeclInfo(VertexDataType.Vector4,  VertexUsageType.Tangent,                0),
            } /* size = 0x30 */,
            new[] {
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Normal,                 0),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.TextureCoordinate,      0),
                new VertexDeclInfo(VertexDataType.Vector4,  VertexUsageType.Tangent,                0),
            } /* size = 0x30 */,
            new[] {
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.Position,               0),
                new VertexDeclInfo(VertexDataType.Color,    VertexUsageType.Color,                  0),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.TextureCoordinate,      0),
            } /* size = 0x14 */,
            new[] {
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               0),
                new VertexDeclInfo(VertexDataType.Vector4,  VertexUsageType.Color,                  0),
            } /* size = 0x1C */,
            new[] {
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Normal,                 0),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.TextureCoordinate,      0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               1),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Normal,                 1),
                new VertexDeclInfo(VertexDataType.Float,    VertexUsageType.Tangent,                0),
            } /* size = 0x3C */,
            new[] {
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Normal,                 0),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.TextureCoordinate,      0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Tangent,                0),
                new VertexDeclInfo(VertexDataType.Color,    VertexUsageType.Color,                  0),
                new VertexDeclInfo(VertexDataType.Vector4,  VertexUsageType.BlendWeight,            0),
            } /* size = 0x40 */,
            new[] {
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               1),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.TextureCoordinate,      1),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.TextureCoordinate,      2),
                new VertexDeclInfo(VertexDataType.Color,    VertexUsageType.Color,                  0),
                new VertexDeclInfo(VertexDataType.Color,    VertexUsageType.Color,                  1),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.Position,               2),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.TextureCoordinate,      3),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.TextureCoordinate,      0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               0),
            } /* size = 0x50 */,
        };

        static int GetD3VertexDeclIndex(int size)
        {
            switch (size)
            {
            case 0x14: return 2;
            case 0x1C: return 3;
            case 0x30: return 1;
            case 0x3C: return 4;
            case 0x40: return 5;
            case 0x50: return 6;
            }

            return 0;
        }

        static int GetD3VertexDeclIndexByType(int type)
        {
            switch (type)
            {
            case 2:
            case 3:
            case 4:
                return 1;
            case 5:
                return 4;
            case 6:
                return 5;
            case 8:
                return 6;
            }

            return 0;
        }

        public VertexDeclaration Declaration { get; }
        
        public List<VertexData> Vertices { get; set; }

        public int Count
        {
            get { return (Vertices != null) ? Vertices.Count : 0; }
        }

        public int Size
        {
            get
            {
                if ((Vertices != null) && (Vertices.Count > 0))
                    return Vertices.Count * Declaration.SizeOf;

                return 0;
            }
        }

        public bool HasBlendWeights
        {
            get { return Declaration.HasType(VertexUsageType.BlendWeight, 0); }
        }

        public bool HasDamageVertices
        {
            get
            {
                return Declaration.HasType(VertexUsageType.Position, 1)
                    && Declaration.HasType(VertexUsageType.Normal, 1);
            }
        }

        public bool Has3DVertices
        {
            get { return Declaration.HasType<Vector3>(VertexUsageType.Position, 0); }
        }

        public bool Has3DNormals
        {
            get { return Declaration.HasType<Vector3>(VertexUsageType.Normal, 0); }
        }

        public bool HasTextureCoordinates
        {
            get { return Declaration.HasType<Vector2>(VertexUsageType.TextureCoordinate, 0); }
        }

        protected void CreateVertices(byte[] buffer, int count, int length)
        {
            Vertices = new List<VertexData>(count);
            
            for (int v = 0; v < count; v++)
            {
                var vertex = new VertexData(Declaration, buffer, (v * Declaration.SizeOf));
                Vertices.Add(vertex);
            }
        }
        
        public static VertexBuffer CreateD3Buffer(byte[] buffer, int count, int length, int sizeOf)
        {
            var index = GetD3VertexDeclIndex(sizeOf);
            var vBuffer = new VertexBuffer(D3VertexDecls[index], count);

            vBuffer.CreateVertices(buffer, count, length);
            return vBuffer;
        }

        public static VertexBuffer CreateD3Buffer(int vertexType)
        {
            var index = GetD3VertexDeclIndexByType(vertexType);

            return new VertexBuffer(D3VertexDecls[index]);
        }

        public VertexData CreateVertex()
        {
            return new VertexData(Declaration);
        }
        
        public void WriteTo(Stream stream, bool writeDeclInfo = false)
        {
            // not meant to be used in model package's
            if (writeDeclInfo)
            {
                stream.Write(Count);
                stream.Write(Count * Declaration.SizeOf);

                Declaration.WriteTo(stream);
            }

            foreach (var vertex in Vertices)
                stream.Write(vertex.Buffer);
        }

        protected VertexBuffer(VertexDeclaration declaration)
        {
            Declaration = declaration;
            Vertices = new List<VertexData>();
        }

        protected VertexBuffer(VertexDeclaration declaration, int count)
        {
            Declaration = declaration;
            Vertices = new List<VertexData>(count);
        }
    }
}
