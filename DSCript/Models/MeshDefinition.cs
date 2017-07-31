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

        public VertexData VertexBuffer
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
        /// Member of the <see cref="D3DPRIMITIVETYPE"/> enumerated type, describing the type of primitive to render. D3DPT_POINTLIST is not supported with this method.
        /// </summary>
        public D3DPRIMITIVETYPE PrimitiveType { get; set; }

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

        public List<Vertex> GetVertices()
        {
            var vBuffer = VertexBuffer.Buffer;

            List<Vertex> vertices = new List<Vertex>((int)NumVertices);

            for (uint v = 0; v <= NumVertices; v++)
            {
                uint vIdx = (uint)BaseVertexIndex + MinIndex + v;

                if (vIdx >= vBuffer.Length)
                    break;

                vertices.Add(vBuffer[vIdx]);
            }

            return vertices;
        }

        public void GetVertices(Point3DCollection positions,
            Vector3DCollection normals,
            PointCollection coordinates)
        {
            int nVerts   = (int)NumVertices;

            if (positions == null)
                positions = new Point3DCollection(nVerts);
            if (normals == null)
                normals = new Vector3DCollection(nVerts);
            if (coordinates == null)
                coordinates = new PointCollection(nVerts);

            GetVertices(ref positions, ref normals, ref coordinates);
        }

        public void GetVertices(Point3DCollection positions,
            Vector3DCollection normals,
            PointCollection coordinates,
            Vector3DCollection blendWeights)
        {
            int nVerts   = (int)NumVertices;

            if (positions == null)
                positions = new Point3DCollection(nVerts);
            if (normals == null)
                normals = new Vector3DCollection(nVerts);
            if (coordinates == null)
                coordinates = new PointCollection(nVerts);
            if (blendWeights == null)
                blendWeights = new Vector3DCollection(nVerts);

            GetVertices(ref positions, ref normals, ref coordinates, ref blendWeights, true);
        }

        private void GetVertices(ref Point3DCollection positions,
            ref Vector3DCollection normals,
            ref PointCollection coordinates)
        {
            Vector3DCollection blendWeights = null;
            GetVertices(ref positions, ref normals, ref coordinates, ref blendWeights);
        }

        private void GetVertices(ref Point3DCollection positions,
            ref Vector3DCollection normals,
            ref PointCollection coordinates,
            ref Vector3DCollection blendWeights,
            bool getBlendWeights = false)
        {
            var vBuffer = VertexBuffer.Buffer;
            var nVerts = (int)NumVertices;

            for (uint v = 0; v <= NumVertices; v++)
            {
                uint vIdx = (uint)BaseVertexIndex + MinIndex + v;

                if (vIdx >= vBuffer.Length)
                    break;

                Vertex vertex = vBuffer[vIdx];

                positions.Add(vertex.Position);
                normals.Add(vertex.Normal);
                coordinates.Add(vertex.UV);

                if (getBlendWeights)
                    blendWeights.Add(vertex.BlendWeights);
            }
        }

        public Int32Collection GetTriangleIndices()
        {
            var indices = ModelPackage.IndexBuffer.Buffer;

            var tris = new Int32Collection();

            for (int i = 0; i < PrimitiveCount; i++)
            {
                var idx = StartIndex;
                var vIdx = BaseVertexIndex;

                int i0, i1, i2;

                if (PrimitiveType == D3DPRIMITIVETYPE.D3DPT_TRIANGLESTRIP)
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

                    // When reading in the vertices, the YZ-axis was flipped
                    // Therefore i0 and i2 need to be flipped for proper face orientation
                    // This was AFTER learning the hard way...
                    if ((i0 != i1) && (i0 != i2) && (i1 != i2))
                    {
                        tris.Add((int)(i2 - MinIndex));
                        tris.Add((int)(i1 - MinIndex));
                        tris.Add((int)(i0 - MinIndex));
                    }
                }
                else if (PrimitiveType == D3DPRIMITIVETYPE.D3DPT_TRIANGLELIST)
                {
                    DSCript.DSC.Log("Loading a triangle list primitive!");

                    i0 = indices[idx + i];
                    i1 = indices[idx + (i + 1)];
                    i2 = indices[idx + (i + 2)];

                    tris.Add((int)(i2 - MinIndex));
                    tris.Add((int)(i1 - MinIndex));
                    tris.Add((int)(i0 - MinIndex));
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