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

namespace DSCript.Models
{
    /* ############################################################
     * THIS CLASS NEEDS A TOTAL REWRITE, DO NOT USE
     * ############################################################ */
    [Obsolete("This class is in need of a complete rewrite, do not use!", true)]
    public class ModelPackagePC_X : ModelPackagePC
    {
        protected override void Load()
        {
            using (var f = Spooler.GetMemoryStream())
            {
                if (f.ReadInt32() != 1)
                    throw new Exception("Bad magic, cannot load ModelPackage!");

                UID = f.ReadInt32();

                var nParts = f.ReadInt32();
                var partsOffset = f.ReadUInt32();

                var nMeshGroups = f.ReadInt32();
                var meshGroupsOffset = f.ReadUInt32();

                var nMeshes = f.ReadInt32();
                var meshesOffset = f.ReadUInt32();

                // Skip padding
                f.Seek(0x8, SeekOrigin.Current);

                var ddsOffset = f.ReadUInt32();
                var pcmpOffset = f.ReadUInt32();

                var nIndices = f.ReadInt32();
                var indicesSize = f.ReadUInt32();
                var indicesOffset = f.ReadUInt32();
                
                var unkFaceType = f.ReadUInt32();

                /*if (unkFaceType != 0x1)
                    DSC.Log("Unknown face type check failed, errors may occur.");*/

                uint fvfOffset = f.ReadUInt32();

                /* ------------------------------
                 * Read vertex header
                 * ------------------------------ */
                f.Seek(fvfOffset, SeekOrigin.Begin);

                int nVerts = f.ReadInt32();
                uint vertsSize = f.ReadUInt32();
                uint vertsOffset = f.ReadUInt32();
                int vertLength = f.ReadInt32();

                uint unkFVF1 = f.ReadUInt32();
                uint unkFVF2 = f.ReadUInt32();
                uint unkFVF3 = f.ReadUInt32();
                uint unkFVF4 = f.ReadUInt32();

                /* ------------------------------
                 * Read indices
                 * ------------------------------ */
                f.Seek(indicesOffset, SeekOrigin.Begin);

                Indices = new IndexData(nIndices);

                for (int i = 0; i < nIndices; i++)
                    Indices.Buffer[i] = f.ReadUInt16();

                //--DSC.Log("Finished reading {0} index entries.", nIndices);

                /* ------------------------------
                    * Read vertices
                    * ------------------------------ */
                f.Seek(vertsOffset, SeekOrigin.Begin);

                Vertices = new VertexData(nVerts, vertLength);

                for (int i = 0; i < nVerts; i++)
                    Vertices.Buffer[i] = new Vertex(f.ReadBytes(vertLength), Vertices.VertexType);

                //--DSC.Log("Finished reading {0} vertex entries.", nVerts);

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

                Meshes = new List<IndexedMesh>(nMeshes);

                for (int i = 0; i < nMeshes; i++)
                {
                    uint offset = (uint)f.GetPosition();

                    IndexedMesh mesh = new IndexedMesh();

                    Meshes.Add(mesh);

                    mesh.PrimitiveType = (D3DPRIMITIVETYPE)f.ReadInt32();

                    int unkIndex = f.ReadInt32();

                    f.Seek(0x4, SeekOrigin.Current);

                    mesh.PrimitiveCount = f.ReadUInt32();

                    uint indexOffset = f.ReadUInt32();
                    mesh.StartIndex = (indexOffset != 0) ? ((indexOffset / 2) - 1) : 0;

                    mesh.MinIndex = 0;

                    mesh.NumVertices = 0;
                    mesh.BaseVertexIndex = 0;

                    // skip padding
                    f.Seek(0x1C, SeekOrigin.Current);

                    mesh.MaterialId = f.ReadInt16();
                    mesh.SourceUID = f.ReadUInt16();

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
                    f.Seek(0x40, SeekOrigin.Current);

                    short count = f.ReadInt16();

                    // skip padding
                    f.Seek(0x2 + 0x6, SeekOrigin.Current);

                    MeshGroup mGroup = new MeshGroup(offset, count);
                    MeshGroups.Add(mGroup);

                    // Add meshes to group
                    for (int k = 0; k < count; k++)
                    {
                        int v = Meshes.FindIndex((m) => m.Offset == mOffset + (0x38 * k));

                        if (v == -1)
                            throw new Exception("An error occurred while trying to add a mesh to a group!");

                        IndexedMesh mesh = Meshes[v];
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
                    f.Seek(0x4 + 0x80, SeekOrigin.Current);

                    for (int k = 0; k < group.Parts.Capacity; k++)
                    {
                        PartDefinition entry = new PartDefinition(k);

                        group.Parts.Add(entry);

                        uint gOffset = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x8, SeekOrigin.Current);

                        entry.Unknown = f.ReadUInt32();
                        entry.Reserved = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x4, SeekOrigin.Current);

                        entry.Parent = group;

                        if (gOffset != 0)
                        {
                            int v = MeshGroups.FindIndex((g) => g.Offset == gOffset);

                            if (v == -1)
                                throw new Exception("An error occurred while trying to add a Mesh group to a Part entry!");

                            MeshGroup mGroup = MeshGroups[v];

                            entry.Group = mGroup;
                            mGroup.Parent = entry;
                        }
                    }
                }
            }
        }

        public ModelPackagePC_X(BlockData blockData)
            : base(blockData)
        {

        }
    }
}
