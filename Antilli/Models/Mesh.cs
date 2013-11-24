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

using HelixToolkit.Wpf;

namespace Antilli.Models
{
    /* ================= New ======================== */
    public class DriverModel3D
    {
        static Color[] colors = {
                                Color.FromArgb(255, 255, 128, 128),
                                Color.FromArgb(255, 128, 255, 128),
                                Color.FromArgb(255, 128, 128, 255),
                                Color.FromArgb(255, 128, 32, 32),
                                Color.FromArgb(255, 32, 128, 32),
                                Color.FromArgb(255, 32, 32, 128),
                                Color.FromArgb(255, 128, 255, 32),
                                Color.FromArgb(255, 32, 128, 255),
                                Color.FromArgb(255, 255, 128, 32),
                                Color.FromArgb(255, 128, 128, 255),
                                Color.FromArgb(255, 128, 255, 255),
                                Color.FromArgb(255, 128, 255, 128),
                                Color.FromArgb(255, 32, 255, 255),
                                Color.FromArgb(255, 255, 32, 255),
                                Color.FromArgb(255, 255, 255, 255),
                                Color.FromArgb(255, 32, 32, 32),
                                Color.FromArgb(255, 31, 41, 76),
                                Color.FromArgb(255, 41, 76, 31),
                                Color.FromArgb(255, 76, 41, 31),
                                Color.FromArgb(255, 76, 31, 41),
                                Color.FromArgb(255, 76, 31, 76),
                                Color.FromArgb(255, 31, 76, 76),
                                Color.FromArgb(255, 31, 76, 31),
                            };

        public Point3DCollection BlendedPositions { get; set; }
        public Point3DCollection Positions { get; set; }
        public Vector3DCollection Normals { get; set; }
        public PointCollection TextureCoordinates { get; set; }
        public Int32Collection TriangleIndices { get; set; }

        public Material Material { get; set; }

        public static implicit operator GeometryModel3D(DriverModel3D model)
        {
            return model.ToGeometry();
        }

        public GeometryModel3D ToGeometry()
        {
            return ToGeometry(false);
        }

        public GeometryModel3D ToGeometry(bool useBlendWeights)
        {
            MeshGeometry3D mesh = new MeshGeometry3D() {
                Positions = (useBlendWeights) ? BlendedPositions : Positions,
                Normals = Normals,
                TextureCoordinates = TextureCoordinates,
                TriangleIndices = TriangleIndices
            };

            //-- Generate a random color
            //int colorIdx = new Random((int)DateTime.Now.ToBinary() * TriangleIndices.Count).Next(0, colors.Length);
            //Random random = new Random((int)DateTime.Now.ToBinary() / Positions.Count * (TriangleIndices.Count / 2));
            //
            //Color mixColor = Color.FromArgb(
            //        255,
            //        (byte)random.Next(random.Next(0, 254), 255),
            //        (byte)random.Next(random.Next(0, 254), 255),
            //        (byte)random.Next(random.Next(0, 254), 255)
            //    );
            //
            //SolidColorBrush matColor = new SolidColorBrush(Color.Add(colors[colorIdx], mixColor));

            SolidColorBrush matColor = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
            DiffuseMaterial material = new DiffuseMaterial(matColor);

            return new GeometryModel3D() {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }

        public DriverModel3D(ModelsPackage modelsPackage, IndexedPrimitive primitive)
        {
            Vertex[] vertices = modelsPackage.Vertices.Buffer;
            ushort[] indices = modelsPackage.Indices.Buffer;

            int nVerts = vertices.Length;

            Positions = new Point3DCollection(nVerts);
            Normals = new Vector3DCollection(nVerts);
            TextureCoordinates = new PointCollection(nVerts);

            if (modelsPackage.Vertices.VertexType != FVFType.Vertex12)
                BlendedPositions = new Point3DCollection(nVerts);

            for (int v = 0; v <= primitive.NumVertices; v++)
            {
                int vIdx = v + primitive.BaseVertexIndex + primitive.MinIndex;

                if (vIdx == vertices.Length)
                    break;

                Vertex vertex = vertices[vIdx];

                Positions.Add(vertex.Positions);
                Normals.Add(vertex.Normals);
                TextureCoordinates.Add(vertex.UVs);

                if (BlendedPositions != null)
                    BlendedPositions.Add(Vertex.Tween(vertex.Positions, vertex.BlendWeights, 1.0));
            }

            TriangleIndices = new Int32Collection();

            for (int i = 0; i < primitive.PrimitiveCount; i++)
            {
                int idx = primitive.StartIndex;
                int vIdx = primitive.BaseVertexIndex;

                int i0, i1, i2;

                if (i % 2 == 1.0)
                {
                    i0 = indices[idx + i];
                    i1 = indices[idx + (i + 1)];
                    i2 = indices[idx + (i + 2)];
                }
                else
                {
                    i0 = indices[idx + (i + 2)];
                    i1 = indices[idx + (i + 1)];
                    i2 = indices[idx + i];
                }

                // When reading in the vertices, the YZ-axis was flipped
                // Therefore i0 and i2 need to be flipped for proper face orientation
                // This was AFTER learning the hard way...
                if ((i0 != i1) && (i0 != i2) && (i1 != i2))
                {
                    TriangleIndices.Add(i2 - primitive.MinIndex);
                    TriangleIndices.Add(i1 - primitive.MinIndex);
                    TriangleIndices.Add(i0 - primitive.MinIndex);
                }
            }
        }
    }


    /* ================= Old ======================== */
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
                    Point3D.Add(vertex.Positions, (Vector3D.Multiply(vertex.BlendWeights, 1.0)));

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
                vertices.Add(v.Positions);
                normals.Add(v.Normals);
                texCoords.Add(v.UVs);
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
