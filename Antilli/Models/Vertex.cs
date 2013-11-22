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

using Antilli;

namespace Antilli.Models
{
    public class Vertex
    {
        public FVFType VertexType { get; private set; }

        Vector3 _blendWeights;
        double _unknown = -1.0;

        /// <summary>Gets or sets the position of the vertex.</summary>
        public Vector3 Position { get; set; }

        /// <summary>Gets or sets the normals of the vertex.</summary>
        public Vector3 Normals { get; set; }

        /// <summary>Gets or sets the UV mapping of the vertex.</summary>
        public Vector2T UVMap { get; set; }

        /// <summary>Gets or sets the RGBA specular color of the vertex.</summary>
        public Vector4C Specular { get; set; }

        /// <summary>Gets or sets the blending weights of the vertex. This field is only used with certain VertexType's, and is ignored otherwise.</summary>
        public Vector3 BlendWeights
        {
            get { return _blendWeights; }
            set
            {
                if (VertexType == FVFType.Vertex15 || VertexType == FVFType.Vertex16)
                    _blendWeights = value;

                return;
            }
        }

        /// <summary>Gets or sets the unknown value of the vertex. This field is only used with certain VertexType's, and is ignored otherwise.</summary>
        public double Unknown
        {
            get { return _unknown; }
            set
            {
                if (VertexType == FVFType.Vertex16)
                    _unknown = value;

                return;
            }
        }

        /// <summary>
        /// Returns the byte-array representing this <see cref="Vertex"/> in its compiled form.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            byte[] bytes = new byte[(int)VertexType];

            Array.Copy(BitConverter.GetBytes((float)Position.X), 0, bytes, 0, 4);
            Array.Copy(BitConverter.GetBytes((float)Position.Y), 0, bytes, 4, 4);
            Array.Copy(BitConverter.GetBytes((float)Position.Z), 0, bytes, 8, 4);

            Array.Copy(BitConverter.GetBytes((float)Normals.X), 0, bytes, 12, 4);
            Array.Copy(BitConverter.GetBytes((float)Normals.Y), 0, bytes, 16, 4);
            Array.Copy(BitConverter.GetBytes((float)Normals.Z), 0, bytes, 20, 4);

            Array.Copy(BitConverter.GetBytes((float)UVMap.U), 0, bytes, 24, 4);
            Array.Copy(BitConverter.GetBytes((float)UVMap.V), 0, bytes, 28, 4);

            Array.Copy(BitConverter.GetBytes((float)Specular.A), 0, bytes, 32, 4);
            Array.Copy(BitConverter.GetBytes((float)Specular.R), 0, bytes, 36, 4);
            Array.Copy(BitConverter.GetBytes((float)Specular.G), 0, bytes, 40, 4);
            Array.Copy(BitConverter.GetBytes((float)Specular.B), 0, bytes, 44, 4);

            if (VertexType == FVFType.Vertex15 || VertexType == FVFType.Vertex16)
            {
                Array.Copy(BitConverter.GetBytes((float)BlendWeights.X), 0, bytes, 48, 4);
                Array.Copy(BitConverter.GetBytes((float)BlendWeights.Y), 0, bytes, 52, 4);
                Array.Copy(BitConverter.GetBytes((float)BlendWeights.Z), 0, bytes, 56, 4);

                if (VertexType == FVFType.Vertex16)
                    Array.Copy(BitConverter.GetBytes((float)Unknown), 0, bytes, 60, 4);
            }

            return bytes;
        }

        public Vertex Copy()
        {
            return new Vertex(this, VertexType);
        }

        /// <summary>Creates a new <see cref="Vertex"/>.</summary>
        /// <param name="vertexType">The <see cref="FVFType"/> this <see cref="Vertex"/> will be based on.</param>
        public Vertex(FVFType vertexType)
        {
            if (vertexType == FVFType.Unknown)
                throw new InvalidEnumArgumentException("vertexType", -1, typeof(FVFType));

            VertexType = vertexType;

            Position = new Vector3();
            Normals  = new Vector3();
            UVMap    = new Vector2T();
            Specular = new Vector4C();

            if (VertexType == FVFType.Vertex15 || VertexType == FVFType.Vertex16)
            {
                _blendWeights = new Vector3();

                if (VertexType == FVFType.Vertex16)
                    _unknown = 0.0;
            }
        }

        /// <summary>
        /// Creates a new <see cref="Vertex"/> based on an existing instance.
        /// </summary>
        /// <param name="vertex">The existing <see cref="Vertex"/> to copy values from</param>
        /// <param name="vertexType">The vertex format used. The new vertex will reflect upon this format.</param>
        public Vertex(Vertex vertex, FVFType vertexType)
        {
            if (vertexType == FVFType.Unknown)
                throw new InvalidEnumArgumentException("vertexType", -1, typeof(FVFType));

            VertexType = vertexType;

            Position = vertex.Position;
            Normals  = vertex.Normals;
            UVMap    = vertex.UVMap;
            Specular = vertex.Specular;

            if (VertexType == FVFType.Vertex15 || VertexType == FVFType.Vertex16)
            {
                _blendWeights = (vertex.BlendWeights != null)
                    ? vertex.BlendWeights
                    : new Vector3();

                if (VertexType == FVFType.Vertex16)
                    _unknown = (vertex.Unknown != -1.0)
                        ? vertex.Unknown
                        : 0.0;
            }
        }

        /// <summary>
        /// Creates a new <see cref="Vertex"/> based on a buffer.
        /// </summary>
        /// <param name="vertexBuffer">The bytes from a vertex buffer</param>
        /// <param name="vertexType">The vertex format used to read the buffer.</param>
        public Vertex(byte[] vertexBuffer, FVFType vertexType)
        {
            if (VertexType == FVFType.Unknown)
                throw new InvalidEnumArgumentException("vertexType", -1, typeof(FVFType));

            VertexType = vertexType;

            using (MemoryStream f = new MemoryStream(vertexBuffer, 0, vertexBuffer.Length))
            {
                Position = new Vector3() {
                    X = f.ReadSingle(),
                    Y = f.ReadSingle(),
                    Z = f.ReadSingle()
                };

                Normals = new Vector3() {
                    X = f.ReadSingle(),
                    Y = f.ReadSingle(),
                    Z = f.ReadSingle()
                };

                UVMap = new Vector2T() {
                    U = f.ReadSingle(),
                    V = f.ReadSingle()
                };

                Specular = new Vector4C() {
                    A = f.ReadSingle(),
                    R = f.ReadSingle(),
                    G = f.ReadSingle(),
                    B = f.ReadSingle()
                };

                if (VertexType == FVFType.Vertex15 || VertexType == FVFType.Vertex16)
                {
                    BlendWeights = new Vector3() {
                        X = f.ReadSingle(),
                        Y = f.ReadSingle(),
                        Z = f.ReadSingle()
                    };

                    if (VertexType == FVFType.Vertex16)
                        Unknown = f.ReadSingle();
                }
            }
        }
    }
}
