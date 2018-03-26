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
    public class MeshDefinition
    {
        /// <summary>
        /// The <see cref="ModelPackage"/> this mesh belongs to.
        /// </summary>
        public ModelPackage ModelPackage { get; set; }

        public Driv3rModelFile ModelFile
        {
            get { return ModelPackage.ModelFile; }
        }

        public VertexBuffer VertexBuffer
        {
            get
            {
                if (PartsGroup != null)
                    return PartsGroup.VertexBuffer;

                return null;
            }
        }

        /// <summary>
        /// The <see cref="PartsGroup"/> this mesh belongs to.
        /// </summary>
        public PartsGroup PartsGroup { get; set; }

        /// <summary>
        /// The <see cref="MeshGroup"/> this mesh belongs to.
        /// </summary>
        public MeshGroup MeshGroup { get; set; }
        
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

                    ModelPackage mPak = ModelFile.GetModelPackage(SourceUID);

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

        public List<Vertex> GetVertices(bool adjustVertices = false)
        {
            var vBuffer = VertexBuffer;

            if (!vBuffer.Has3DVertices)
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
            default:
                throw new InvalidOperationException($"Unsupported primitive type '{PrimitiveType}'!");
            }

            return tris;
        }

        public MeshDefinition() { }
        public MeshDefinition(ModelPackage modelPackage)
        {
            ModelPackage = modelPackage;
        }
    }
}