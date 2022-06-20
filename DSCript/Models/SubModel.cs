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

    public class SubModel : ICopyCat<SubModel>
    {
        bool ICopyCat<SubModel>.CanCopy(CopyClassType copyType)                 => true;
        bool ICopyCat<SubModel>.CanCopyTo(SubModel obj, CopyClassType copyType) => true;

        bool ICopyCat<SubModel>.IsCopyOf(SubModel obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        SubModel ICopyClass<SubModel>.Copy(CopyClassType copyType)
        {
            var submodel = new SubModel();

            CopyTo(submodel, copyType);

            return submodel;
        }

        void ICopyClassTo<SubModel>.CopyTo(SubModel obj, CopyClassType copyType)
        {
            CopyTo(obj, copyType);
        }

        protected void CopyTo(SubModel obj, CopyClassType copyType)
        {
            obj.PrimitiveType = PrimitiveType;

            obj.VertexBaseOffset = VertexBaseOffset;
            obj.VertexOffset = VertexOffset;
            obj.VertexCount = VertexCount;

            obj.IndexOffset = IndexOffset;
            obj.IndexCount = IndexCount;

            obj.Material = Material;

            if (copyType == CopyClassType.DeepCopy)
            {
                throw new Exception("Can't deeply copy a SubModel due to poor design choices!");
            }
            else
            {
                // reuse all references
                obj.ModelPackage = ModelPackage;
                obj.Model = Model;
                obj.LodInstance = LodInstance;
            }
        }

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

#if SHITTY_REBASING
        public void RebaseTo(ModelPackage package,
                    Model model,
                    LodInstance instance,
                    VertexBuffer vertexBuffer, int vertexBaseOffset, ref int vertexOffset,
                    ref List<int> indexBuffer)
        {
            var indices = ModelPackage.IndexBuffer.Indices;

            var vertices = new List<int>();

            // collect vertex indices + build index buffer
            if (PrimitiveType == PrimitiveType.TriangleFan)
            {
                var lookup = new Dictionary<int, int>();
                var index = 0;

                var indexOffset = (IndexOffset / 2);

                // hopefully the new offset... :x
                IndexOffset = indexBuffer.Count * 2;

                // collect vertices + fans
                for (int v = 0; v < VertexCount; v++)
                {
                    var vIdx = (ushort)indices[indexOffset + v];

                    if (!lookup.ContainsKey(vIdx))
                    {
                        vertices.Add(vIdx);
                        lookup.Add(vIdx, index++);
                    }

                    indexBuffer.Add(vertexOffset + lookup[vIdx]);
                }

                // clean up our mess
                lookup.Clear();
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

                // save the offset so we can replace it afterwards
                var indexOffset = indexBuffer.Count;

                switch (PrimitiveType)
                {
                case PrimitiveType.TriangleList:
                case PrimitiveType.TriangleStrip:
                    for (int i = 0; i < IndexCount; i++)
                    {
                        var idx = indices[IndexOffset + i] - VertexOffset;

                        // add the rebased primitive
                        indexBuffer.Add(vertexOffset + idx);
                    }
                    break;
                default:
                    throw new Exception($"Unhandled primitive type '{PrimitiveType}' - cannot rebase submodel!");
                }

                // rebase index buffer
                IndexOffset = indexOffset;
            }

            // rebase vertex buffer
            VertexBaseOffset = vertexBaseOffset;
            VertexOffset = vertexOffset;
            VertexCount = vertices.Count; // we've probably duped some vertices, oh well... ;)

            // add vertices to new vertex buffer
            var decl = vertexBuffer.Declaration;
            var offset = 0;
            var stride = decl.SizeOf;
            var buffer = new byte[VertexCount * stride];

            foreach (var vIdx in vertices)
            {
                var vertex = VertexBuffer.Vertices[vIdx];

                Buffer.BlockCopy(vertex.Buffer, 0, buffer, offset, stride);
                offset += stride;
            }

            vertexBuffer.AddVertices(buffer, VertexCount);
            vertexOffset += VertexCount;

            // finally, reparent to new package/model/instance/etc
            ModelPackage = package;
            Model = model;
            LodInstance = instance;

            Debug.WriteLine("**** AFTER REBASE ****");
            Debug.WriteLine($"\tVertexBaseOffset: {VertexBaseOffset:X8}");
            Debug.WriteLine($"\tVertexOffset: {VertexOffset:X8}");
            Debug.WriteLine($"\tVertexCount: {VertexCount:X8}");
            Debug.WriteLine($"\tIndexOffset: {IndexOffset:X8}");
            Debug.WriteLine($"\tIndexCount: {IndexCount:X8}");
        }
#endif
        public List<int> CollectTriangleFans(ref Dictionary<int, int> lookup,
            ref List<int> vertices,
            ref short[] indices,
            ref int index,
            ref List<int> tris)
        {
            var fans = new List<int>();
            var indexOffset = (IndexOffset / 2);

            var instance = LodInstance;
            var lod = instance.Parent;
            var model = lod.Parent;
#if TRI_LOG
            var lines = new List<String>();

            lines.Add($"{VertexCount} fans, offset is {IndexOffset} (/ 2 = {indexOffset})");
#endif
            var lowestIndex = -1;
            var highestIndex = 0;

            // collect vertices + fans
            for (int v = 0; v < VertexCount; v++)
            {
                var offset = indexOffset + v;
                var vIdx = (ushort)indices[offset];

                if (!lookup.ContainsKey(vIdx))
                {
                    vertices.Add(vIdx);
                    lookup.Add(vIdx, index++);

                    if (lowestIndex == -1 || vIdx < lowestIndex)
                        lowestIndex = vIdx;
                    if (vIdx > highestIndex)
                        highestIndex = vIdx;
                }

                fans.Add(vIdx);
#if TRI_LOG
                lines.Add($"\t{v:D4}={vIdx:D4}: {lookup[vIdx]:D4}");
#endif
            }
#if TRI_LOG
            lines.Add("-----------------------------------------------------------------------------");
#endif
            var numVertices = (highestIndex - lowestIndex);
            var vertexOffset = 0;
#if TRI_LOG
            lines.Add($"{numVertices} vertices, offset is {lowestIndex}");
#endif
            var vertexLookup = new Dictionary<int, int>();

            for (int v = 0; v <= numVertices; v++)
            {
                var vIdx = lowestIndex + v;

                if (vIdx >= VertexBuffer.Count)
                    break;
#if TRI_LOG
                lines.Add($"\t{v:D4}={vIdx:D4}: {vertexOffset:D4}");
#endif
                vertexLookup.Add(vIdx, vertexOffset++);
            }
#if TRI_LOG
            lines.Add("-----------------------------------------------------------------------------");
#endif
            /*
             	0002: > 0000 0001 0002 : 0003 0000 0001 : TRIANGLE 0 : 10095 10092 10093
	            0003: < 0002 0002 0001 : 0001 0001 0000 : -
	            0004: > 0002 0002 0003 : 0001 0001 0002 : -
	            0005: < 0003 0003 0002 : 0002 0002 0001 : -
	            0006: > 0003 0003 0003 : 0002 0002 0002 : -
	            0007: < 0004 0003 0003 : 0005 0002 0002 : -
	            0008: > 0003 0004 0005 : 0002 0005 0004 : TRIANGLE 1 : 10094 10097 10096
	            0009: < 0005 0005 0004 : 0004 0004 0005 : -
	            0010: > 0005 0005 0006 : 0004 0004 0009 : -
	            0011: < 0006 0006 0005 : 0009 0009 0004 : -
	            0012: > 0006 0006 0006 : 0009 0009 0009 : -
	            0013: < 0007 0006 0006 : 0006 0009 0009 : -
	            0014: > 0006 0007 0008 : 0009 0006 0007 : TRIANGLE 2 : 10101 10098 10099
	            0015: < 0008 0008 0007 : 0007 0007 0006 : -
	            0016: > 0008 0008 0009 : 0007 0007 0008 : -
	            0017: < 0009 0009 0008 : 0008 0008 0007 : -
	            0018: > 0009 0009 0009 : 0008 0008 0008 : -
	            0019: < 0010 0009 0009 : 0011 0008 0008 : -
	            0020: > 0009 0010 0011 : 0008 0011 0010 : TRIANGLE 3 : 10100 10103 10102
            */
            for (int n = 2; n < fans.Count; n++)
            {
                int f0, f1, f2;
#if TRI_LOG
                var info = "";
                var kind = "";
#endif
                if ((n % 2) != 0)
                {
                    f0 = fans[n];
                    f1 = fans[n - 1];
                    f2 = fans[n - 2];
#if TRI_LOG
                    info = $"< {lookup[f0]:D4} {lookup[f1]:D4} {lookup[f2]:D4} : ";
                    info += $"{vertexLookup[f0]:D4} {vertexLookup[f1]:D4} {vertexLookup[f2]:D4} : ";
#endif
                }
                else
                {
                    f0 = fans[n - 2];
                    f1 = fans[n - 1];
                    f2 = fans[n];
#if TRI_LOG
                    info = $"> {lookup[f0]:D4} {lookup[f1]:D4} {lookup[f2]:D4} : ";
                    info += $"{vertexLookup[f0]:D4} {vertexLookup[f1]:D4} {vertexLookup[f2]:D4} : ";
#endif
                }

                if ((f0 != f1) && (f0 != f2) && (f1 != f2))
                {
#if TRI_LOG
                    kind = $"TRIANGLE {tris.Count / 3} : {f0:D4} {f1:D4} {f2:D4}";
#endif
                    tris.Add(lookup[f0]);
                    tris.Add(lookup[f1]);
                    tris.Add(lookup[f2]);
                }
#if TRI_LOG
                else
                {
                    kind = "DEGENERATE";
                }

                lines.Add($"\t{n:D4}: {info}{kind}");
#endif
            }
#if TRI_LOG
            lines.Add("-----------------------------------------------------------------------------");
            File.AppendAllLines("tri_fans.log", lines, Encoding.UTF8);
#endif

            return fans;
        }

        public List<int> CollectVertexTris(ref Dictionary<int, int> luVerts, ref List<int> vertices, short[] indices, out int numVertices)
        {
            var tris = new List<int>();
            var index = vertices.Count;

            // collect list of vertex indices
            if (PrimitiveType == PrimitiveType.TriangleFan)
            {
                var fans = new List<int>();
                var indexOffset = (IndexOffset / 2);

                // collect vertices + fans
                for (int v = 0; v < VertexCount; v++)
                {
                    var offset = indexOffset + v;
                    var vIdx = (ushort)indices[offset];

                    if (!luVerts.ContainsKey(vIdx))
                    {
                        vertices.Add(vIdx);
                        luVerts.Add(vIdx, index++);
                    }

                    fans.Add(vIdx);
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
                        tris.Add(luVerts[f0]);
                        tris.Add(luVerts[f1]);
                        tris.Add(luVerts[f2]);
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

                    if (!luVerts.ContainsKey(vIdx))
                    {
                        vertices.Add(vIdx);
                        luVerts.Add(vIdx, index++);
                    }
                }

                switch (PrimitiveType)
                {
                case PrimitiveType.TriangleList:
                    for (int i = 0; i < IndexCount; i++)
                    {
                        int i0, i1, i2;

                        var offset = IndexOffset + (i * 3);

#if LOOKUP_V1
                        i0 = indices[offset] - VertexOffset;
                        i1 = indices[offset + 1] - VertexOffset;
                        i2 = indices[offset + 2] - VertexOffset;

                        tris.Add(luVerts[i0]);
                        tris.Add(luVerts[i1]);
                        tris.Add(luVerts[i2]);
#else
                        i0 = indices[offset];
                        i1 = indices[offset + 1];
                        i2 = indices[offset + 2];

                        tris.Add(luVerts[i0]);
                        tris.Add(luVerts[i1]);
                        tris.Add(luVerts[i2]);
#endif
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

#if LOOKUP_V1
                        i0 -= VertexOffset;
                        i1 -= VertexOffset;
                        i2 -= VertexOffset;

                        if ((i0 != i1) && (i0 != i2) && (i1 != i2))
                        {
                            tris.Add(luVerts[i0]);
                            tris.Add(luVerts[i1]);
                            tris.Add(luVerts[i2]);
                        }
#else
                        if ((i0 != i1) && (i0 != i2) && (i1 != i2))
                        {
                            tris.Add(luVerts[i0]);
                            tris.Add(luVerts[i1]);
                            tris.Add(luVerts[i2]);
                        }
#endif
                    }
                    break;
                }
            }

            var lowestIndex = -1;
            var highestIndex = 0;

            foreach (var tri in tris)
            {
                var vIdx = vertices[tri];

                if (lowestIndex == -1 || vIdx < lowestIndex)
                    lowestIndex = vIdx;
                if (vIdx > highestIndex)
                    highestIndex = vIdx;
            }

            numVertices = (highestIndex - lowestIndex) + 1;

            Debug.WriteLine($"Lowest index {lowestIndex}, highest index {highestIndex} : {numVertices} vertices.");

            return tris;
        }

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
#if TRI_FANS_NORMAL
                var fans = CollectTriangleFans(ref lookup, ref vertices, ref indices, ref index, ref tris);
#else
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
#endif
            }
            else
            {
#if TRI_LOG
                var lines = new List<String>();

                lines.Add($"{VertexCount} vertices, offset is {VertexOffset}");
#endif
                for (int v = 0; v <= VertexCount; v++)
                {
                    var vIdx = (VertexBaseOffset + VertexOffset + v);

                    if (vIdx >= VertexBuffer.Count)
                        break;
#if TRI_LOG
                    lines.Add($"\t{v:D4}={vIdx:D4}: {vertices.Count:D4}");
#endif
                    vertices.Add(vIdx);
                }
#if TRI_LOG
                lines.Add("-----------------------------------------------------------------------------");
#endif
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
#if TRI_LOG
                        lines.Add($"\t{i:D4}: < {i0-VertexOffset:D4} {i1-VertexOffset:D4} {i2-VertexOffset:D4} : TRIANGLE {tris.Count / 3}");
#endif
                        tris.Add(i0 - VertexOffset);
                        tris.Add(i1 - VertexOffset);
                        tris.Add(i2 - VertexOffset);
                    }
#if TRI_LOG
                    lines.Add("-----------------------------------------------------------------------------");
                    File.AppendAllLines("tri_lists.log", lines, Encoding.UTF8);
#endif
                    break;
                case PrimitiveType.TriangleStrip:
                    for (int i = 0; i < IndexCount; i++)
                    {
                        int i0, i1, i2;

                        var offset = (IndexOffset + i);

#if TRI_LOG
                        var info = "";
                        var kind = "";
#endif
                        if ((i % 2) != 0)
                        {
                            i0 = indices[offset + 2];
                            i1 = indices[offset + 1];
                            i2 = indices[offset];
#if TRI_LOG
                            info = $"< {i0-VertexOffset:D4} {i1-VertexOffset:D4} {i2-VertexOffset:D4} : ";
#endif
                        }
                        else
                        {
                            i0 = indices[offset];
                            i1 = indices[offset + 1];
                            i2 = indices[offset + 2];
#if TRI_LOG
                            info = $"> {i2-VertexOffset:D4} {i1-VertexOffset:D4} {i0-VertexOffset:D4} : ";
#endif
                        }

                        if ((i0 != i1) && (i0 != i2) && (i1 != i2))
                        {
#if TRI_LOG
                            kind = $"TRIANGLE {tris.Count / 3}";
#endif
                            tris.Add(i0 - VertexOffset);
                            tris.Add(i1 - VertexOffset);
                            tris.Add(i2 - VertexOffset);
                        }
#if TRI_LOG
                        else
                        {
                            kind = "DEGENERATE";
                        }

                        lines.Add($"\t{i:D4}: {info}{kind}");
#endif
                    }
#if TRI_LOG
                    lines.Add("-----------------------------------------------------------------------------");

                    File.AppendAllLines("tri_strips.log", lines, Encoding.UTF8);
#endif
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