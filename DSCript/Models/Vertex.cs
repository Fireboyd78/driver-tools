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
    public class Vertex
    {
        public FVFType VertexType { get; private set; }

        Vector3D _blendWeights;
        double _unknown = -1.0;

        /// <summary>Gets or sets the position of the vertex.</summary>
        public Point3D Positions { get; set; }

        /// <summary>Gets or sets the normals of the vertex.</summary>
        public Vector3D Normals { get; set; }

        /// <summary>Gets or sets the UV mapping of the vertex.</summary>
        public Point UVs { get; set; }

        /// <summary>Gets or sets the RGBA diffuse color of the vertex.</summary>
        public Color Diffuse { get; set; }

        /// <summary>Gets or sets the blending weights of the vertex. This field is only used with certain VertexType's, and is ignored otherwise.</summary>
        public Vector3D BlendWeights
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

        public static Point3D Tween(Point3D positions, Vector3D weights, double tweenFactor)
        {
            return Point3D.Add(positions, Vector3D.Multiply(weights, tweenFactor));
        }

        /// <summary>
        /// Returns the byte-array representing this <see cref="Vertex"/> in its compiled form.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            using (MemoryStream ms = new MemoryStream((int)VertexType))
            {
                ms.WriteFloat(-Positions.X);
                ms.WriteFloat(Positions.Z);
                ms.WriteFloat(Positions.Y);

                ms.WriteFloat(-Normals.X);
                ms.WriteFloat(Normals.Z);
                ms.WriteFloat(Normals.Y);

                ms.WriteFloat(UVs.X);
                ms.WriteFloat(UVs.Y);

                if (VertexType == FVFType.Vertex15 || VertexType == FVFType.Vertex16)
                {
                    ms.WriteFloat(-BlendWeights.X);
                    ms.WriteFloat(BlendWeights.Z);
                    ms.WriteFloat(BlendWeights.Y);
                }

                if (VertexType == FVFType.Vertex16)
                    ms.WriteFloat(Unknown);

                ms.Write(Diffuse.ScR);
                ms.Write(Diffuse.ScG);
                ms.Write(Diffuse.ScB);
                ms.Write(Diffuse.ScA);

                return ms.ToArray();
            }
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

            Positions = new Point3D();
            Normals = new Vector3D();
            UVs = new Point();
            Diffuse = Color.FromArgb(255, 0, 0, 0);

            if (VertexType == FVFType.Vertex15 || VertexType == FVFType.Vertex16)
            {
                _blendWeights = new Vector3D();

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

            Positions = vertex.Positions;
            Normals = vertex.Normals;
            UVs = vertex.UVs;
            Diffuse = vertex.Diffuse;

            if (VertexType == FVFType.Vertex15 || VertexType == FVFType.Vertex16)
            {
                _blendWeights = (vertex.BlendWeights != null)
                    ? vertex.BlendWeights
                    : new Vector3D();

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
                // IMPORTANT NOTE: The Y & Z Axes are flipped and the X axis is negated!

                Positions = new Point3D() {
                    X = -f.ReadSingle(),
                    Z = f.ReadSingle(),
                    Y = f.ReadSingle()
                };

                Normals = new Vector3D() {
                    X = -f.ReadSingle(),
                    Z = f.ReadSingle(),
                    Y = f.ReadSingle()
                };

                UVs = new Point() {
                    X = f.ReadSingle(),
                    Y = f.ReadSingle()
                };

                if (VertexType == FVFType.Vertex15 || VertexType == FVFType.Vertex16)
                {
                    BlendWeights = new Vector3D() {
                        X = -f.ReadSingle(),
                        Z = f.ReadSingle(),
                        Y = f.ReadSingle()
                    };
                }

                if (VertexType == FVFType.Vertex16)
                    Unknown = f.ReadSingle();

                float r = f.ReadSingle();
                float g = f.ReadSingle();
                float b = f.ReadSingle();
                float a = f.ReadSingle();

                Diffuse = Color.FromScRgb(a, r, g, b);
            }
        }
    }
}
