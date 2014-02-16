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
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

using HelixToolkit.Wpf;

namespace DSCript.Models
{
    // This isn't designed to be used as a normal class, so use at your own risk
    public class BlendWeightsModel3D : GeometryModelBase3D
    {
        protected bool flipUVs = false;

        protected Boolean UseBlendWeights { get; set; }

        public static implicit operator GeometryModel3D(BlendWeightsModel3D model)
        {
            return model.ToGeometry();
        }

        public override GeometryModel3D ToGeometry()
        {
            int nVerts = Vertices.Count;

            Point3DCollection positions        = new Point3DCollection(nVerts);
            Vector3DCollection normals         = new Vector3DCollection(nVerts);
            PointCollection textureCoordinates = new PointCollection(nVerts);

            foreach (Vertex vertex in Vertices)
            {
                Point3D pos = (UseBlendWeights) ? Vertex.Tween(vertex.Positions, vertex.BlendWeights, 1.0) : vertex.Positions;

                positions.Add(pos);
                normals.Add(vertex.Normals);
                textureCoordinates.Add(vertex.UVs);
            }

            if (flipUVs)
            {
                for (int v = 0; v < textureCoordinates.Count; v++)
                {
                    Point vx = textureCoordinates[v];

                    vx.Y = -vx.Y;

                    textureCoordinates[v] = vx;
                }
            }

            MeshGeometry3D mesh = new MeshGeometry3D() {
                Positions = positions,
                Normals = normals,
                TextureCoordinates = textureCoordinates,
                TriangleIndices = TriangleIndices
            };

            return new GeometryModel3D() {
                Geometry = mesh,
                Material = Material,
                BackMaterial = (DoubleSided) ? Material : null
            };
        }

        public BlendWeightsModel3D() : base() { }
        public BlendWeightsModel3D(int numVertices, int numIndices) : base(numVertices, numIndices) { }
        public BlendWeightsModel3D(List<Vertex> vertices, Int32Collection triangleIndices) : base(vertices, triangleIndices) { }
    }
}
