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
using System.Windows.Media.Media3D;

namespace Antilli.Models
{
    public class Mesh
    {
        public List<Vertex> Vertices { get; set; }
        public List<TriangleFace> Faces { get; set; }

        public static Mesh Create(ModelsPackage modelPackage, IndexedPrimitive mesh, bool useBlendWeights)
        {
            Vertex[] Vertices = modelPackage.Vertices.Buffer;
            ushort[] Indices = modelPackage.Indices.Buffer;

            List<Vertex> vertices = new List<Vertex>();
            List<TriangleFace> faces = new List<TriangleFace>();

            for (int v = 0; v < mesh.NumVertices; v++)
            {
                Vertex vertex = Vertices[v + mesh.BaseVertexIndex + mesh.MinIndex].Copy();

                if (useBlendWeights)
                {
                    vertex.Position.X += vertex.BlendWeights.X * 1.0;
                    vertex.Position.Y += vertex.BlendWeights.Y * 1.0;
                    vertex.Position.Z += vertex.BlendWeights.Z * 1.0;
                }

                vertices.Add(vertex);
            }

            for (int i = 0; i < mesh.PrimitiveCount; i++)
            {
                int i0, i1, i2;

                if (i % 2 == 1.0)
                {
                    i0 = Indices[i + mesh.StartIndex];
                    i1 = Indices[(i + 1) + mesh.StartIndex];
                    i2 = Indices[(i + 2) + mesh.StartIndex];
                }
                else
                {
                    i0 = Indices[(i + 2) + mesh.StartIndex];
                    i1 = Indices[(i + 1) + mesh.StartIndex];
                    i2 = Indices[i + mesh.StartIndex];
                }

                if ((i0 != i1) && (i0 != i2) && (i1 != i2))
                    faces.Add(new TriangleFace(i0, i1, i2));
            }

            return new Mesh(vertices, faces);
        }

        public MeshGeometry3D ToMeshGeometry3D()
        {
            Point3DCollection vertices = new Point3DCollection();
            Vector3DCollection normals = new Vector3DCollection();
            PointCollection texCoords = new PointCollection();

            Int32Collection triIndices = new Int32Collection();

            foreach (Vertex v in Vertices)
            {
                vertices.Add(v.Position.ToPoint3D());
                normals.Add(v.Normals.ToVector3D());
                texCoords.Add(v.UVMap.ToPoint());
            }

            foreach (TriangleFace t in Faces)
            {
                triIndices.Add(t.P1);
                triIndices.Add(t.P2);
                triIndices.Add(t.P3);
            }

            return new MeshGeometry3D() {
                Positions = vertices,
                Normals = normals,
                TextureCoordinates = texCoords,
                TriangleIndices = triIndices
            };
        }

        public Mesh(List<Vertex> vertices, List<TriangleFace> faces)
        {
            Vertices = vertices;
            Faces = faces;
        }
    }
}
