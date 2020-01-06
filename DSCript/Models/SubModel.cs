using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DSCript.Models
{
    public struct IndexedPrimitive
    {
        public PrimitiveType Type;

        public int VertexBaseOffset;

        public int VertexOffset;
        public int VertexCount;

        public int IndexOffset;
        public int IndexCount;

        public MaterialHandle Material;
    }

    public class SubModel
    {
        /// <summary>
        /// The <see cref="ModelPackage"/> this mesh belongs to.
        /// </summary>
        public ModelPackage ModelPackage { get; set; }
        
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

        public MaterialHandle Material;
        
        public List<int> CollectVertices(out List<int> tris)
        {
            tris = new List<int>();
            
            var indices = ModelPackage.IndexBuffer.Indices;

            // collect list of vertex indices
            var vertices = new List<int>();
            
            var lookup = new Dictionary<int, int>();
            var index = 0;

            if (PrimitiveType == PrimitiveType.TriangleFan)
            {
                var fans = new List<int>();
                var indexOffset = (IndexOffset / 2);

                var instance = LodInstance;
                var lod = instance.Parent;
                var model = lod.Parent;

                // collect vertices + fans
                for (int v = 0; v < VertexCount; v++)
                {
                    var offset = indexOffset + v;
                    var vIdx = (ushort)indices[offset];

                    if (!lookup.ContainsKey(vIdx))
                    {
                        vertices.Add(vIdx);
                        lookup.Add(vIdx, index++);
                    }

                    fans.Add(lookup[vIdx]);
                }

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
                        tris.Add(f0);
                        tris.Add(f1);
                        tris.Add(f2);
                    }
                }
            }
            else
            {
                for (int v = 0; v <= VertexCount; v++)
                {
                    var vIdx = (VertexBaseOffset + VertexOffset + v);

                    if (vIdx >= VertexBuffer.Count)
                        break;

                    vertices.Add(vIdx);
                }

                switch (PrimitiveType)
                {
                case PrimitiveType.TriangleList:
                    for (int i = 0; i < IndexCount; i++)
                    {
                        int i0, i1, i2;

                        var offset = IndexOffset + (i * 3);

                        i0 = indices[offset];
                        i1 = indices[offset + 1];
                        i2 = indices[offset + 2];

                        tris.Add(i0 - VertexOffset);
                        tris.Add(i1 - VertexOffset);
                        tris.Add(i2 - VertexOffset);
                    }
                    break;
                case PrimitiveType.TriangleStrip:
                    for (int i = 0; i < IndexCount; i++)
                    {
                        int i0, i1, i2;

                        var offset = (IndexOffset + i);

                        if ((i % 2) != 0)
                        {
                            i0 = indices[offset + 2];
                            i1 = indices[offset + 1];
                            i2 = indices[offset];
                        }
                        else
                        {
                            i0 = indices[offset];
                            i1 = indices[offset + 1];
                            i2 = indices[offset + 2];
                        }

                        if ((i0 != i1) && (i0 != i2) && (i1 != i2))
                        {
                            tris.Add(i0 - VertexOffset);
                            tris.Add(i1 - VertexOffset);
                            tris.Add(i2 - VertexOffset);
                        }
                    }
                    break;
                }
            }
            
            lookup.Clear();
            lookup = null;

            return vertices;
        }

        public List<Vertex> GetVertices(bool adjustVertices, ref List<int> indices)
        {
            var vBuffer = VertexBuffer;

            if (!vBuffer.HasPositions)
                return null;

#if !USE_OLD_VERTEX_CODE
            var tris = new List<int>();
            var vertices = CollectVertices(out tris);

            var result = new List<Vertex>();
            var index = 0;

            var lookup = new Dictionary<int, int>();

            var instance = LodInstance;
            var lod = instance.Parent;
            var model = lod.Parent;
            
            Vector3 scale = model.Scale;
            var useScale = (ModelPackage.Version == 1);

            var prims = new List<int>();
            
            // combine vertices
            for (int t = 0; t < tris.Count; t++)
            {
                var idx = tris[t];
                var vIdx = vertices[idx];

                if (!lookup.ContainsKey(vIdx))
                {
                    var vertex = VertexBuffer.Vertices[vIdx].ToVertex();

                    if (useScale)
                        vertex.ApplyScale(model.Scale);

                    if (adjustVertices)
                        vertex.FixDirection();

                    lookup.Add(vIdx, index++);
                    result.Add(vertex);
                }

                // append to buffer
                prims.Add(lookup[vIdx]);
            }
            
            indices.AddRange(prims);

            // clean up after ourselves
            lookup.Clear();
            lookup = null;

            return result;
#else
            var vertices = new List<Vertex>(VertexCount);
            
            switch (PrimitiveType)
            {
            case PrimitiveType.TriangleFan:
                var fans = new List<int>();

                var indexBuffer = ModelPackage.IndexBuffer.Buffer;
                var indexOffset = (IndexOffset / 2);

                var lookup = new Dictionary<int, int>();
                var index = 0;

                var instance = LodInstance;
                var lod = instance.Parent;
                var model = lod.Parent;

                Vector3 scale = model.Scale;
                var useScale = (ModelPackage.Version == 1);

                // collect vertices + fans
                for (int v = 0; v < VertexCount; v++)
                {
                    var offset = indexOffset + v;
                    var vIdx = (ushort)indexBuffer[offset];

                    if (!lookup.ContainsKey(vIdx))
                    {
                        var vertex = vBuffer.Vertices[vIdx].ToVertex();

                        if (useScale)
                            vertex.ApplyScale(scale);

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
#endif
        }

        public List<Vertex> GetVertices(bool adjustVertices = false)
        {
            var vBuffer = VertexBuffer;

            if (!vBuffer.HasPositions)
                return null;

            var vertices = new List<Vertex>(VertexCount);

            var instance = LodInstance;
            var lod = instance.Parent;
            var model = lod.Parent;

            Vector3 scale = model.Scale;
            var useScale = (ModelPackage.Version == 1);

            for (int v = 0; v <= VertexCount; v++)
            {
                var vIdx = (VertexBaseOffset + VertexOffset + v);

                if (vIdx >= vBuffer.Count)
                    break;

                var vertex = vBuffer.Vertices[vIdx].ToVertex();

                if (useScale)
                    vertex.ApplyScale(scale);

                if (adjustVertices)
                    vertex.FixDirection();
                
                vertices.Add(vertex);
            }

            return vertices;
        }
        
        public List<Int32> GetTriangleIndices(bool swapOrder = false)
        {
            var indices = ModelPackage.IndexBuffer.Indices;
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
        public SubModel(ModelPackage modelPackage)
        {
            ModelPackage = modelPackage;
        }
    }
}