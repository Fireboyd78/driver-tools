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
    public class VertexBuffer
    {
        static readonly VertexDeclaration[] D3VertexDecls = {
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

        static readonly VertexDeclaration[] D4VertexDecls = {
            null,
            null,
            null,
            null,
            new[] {
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Normal,                 0),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.TextureCoordinate,      0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Tangent,                0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.BiNormal,               0),
                new VertexDeclInfo(VertexDataType.Color,    VertexUsageType.Color,                  0),
            } /* size = 0x3C */,
            new[] {
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               0),
                new VertexDeclInfo(VertexDataType.Color,    VertexUsageType.Color,                  0),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.TextureCoordinate,      0),
            } /* size = 0x18 */,
            new[] {
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Normal,                 0),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.TextureCoordinate,      0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Tangent,                0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.BiNormal,               0),
                new VertexDeclInfo(VertexDataType.Color,    VertexUsageType.BlendIndices,           0),
                new VertexDeclInfo(VertexDataType.Color,    VertexUsageType.BlendWeight,            0),
            } /* size = 0x40 */,
            new[] {
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Normal,                 0),
                new VertexDeclInfo(VertexDataType.Vector2,  VertexUsageType.TextureCoordinate,      0),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Position,               1),
                new VertexDeclInfo(VertexDataType.Vector3,  VertexUsageType.Normal,                 1),
                new VertexDeclInfo(VertexDataType.Float,    VertexUsageType.Tangent,                0),
            } /* size = 0x3C */,
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
        
        static int GetD3VertexDeclType(int type)
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
        
        static int GetD4VertexDeclType(int type)
        {
            switch (type)
            {
            case 5: return 7;
            case 6: return 6;
            case 8: return 8;
            }

            return 4;
        }

        public static int GetVertexDeclType(int version, int type)
        {
            switch (version)
            {
            case 1:
                return GetD4VertexDeclType(type);
            case 6:
            case 9:
                return GetD3VertexDeclType(type);
            }

            throw new InvalidOperationException("Couldn't determine vertex declaration index!");
        }

        public static int GetVertexDecl(int version, int type, out VertexDeclaration decl)
        {
            int index = 0;

            switch (version)
            {
            case 1:
                index = GetD4VertexDeclType(type);
                decl = D4VertexDecls[index];
                break;
            case 6:
            case 9:
                index = GetD3VertexDeclType(type);
                decl = D3VertexDecls[index];
                break;
            default:
                throw new InvalidOperationException("Couldn't get vertex declaration!");
            }

            return index;
        }

        public static VertexBuffer Create(int version, int type)
        {
            return new VertexBuffer(version, type);
        }
        
        private VertexDeclaration m_decl = null;

        public int Type { get; }
        public int Format { get; }

        public int Version { get; }
        
        public VertexDeclaration Declaration
        {
            get { return m_decl; }
        }
        
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

        public bool HasPositions
        {
            get { return Declaration.HasType<Vector3>(VertexUsageType.Position, 0); }
        }

        public bool HasNormals
        {
            get { return Declaration.HasType<Vector3>(VertexUsageType.Normal, 0); }
        }

        public bool HasTextureCoordinates
        {
            get { return Declaration.HasType<Vector2>(VertexUsageType.TextureCoordinate, 0); }
        }

        public void CreateVertices(byte[] buffer, int count)
        {
            Vertices = new List<VertexData>(count);

            for (int v = 0; v < count; v++)
            {
                var vertex = new VertexData(Declaration, buffer, (v * Declaration.SizeOf));
                Vertices.Add(vertex);
            }
        }

        public void CreateVertices(List<Vertex> vertices)
        {
            Vertices = new List<VertexData>();

            foreach (var v in vertices)
            {
                var vertex = CreateVertex(v);

                Vertices.Add(vertex);
            }
        }
        
        public VertexData CreateVertex()
        {
            return new VertexData(Declaration);
        }

        public VertexData CreateVertex(Vertex source)
        {
            var vertex = new VertexData(Declaration);

            if (Declaration.HasType<Vector3>(VertexUsageType.Position, 0))
            {
                vertex.SetData(VertexUsageType.Position, 0, source.Position);

                if (Declaration.HasType<Vector3>(VertexUsageType.Position, 1))
                    vertex.SetData(VertexUsageType.Position, 1, source.PositionW);
            }

            if (Declaration.HasType<Vector3>(VertexUsageType.Normal, 0))
            {
                vertex.SetData(VertexUsageType.Normal, 0, source.Normal);

                if (Declaration.HasType<Vector3>(VertexUsageType.Normal, 1))
                    vertex.SetData(VertexUsageType.Normal, 1, source.NormalW);
            }

            if (Declaration.HasType<Vector2>(VertexUsageType.TextureCoordinate, 0))
                vertex.SetData(VertexUsageType.TextureCoordinate, 0, source.UV);

            if (Declaration.HasType<Vector4>(VertexUsageType.BlendWeight, 0))
                vertex.SetData(VertexUsageType.BlendWeight, 0, source.BlendWeight);


            if (Declaration.HasType<ColorRGBA>(VertexUsageType.Color, 0))
                vertex.SetData(VertexUsageType.Color, 0, (ColorRGBA)source.Color);
            else if (Declaration.HasType<Vector4>(VertexUsageType.Color, 0))
                vertex.SetData(VertexUsageType.Color, 0, (Vector4)source.Color);

            switch (Declaration.GetType(VertexUsageType.Tangent, 0))
            {
            case VertexDataType.Float:
                vertex.SetData(VertexUsageType.Tangent, 0, source.Tangent);
                break;
            case VertexDataType.Vector3:
                vertex.SetData(VertexUsageType.Tangent, 0, (Vector3)source.TangentVector);
                break;
            case VertexDataType.Vector4:
                vertex.SetData(VertexUsageType.Tangent, 0, source.TangentVector);
                break;
            }

            return vertex;
        }

        public bool CanUseForType(int version, int type)
        {
            var otherType = GetVertexDeclType(version, type);
            
            if (Version != version)
                throw new InvalidOperationException($"Vertex buffer version mismatch! ({version} != {Version})");

            return (Format == otherType);
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

        protected VertexBuffer(int version, int type)
        {
            Type = type;
            Format = GetVertexDecl(version, type, out m_decl);
            Version = version;
            Vertices = new List<VertexData>();
        }

        protected VertexBuffer(VertexDeclaration declaration)
        {
            m_decl = declaration;
            Vertices = new List<VertexData>();
        }

        protected VertexBuffer(VertexDeclaration declaration, int count)
        {
            m_decl = declaration;
            Vertices = new List<VertexData>(count);
        }
    }
}
