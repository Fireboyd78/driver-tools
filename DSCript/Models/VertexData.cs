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
            vertex.Reset();

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
}
