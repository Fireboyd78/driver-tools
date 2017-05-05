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
        public FVFType VertexType { get; set; }
        
        /// <summary>Gets or sets the position of the vertex.</summary>
        public Vector3 Position { get; set; }

        /// <summary>Gets or sets the normals of the vertex.</summary>
        public Vector3 Normal { get; set; }

        /// <summary>Gets or sets the UV mapping of the vertex.</summary>
        public Vector2 UV { get; set; }

        /// <summary>Gets or sets the RGBA diffuse color of the vertex.</summary>
        public Color Diffuse { get; set; }

        /// <summary>Gets or sets the blending weights of the vertex. This field is only used with certain VertexType's, and is ignored otherwise.</summary>
        public Vector3 BlendWeights { get; set; }

        /// <summary>Gets or sets the unknown value of the vertex. This field is only used with certain VertexType's, and is ignored otherwise.</summary>
        public float Unknown { get; set; }
        
        /// <summary>
        /// Returns the byte-array representing this <see cref="Vertex"/> in its compiled form.
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            using (var ms = new MemoryStream((int)VertexType))
            {
                ms.WriteFloat(-Position.X);
                ms.WriteFloat(Position.Z);
                ms.WriteFloat(Position.Y);

                ms.WriteFloat(-Normal.X);
                ms.WriteFloat(Normal.Z);
                ms.WriteFloat(Normal.Y);

                ms.WriteFloat(UV.X);
                ms.WriteFloat(UV.Y);

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

        protected Vertex()
        {
            Position = new Vector3();
            Normal = new Vector3();
            UV = new Vector2();
            Diffuse = Color.FromArgb(255, 0, 0, 0);

            BlendWeights = new Vector3();

            Unknown = 1.0f;
        }

        /// <summary>Creates a new <see cref="Vertex"/>.</summary>
        /// <param name="vertexType">The <see cref="FVFType"/> this <see cref="Vertex"/> will be based on.</param>
        public Vertex(FVFType vertexType)
            : this()
        {
            if (vertexType == FVFType.Unknown)
                throw new InvalidEnumArgumentException("vertexType", -1, typeof(FVFType));

            VertexType = vertexType;   
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

            using (MemoryStream f = new MemoryStream(vertexBuffer))
            {
                // IMPORTANT NOTE: The Y & Z Axes are flipped and the X axis is negated!

                Position = new Vector3() {
                    X = -f.ReadSingle(),
                    Z = f.ReadSingle(),
                    Y = f.ReadSingle()
                };

                Normal = new Vector3() {
                    X = -f.ReadSingle(),
                    Z = f.ReadSingle(),
                    Y = f.ReadSingle()
                };

                UV = f.Read<Vector2>();

                if (VertexType == FVFType.Vertex15 || VertexType == FVFType.Vertex16)
                {
                    BlendWeights = new Vector3() {
                        X = -f.ReadSingle(),
                        Z = f.ReadSingle(),
                        Y = f.ReadSingle()
                    };
                }

                if (VertexType == FVFType.Vertex16)
                    Unknown = f.ReadSingle();

                var r = f.ReadSingle();
                var g = f.ReadSingle();
                var b = f.ReadSingle();
                var a = f.ReadSingle();

                Diffuse = Color.FromScRgb(a, r, g, b);
            }
        }

        public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            VertexType = FVFType.Vertex15;

            Position    = position;
            Normal      = normal;
            UV          = uv;

            Diffuse     = Color.FromArgb(255, 0, 0, 0);

            BlendWeights = new Vector3();
        }
    }
}
