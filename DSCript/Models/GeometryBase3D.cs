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

namespace DSCript.Models
{
    public abstract class GeometryModelBase3D
    {
        public List<Vertex> Vertices { get; protected set;}

        public Int32Collection TriangleIndices { get; protected set; }

        public Material Material { get; set; }

        public Boolean DoubleSided { get; set; }

        public virtual GeometryModel3D ToGeometry()
        {
            int nVerts = Vertices.Count;

            Point3DCollection positions        = new Point3DCollection(nVerts);
            Vector3DCollection normals         = new Vector3DCollection(nVerts);
            PointCollection textureCoordinates = new PointCollection(nVerts);

            foreach (Vertex vertex in Vertices)
            {
                positions.Add(vertex.Positions);
                normals.Add(vertex.Normals);
                textureCoordinates.Add(vertex.UVs);
            }

            MeshGeometry3D mesh = new MeshGeometry3D() {
                Positions          = positions,
                Normals            = normals,
                TextureCoordinates = textureCoordinates,
                TriangleIndices    = TriangleIndices
            };

            return new GeometryModel3D() {
                Geometry     = mesh,
                Material     = Material,
                BackMaterial = (DoubleSided) ? Material : null
            };
        }

        protected GeometryModelBase3D()
        {
            Vertices = new List<Vertex>();
            TriangleIndices = new Int32Collection();
        }

        protected GeometryModelBase3D(int numVertices, int numIndices)
        {
            Vertices = new List<Vertex>(numVertices);
            TriangleIndices = new Int32Collection(numIndices);
        }

        protected GeometryModelBase3D(List<Vertex> vertices, Int32Collection triangleIndices)
        {
            Vertices = vertices;
            TriangleIndices = triangleIndices;
        }
    }
}
