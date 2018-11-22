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
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

using DSCript;

namespace DSCript.Models
{
    public class SubModel
    {
        /// <summary>
        /// The <see cref="ModelPackage"/> this mesh belongs to.
        /// </summary>
        public ModelPackageResource ModelPackage { get; set; }

        public ModelFile ModelFile
        {
            get { return ModelPackage.ModelFile; }
        }

        public VertexBuffer VertexBuffer
        {
            get
            {
                if (Model != null)
                    return Model.VertexBuffer;

                return null;
            }
        }
        
        public Model Model { get; set; }
        public LodInstance LodInstance { get; set; }
        
        public PrimitiveType PrimitiveType { get; set; }
        
        public int VertexBaseOffset { get; set; }
        
        public int VertexOffset { get; set; }
        public int VertexCount { get; set; }

        public int IndexOffset { get; set; }
        public int IndexCount { get; set; }

        /// <summary>
        /// The material used for this mesh.
        /// </summary>
        public int MaterialId { get; set; }

        /// <summary>
        /// The UID of the package containing the material.
        /// </summary>
        public int SourceUID { get; set; }

        public MaterialDataPC GetMaterial()
        {
            try
            {
                if (SourceUID == ModelPackage.UID || SourceUID == 0xFFFD)
                    return ModelPackage.Materials[MaterialId];

                if (ModelFile != null)
                {
                    if (ModelFile is Driv3rVehiclesFile && SourceUID == (int)PackageType.VehicleGlobals)
                        return ((Driv3rVehiclesFile)ModelFile).VehicleGlobals.GetStandaloneTexture(MaterialId);

                    ModelPackageResource mPak = ModelFile.GetModelPackage(SourceUID);

                    if (mPak != null && mPak.HasMaterials)
                        return mPak.Materials[MaterialId];
                }
            }
            catch (Exception)
            {
                Debug.WriteLine($"Material load error -- ({MaterialId} : {SourceUID:X4})");
            }

            return null;
        }

        public List<Vertex> GetVertices(bool adjustVertices, ref List<int> indices)
        {
            var vBuffer = VertexBuffer;

            if (!vBuffer.HasPositions)
                return null;

            var vertices = new List<Vertex>(VertexCount);
            var fans = new List<int>();

            var indexBuffer = ModelPackage.IndexBuffer.Buffer;
            var indexOffset = (IndexOffset / 2);

            var lookup = new Dictionary<int, int>();
            var index = 0;

            switch (PrimitiveType)
            {
            case PrimitiveType.TriangleFan:
                // collect vertices + fans
                for (int v = 0; v < VertexCount; v++)
                {
                    var offset = indexOffset + v;
                    var vIdx = (ushort)indexBuffer[offset];

                    if (!lookup.ContainsKey(vIdx))
                    {
                        var vertex = vBuffer.Vertices[vIdx].ToVertex();
                        
                        if (adjustVertices)
                            vertex.FixDirection();

                        vertices.Add(vertex);
                        lookup.Add(vIdx, index++);
                    }

                    fans.Add(lookup[vIdx]);
                }

                // create triangle list
                // (it really was this simple...)
                for (int n = 2; n < fans.Count; n++)
                {
                    int f0, f1, f2;

                    if ((n % 2) != 0)
                    {
                        f0 = fans[n];
                        f1 = fans[n - 1];
                        f2 = fans[n - 2];
                    }
                    else
                    {
                        f0 = fans[n - 2];
                        f1 = fans[n - 1];
                        f2 = fans[n];
                    }

                    if ((f0 != f1) && (f0 != f2) && (f1 != f2))
                    {
                        indices.Add(f0);
                        indices.Add(f1);
                        indices.Add(f2);
                    }
                }

                // clean up after ourselves
                lookup.Clear();
                lookup = null;

                fans.Clear();
                fans = null;
                break;
            default:
                vertices = GetVertices(adjustVertices);
                indices = GetTriangleIndices(adjustVertices);
                break;
            }

            return vertices;
        }

        public List<Vertex> GetVertices(bool adjustVertices = false)
        {
            var vBuffer = VertexBuffer;

            if (!vBuffer.HasPositions)
                return null;

            var vertices = new List<Vertex>(VertexCount);
            
            for (int v = 0; v <= VertexCount; v++)
            {
                var vIdx = (VertexBaseOffset + VertexOffset + v);

                if (vIdx >= vBuffer.Count)
                    break;

                var vertex = vBuffer.Vertices[vIdx].ToVertex(adjustVertices);

                vertices.Add(vertex);
            }

            return vertices;
        }
        
        public List<Int32> GetTriangleIndices(bool swapOrder = false)
        {
            var indices = ModelPackage.IndexBuffer.Buffer;
            var idx = IndexOffset;

            var tris = new List<Int32>();

            switch (PrimitiveType)
            {
            case PrimitiveType.TriangleList:
                for (int i = 0; i < IndexCount; i++)
                {
                    var offset = idx + (i * 3);

                    int i0 = indices[offset];
                    int i1 = indices[offset + 1];
                    int i2 = indices[offset + 2];

                    tris.Add(i0 - VertexOffset);
                    tris.Add(i1 - VertexOffset);
                    tris.Add(i2 - VertexOffset);
                }
                break;
            case PrimitiveType.TriangleStrip:
                for (int i = 0; i < IndexCount; i++)
                {
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

                    if ((i0 != i1) && (i0 != i2) && (i1 != i2))
                    {
                        if (swapOrder)
                        {
                            tris.Add(i2 - VertexOffset);
                            tris.Add(i1 - VertexOffset);
                            tris.Add(i0 - VertexOffset);
                        }
                        else
                        {
                            tris.Add(i0 - VertexOffset);
                            tris.Add(i1 - VertexOffset);
                            tris.Add(i2 - VertexOffset);
                        }
                    }
                }
                break;
            case PrimitiveType.TriangleFan:
                throw new InvalidOperationException("Can't generate a triangle fan using this method!");
            default:
                throw new InvalidOperationException($"Unsupported primitive type '{PrimitiveType}'!");
            }

            return tris;
        }

        public SubModel() { }
        public SubModel(ModelPackageResource modelPackage)
        {
            ModelPackage = modelPackage;
        }
    }
}