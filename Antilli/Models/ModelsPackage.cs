using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using DSCript;

namespace Antilli.Models
{
    public class ModelsPackage
    {
        public BlockData BlockData { get; set; }

        public const uint Magic = 0x6;

        public PackageType Type { get; private set; }

        public List<PartsGroup> Parts { get; set; }
        public List<MeshGroup> MeshGroups { get; set; }
        public List<IndexedPrimitive> Meshes { get; set; }

        public VertexData Vertices { get; set; }
        public IndexData Indices { get; set; }

        public Mesh CreateIndexedPrimitive(IndexedPrimitive mesh, bool useBlendWeights)
        {
            List<Vertex> vertices = new List<Vertex>(mesh.NumVertices);
            List<TriangleFace> faces = new List<TriangleFace>();

            for (int v = 0; v <= mesh.NumVertices; v++)
            {
                Vertex vertex = Vertices[v + mesh.BaseVertexIndex + mesh.MinIndex].Copy();

                if (useBlendWeights)
                {
                    vertex.Position.X += vertex.BlendWeights.X * 1.0;
                    vertex.Position.Y += vertex.BlendWeights.Y * 1.0;
                    vertex.Position.Z += vertex.BlendWeights.Z * 1.0;
                }
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

        public void LoadModelPackage()
        {
            using (BlockEditor blockEditor = new BlockEditor(BlockData))
            using (BinaryReader f = new BinaryReader(blockEditor.Stream))
            {
                if (f.ReadUInt32() != Magic)
                    throw new Exception("Bad magic, cannot load ModelPackage!");

                uint type = f.ReadUInt32();

                Type = Enum.IsDefined((typeof(PackageType)), type) ? (PackageType)type : PackageType.Unknown;

                int nParts = f.ReadInt32();
                uint partsOffset = f.ReadUInt32();

                int nMeshGroups = f.ReadInt32();
                uint meshGroupsOffset = f.ReadUInt32();

                int nMeshes = f.ReadInt32();
                uint meshesOffset = f.ReadUInt32();

                if (f.ReadUInt16() != (uint)Type)
                    throw new Exception("Magic check failed, cannot load ModelPackage!");

                // Skip junk
                f.Seek(0x28, SeekOrigin.Begin);

                uint ddsOffset = f.ReadUInt32();
                uint pcmpOffset = f.ReadUInt32();

                int nIndices = f.ReadInt32();
                uint indicesSize = f.ReadUInt32();
                uint indicesOffset = f.ReadUInt32();

                if (f.ReadUInt32() != 0x1)
                    DSC.Log("Unknown face type check failed, errors may occur.");

                uint fvfOffset = f.ReadUInt32();

                /* ------------------------------
                 * Read vertex header
                 * ------------------------------ */
                f.Seek(fvfOffset, SeekOrigin.Begin);

                int nVerts = f.ReadInt32();
                uint vertsSize = f.ReadUInt32();
                uint vertsOffset = f.ReadUInt32();
                int vertLength = f.ReadInt32();

                /* ------------------------------
                 * Read indices
                 * ------------------------------ */
                f.Seek(indicesOffset, SeekOrigin.Begin);

                Indices = new IndexData(nIndices);

                for (int i = 0; i < nIndices; i++)
                    Indices.Buffer[i] = f.ReadUInt16();

                DSC.Log("Finished reading {0} index entries.", nIndices);

                /* ------------------------------
                 * Read vertices
                 * ------------------------------ */
                f.Seek(vertsOffset, SeekOrigin.Begin);

                Vertices = new VertexData(nVerts, vertLength);

                for (int i = 0; i < nVerts; i++)
                    Vertices.Buffer[i] = new Vertex(f.ReadBytes(vertLength), Vertices.VertexType);

                DSC.Log("Finished reading {0} vertex entries.", nVerts);

                // To collect the data for our meshes, we will read everything backwards:
                // - 1) Meshes
                // - 2) MeshGroups
                // - 3) PartsGroups
                //
                // This will help prevent redundant loops, and everything is read once, not twice!
                
                /* ------------------------------
                 * Read meshes
                 * ------------------------------ */
                f.Seek(meshesOffset, SeekOrigin.Begin);

                Meshes = new List<IndexedPrimitive>(nMeshes);

                for (int i = 0; i < nMeshes; i++)
                {
                    uint offset = (uint)f.GetPosition();

                    IndexedPrimitive mesh = new IndexedPrimitive() {
                        Offset = offset
                    };

                    Meshes.Add(mesh);

                    mesh.PrimitiveType = (D3DPRIMITIVETYPE)f.ReadInt32();

                    mesh.BaseVertexIndex = f.ReadInt32();
                    mesh.MinIndex = f.ReadInt32();
                    mesh.NumVertices = f.ReadInt32();

                    mesh.StartIndex = f.ReadInt32();
                    mesh.PrimitiveCount = f.ReadInt32();

                    // skip padding
                    f.Seek(0x18, SeekOrigin.Current);

                    mesh.MaterialId = f.ReadInt16();
                    mesh.TextureFlag = f.ReadInt16();

                    //skip padding
                    f.Seek(0x4, SeekOrigin.Current);
                }

                /* ------------------------------
                 * Read mesh groups
                 * ------------------------------ */
                f.Seek(meshGroupsOffset, SeekOrigin.Begin);

                MeshGroups = new List<MeshGroup>(nMeshGroups);

                for (int i = 0; i < nMeshGroups; i++)
                {
                    uint offset = (uint)f.GetPosition();
                    uint mOffset = f.ReadUInt32();

                    // skip padding
                    f.Seek(0x44, SeekOrigin.Current);

                    short count = f.ReadInt16();

                    // skip padding
                    f.Seek(0x2 + 0xC, SeekOrigin.Current);

                    MeshGroup mGroup = new MeshGroup(offset, count);

                    MeshGroups.Add(mGroup);

                    // Add meshes to group
                    for (int k = 0; k < count; k++)
                    {
                        int v = Meshes.FindIndex((m) => m.Offset == mOffset + (0x38 * k));

                        if (v == -1)
                            throw new Exception("An error occurred while trying to add a mesh to a group!");

                        IndexedPrimitive mesh = Meshes[v];
                        mesh.Group = mGroup;

                        mGroup.Meshes.Add(mesh);
                    }
                }

                /* ------------------------------
                 * Read parts groups
                 * ------------------------------ */
                f.Seek(partsOffset, SeekOrigin.Begin);

                Parts = new List<PartsGroup>(nParts);

                for (int i = 0; i < nParts; i++)
                {
                    PartsGroup group = new PartsGroup() {
                        UID = f.ReadUInt32(),
                        Handle = f.ReadUInt32()
                    };

                    Parts.Add(group);

                    // skip float padding
                    f.Seek(0x10, SeekOrigin.Current);

                    group.Unknown1 = f.ReadInt32();
                    group.Unknown2 = f.ReadInt32();

                    // skip padding + bigass float list
                    f.Seek(0x8 + 0x80, SeekOrigin.Current);

                    for (int k = 0; k < group.Parts.Capacity; k++)
                    {
                        PartsGroup.Entry entry = new PartsGroup.Entry(k);

                        group.Parts.Add(entry);

                        uint gOffset = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x4, SeekOrigin.Current);

                        entry.Unknown = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x8, SeekOrigin.Current);

                        entry.Reserved = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x8, SeekOrigin.Current);

                        entry.Parent = group;

                        if (gOffset != 0)
                        {
                            int v = MeshGroups.FindIndex((g) => g.Offset == gOffset);

                            if (v == -1)
                                throw new Exception("An error occurred while trying to add a Mesh group to a Part entry!");

                            entry.Group = MeshGroups[v];
                        }                        
                    }
                }

                //--Console.WriteLine("Done!");
            }
        }

        public ModelsPackage(BlockData blockData)
        {
            BlockData = blockData;

            LoadModelPackage();
        }
    }
}
