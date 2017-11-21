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
    /// <summary>
    /// IDirect3DDevice9::DrawIndexedPrimitive -- Based on indexing, renders the specified geometric primitive into an array of vertices.
    /// 
    /// Source: http://msdn.microsoft.com/en-us/library/windows/desktop/bb174369%28v=vs.85%29.aspx
    /// </summary>
    public class MeshDefinition
    {
        /// <summary>
        /// The <see cref="ModelPackage"/> this mesh belongs to.
        /// </summary>
        public ModelPackage ModelPackage { get; protected set; }

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

        /// <summary>
        /// Member of the <see cref="Models.PrimitiveType"/> enumerated type, describing the type of primitive to render.
        /// </summary>
        public PrimitiveType PrimitiveType { get; set; }

        /// <summary>
        /// Offset from the start of the vertex buffer to the first vertex.
        /// </summary>
        public int BaseVertexIndex { get; set; }

        /// <summary>
        /// Minimum vertex index for vertices used during this call. This is a zero based index relative to BaseVertexIndex.
        /// </summary>
        public uint MinIndex { get; set; }

        /// <summary>
        /// Number of vertices used during this call. The first vertex is located at index: BaseVertexIndex + MinIndex.
        /// </summary>
        public uint NumVertices { get; set; }

        /// <summary>
        /// Index of the first index to use when accessing the vertex buffer. Beginning at StartIndex to index vertices from the vertex buffer.
        /// </summary>
        public uint StartIndex { get; set; }

        /// <summary>
        /// Number of primitives to render. The number of vertices used is a function of the primitive count and the primitive type.
        /// </summary>
        public uint PrimitiveCount { get; set; }

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

            return null;
        }

        public List<Vertex> GetVertices(bool adjustVertices = false)
        {
            var vBuffer = VertexBuffer;

            if (!vBuffer.Has3DVertices)
                return null;

            var vertices = new List<Vertex>((int)NumVertices);

            for (int v = 0; v <= NumVertices; v++)
            {
                var vIdx = (int)(BaseVertexIndex + MinIndex + v);

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

            var tris = new List<Int32>();

            for (int i = 0; i < PrimitiveCount; i++)
            {
                var idx = StartIndex;
                var vIdx = BaseVertexIndex;

                int i0, i1, i2;

                if (PrimitiveType == PrimitiveType.TriangleStrip)
                {
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
                            tris.Add((int)(i2 - MinIndex));
                            tris.Add((int)(i1 - MinIndex));
                            tris.Add((int)(i0 - MinIndex));
                        }
                        else
                        {
                            tris.Add((int)(i0 - MinIndex));
                            tris.Add((int)(i1 - MinIndex));
                            tris.Add((int)(i2 - MinIndex));
                        }
                    }
                }
                else if (PrimitiveType == PrimitiveType.TriangleList)
                {
                    DSC.Log("Loading a triangle list primitive!");

                    i0 = indices[idx + i];
                    i1 = indices[idx + (i + 1)];
                    i2 = indices[idx + (i + 2)];

                    if (swapOrder)
                    {
                        tris.Add((int)(i2 - MinIndex));
                        tris.Add((int)(i1 - MinIndex));
                        tris.Add((int)(i0 - MinIndex));
                    }
                    else
                    {
                        tris.Add((int)(i0 - MinIndex));
                        tris.Add((int)(i1 - MinIndex));
                        tris.Add((int)(i2 - MinIndex));
                    }
                }
                else
                {
                    throw new Exception("Unknown primitive type!");
                }
            }

            return tris;
        }

        public MeshDefinition(ModelPackage modelPackage)
        {
            ModelPackage = modelPackage;
        }
    }
}