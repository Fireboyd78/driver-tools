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
using System.Windows.Media.Media3D;

namespace DSCript.Models
{
    public class ModelPackagePC : ModelPackage
    {
        public override uint Magic
        {
            get { return 6; }
        }

        public override void Load()
        {
            using (BlockEditor blockEditor = new BlockEditor(BlockData))
            {
                BinaryReader f = blockEditor.Reader;

                if (blockEditor.BlockData.Block.Reserved != 6)
                    throw new Exception("Bad MDPC version, cannot load ModelPackage!");
                if (f.ReadUInt32() != Magic)
                    throw new Exception("Bad magic, cannot load ModelPackage!");

                UID                     = f.ReadUInt32();

                int nParts              = f.ReadInt32();
                uint partsOffset        = f.ReadUInt32();

                int nMeshGroups         = f.ReadInt32();
                uint meshGroupsOffset   = f.ReadUInt32();

                int nMeshes             = f.ReadInt32();
                uint meshesOffset       = f.ReadUInt32();

                uint uid2               = f.ReadUInt16();

                if (uid2 != UID)
                    DSC.Log("Unknown magic check failed - wanted {0}, got {1}", UID, uid2);

                // Skip junk
                f.Seek(0x28, SeekOrigin.Begin);

                uint ddsOffset          = f.ReadUInt32();
                uint pcmpOffset         = f.ReadUInt32();

                int nIndices            = f.ReadInt32();
                uint indicesSize        = f.ReadUInt32();
                uint indicesOffset      = f.ReadUInt32();

                int numVertexBuffers    = f.ReadInt32();

                uint fvfOffset          = f.ReadUInt32();

                // skip packages with no models
                if (PackageType == PackageType.VehicleGlobals || fvfOffset == 0)
                    goto LoadPCMP;

                VertexBuffers = new List<VertexData>(numVertexBuffers);

                /* ------------------------------
                 * Read vertex buffer header(s) (Size: 0x1C)
                 * ------------------------------ */
                for (int vB = 0; vB < numVertexBuffers; vB++)
                {
                    f.SeekFromOrigin(fvfOffset, (vB * 0x1C));

                    int nVerts              = f.ReadInt32();
                    uint vertsSize          = f.ReadUInt32();
                    uint vertsOffset        = f.ReadUInt32();
                    int vertLength          = f.ReadInt32();

                    VertexData vertexBuffer = new VertexData(nVerts, vertLength);

                    VertexBuffers.Add(vertexBuffer);

                    /* ------------------------------
                     * Read vertices in buffer
                     * ------------------------------ */
                    f.Seek(vertsOffset, SeekOrigin.Begin);

                    for (int i = 0; i < nVerts; i++)
                        vertexBuffer.Buffer[i] = new Vertex(f.ReadBytes(vertLength), vertexBuffer.VertexType);
                }

                /* ------------------------------
                 * Read index buffer
                 * ------------------------------ */
                f.Seek(indicesOffset, SeekOrigin.Begin);

                Indices = new IndexData(nIndices);

                for (int i = 0; i < nIndices; i++)
                    Indices.Buffer[i] = f.ReadUInt16();

                /* ------------------------------
                 * Read model data
                 * ------------------------------ */
                var meshes = new Dictionary<uint, IndexedMesh>(nMeshes);
                var groups = new Dictionary<uint, MeshGroup>(nMeshGroups);

                Meshes = new List<IndexedMesh>(nMeshes);
                MeshGroups = new List<MeshGroup>(nMeshGroups);
                Parts = new List<PartsGroup>(nParts);

                // To collect the data for our meshes, we will read everything backwards:
                // - 1) Meshes
                // - 2) MeshGroups
                // - 3) PartsGroups
                //
                // This will help prevent redundant loops, and everything is read once, not twice!

                /* ------------------------------
                 * Read meshes (Size: 0x38)
                 * ------------------------------ */
                f.Seek(meshesOffset, SeekOrigin.Begin);
                for (int i = 0; i < nMeshes; i++)
                {
                    uint offset = (uint)(f.SeekFromOrigin(meshesOffset, (i * 0x38)));

                    IndexedMesh mesh        = new IndexedMesh(this);
                    Meshes.Add(mesh);

                    // add to mesh lookup
                    meshes.Add(offset, mesh);

                    mesh.PrimitiveType      = (D3DPRIMITIVETYPE)f.ReadInt32();

                    mesh.BaseVertexIndex    = f.ReadInt32();
                    mesh.MinIndex           = f.ReadUInt32();
                    mesh.NumVertices        = f.ReadUInt32();

                    mesh.StartIndex         = f.ReadUInt32();
                    mesh.PrimitiveCount     = f.ReadUInt32();

                    // skip padding
                    f.Seek(0x18, SeekOrigin.Current);

                    mesh.MaterialId         = f.ReadInt16();
                    mesh.SourceUID          = f.ReadUInt16();
                }
                /* ------------------------------
                 * Read mesh groups (Size: 0x58)
                 * ------------------------------ */
                for (int i = 0; i < nMeshGroups; i++)
                {
                    uint offset = (uint)(f.SeekFromOrigin(meshGroupsOffset, (i * 0x58)));
                    uint mOffset = f.ReadUInt32();

                    // skip padding
                    f.Seek(0x44, SeekOrigin.Current);

                    short count = f.ReadInt16();

                    MeshGroup group = new MeshGroup(count);
                    MeshGroups.Add(group);

                    // add to mesh groups lookup
                    groups.Add(offset, group);

                    // Add meshes to group
                    for (uint k = 0; k < count; k++)
                    {
                        IndexedMesh mesh = meshes[mOffset + (k * 0x38)];
                        mesh.MeshGroup = group;

                        group.Meshes.Add(mesh);
                    }
                }
                /* ------------------------------
                 * Read parts groups (Size: 0x188)
                 * ------------------------------ */
                for (int i = 0; i < nParts; i++)
                {
                    f.SeekFromOrigin(partsOffset, (i * 0x188));

                    PartsGroup part = new PartsGroup() {
                        UID = f.ReadUInt32(),
                        Handle = f.ReadUInt32()
                    };

                    Parts.Add(part);

                    // skip unknown float padding
                    f.Seek(0x10, SeekOrigin.Current);

                    // INCOMING TRANSMISSION...
                    // RE: OPERATION S.T.E.R.N....
                    // ...
                    // YOUR ASSISTANCE HAS BEEN NOTED...
                    // ...
                    // <END OF TRANSMISSION>...
                    part.VertexBufferId = f.ReadInt16();

                    part.Unknown1 = f.ReadInt16();
                    part.Unknown2 = f.ReadInt32();

                    // skip padding
                    f.Seek(0x8, SeekOrigin.Current);

                    // read unknown list of 8 Point4Ds
                    for (int t = 0; t < 8; t++)
                        part.Transform.Add(new Point4D(
                            (double)f.ReadSingle(),
                            (double)f.ReadSingle(),
                            (double)f.ReadSingle(),
                            (double)f.ReadSingle()
                        ));

                    // there are 7 part definitions per group
                    for (int k = 0; k < 7; k++)
                    {
                        PartDefinition entry = new PartDefinition(k) {
                            Parent = part
                        };

                        part.Parts.Add(entry);

                        uint gOffset = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x4, SeekOrigin.Current);

                        entry.Unknown = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x8, SeekOrigin.Current);

                        entry.Reserved = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x8, SeekOrigin.Current);

                        if (gOffset != 0)
                        {
                            MeshGroup mGroup = groups[gOffset];

                            entry.Group = mGroup;
                            mGroup.Parent = entry;

                            // TODO: Not have such ugly code!
                            foreach (IndexedMesh mesh in mGroup.Meshes)
                                mesh.PartsGroup = entry.Parent;
                        }
                    }
                }

                // lookup tables no longer needed
                meshes.Clear();
                groups.Clear();

                goto LoadPCMP;

                // Read PCMP
            LoadPCMP:
                if (pcmpOffset == 0)
                    return;

                f.Seek(pcmpOffset, SeekOrigin.Begin);

                if (f.ReadUInt32() != PCMPData.Magic)
                    throw new Exception("Bad textures magic, cannot load ModelPackage!");

                if (f.ReadUInt32() != 0x3)
                    DSC.Log("PCMP version check failed, errors may occur.");

                int nMats               = f.ReadInt32();
                uint matsOffset         = f.ReadUInt32();

                int table1Count         = f.ReadInt32();
                uint table1Offset       = f.ReadUInt32();

                int nSubMats            = f.ReadInt32();
                uint subMatsOffset      = f.ReadUInt32();

                int table2Count         = f.ReadInt32();
                uint table2Offset       = f.ReadUInt32();

                int DDSInfoCount        = f.ReadInt32();
                uint DDSInfoOffset      = f.ReadUInt32();

                uint DDSOffset          = f.ReadUInt32();
                uint Size               = f.ReadUInt32();

                MaterialData = new PCMPData();

                var textures = new Dictionary<uint, PCMPTexture>(DDSInfoCount);
                var subMaterials = new Dictionary<uint, PCMPSubMaterial>(nSubMats);

                // Read backwards

                // Textures (Size: 0x20)
                for (int t = 0; t < DDSInfoCount; t++)
                {
                    uint baseOffset = (uint)(f.SeekFromOrigin(pcmpOffset, (DDSInfoOffset + (t * 0x20)))) - pcmpOffset;
                    
                    PCMPTexture textureInfo = new PCMPTexture();

                    MaterialData.Textures.Add(textureInfo);

                    //add to texture lookup
                    textures.Add(baseOffset, textureInfo);
                    
                    textureInfo.Unk1    = f.ReadByte();
                    textureInfo.Unk2    = f.ReadByte();
                    textureInfo.Unk3    = f.ReadByte();
                    textureInfo.Unk4    = f.ReadByte();

                    textureInfo.CRC32   = f.ReadUInt32();

                    uint offset         = f.ReadUInt32();
                    int size            = f.ReadInt32();

                    textureInfo.Type    = f.ReadUInt32();

                    textureInfo.Width   = f.ReadUInt16();
                    textureInfo.Height  = f.ReadUInt16();

                    textureInfo.Unk5    = f.ReadUInt32();
                    textureInfo.Unk6    = f.ReadUInt32();

                    // get DDS from absolute offset (defined in MDPC header)
                    f.SeekFromOrigin(ddsOffset, offset);
                    textureInfo.Buffer  = f.ReadBytes(size);
                }

                // Submaterials (Size: 0x20)
                for (int s = 0; s < nSubMats; s++)
                {
                    uint baseOffset = (uint)(f.SeekFromOrigin(pcmpOffset, (subMatsOffset + (s * 0x20)))) - pcmpOffset;

                    PCMPSubMaterial subMaterial = new PCMPSubMaterial() {
                        Flags = f.ReadUInt32(),
                        Mode = f.ReadUInt16(),
                        Type = f.ReadUInt16()
                    };

                    MaterialData.SubMaterials.Add(subMaterial);

                    //add to submaterial lookup
                    subMaterials.Add(baseOffset, subMaterial);

                    f.Seek(0x8, SeekOrigin.Current);

                    // table info
                    uint offset = f.ReadUInt32();
                    uint count = f.ReadUInt32();

                    // get texture from table
                    for (int t = 0; t < count; t++)
                    {
                        f.SeekFromOrigin(pcmpOffset, (offset + (t * 0x8)));
                        subMaterial.Textures.Add(textures[f.ReadUInt32()]);
                    }
                }

                // Materials (Size: 0x18)
                for (int m = 0; m < nMats; m++)
                {
                    f.SeekFromOrigin(pcmpOffset, (matsOffset + (m * 0x18)));

                    // table info
                    uint offset = f.ReadUInt32();
                    uint count = f.ReadUInt32();

                    PCMPMaterial material = new PCMPMaterial() {
                        Reserved1 = f.ReadUInt32(),
                        Reserved2 = f.ReadUInt32(),
                        Reserved3 = f.ReadUInt32(),
                        Reserved4 = f.ReadUInt32()
                    };

                    MaterialData.Materials.Add(material);

                    // get submaterial from table
                    for (int s = 0; s < count; s++)
                    {
                        f.SeekFromOrigin(pcmpOffset, (offset + (s * 0x8)));
                        material.SubMaterials.Add(subMaterials[f.ReadUInt32()]);
                    }
                }

                // lookup tables no longer needed
                textures.Clear();
                subMaterials.Clear();
            }
        }

        public override void Compile()
        {
            throw new Exception("Needs to be rewritten first!");

            /*
            int bufSize             = 0;

            int nParts              = 0;
            int nGroups             = 0;
            int nMeshes             = 0;
            int nIndices            = 0;
            int nVertices           = 0;

            int partsOffset         = 0x80; // haven't seen any different values
            int groupsOffset        = 0;
            int meshesOffset        = 0;
            int ddsOffset           = 0;
            int pcmpOffset          = 0;
            int indicesOffset       = 0;
            int fvfOffset           = 0;
            int vertsOffset         = 0;
            int ddsOffset2          = 0;
            int materialsOffset     = 0;
            int subMatTableOffset   = 0;
            int subMaterialsOffset  = 0;
            int texInfoTableOffset  = 0;
            int texInfoOffset       = 0;

            int vertLength          = 0;
            int vertsSize           = 0;

            int indicesSize         = 0;

            bool writeModels        = (Parts != null && VertexBuffers.Buffer != null && Indices.Buffer != null);

            // sections aligned 128 bytes
            // Size of header
            bufSize += 0x44;
            bufSize += bufSize.Align(128);

            if (!writeModels)
            {
                bufSize += bufSize.Align(4096);
            }
            else
            {
                nParts = Parts.Count;
                nGroups = MeshGroups.Count;
                nMeshes = Meshes.Count;

                nIndices = Indices.Buffer.Length;
                nVertices = VertexBuffers.Buffer.Length;

                vertLength = (int)VertexBuffers.VertexType;
                vertsSize = vertLength * nVertices;

                indicesSize = nIndices * 2;

                partsOffset = bufSize;

                // Add up size of parts groups
                bufSize += nParts * 0x188;
                bufSize += bufSize.Align(128);

                groupsOffset = bufSize;

                // Add up size of mesh groups
                bufSize += nGroups * 0x58;
                meshesOffset = bufSize;

                // Add up size of mesh definitions
                bufSize += nMeshes * 0x38;
                bufSize += bufSize.Align(128);

                fvfOffset = bufSize;

                // Add up size of FVF data
                bufSize += 0x1C;

                indicesOffset = bufSize;
                bufSize += indicesSize;

                bufSize += bufSize.Align(4096);

                vertsOffset = bufSize;
                bufSize += vertsSize;

                bufSize += bufSize.Align(4096);
            }

            // -- PCMP -- \\

            int nMaterials      = MaterialData.Materials.Count;
            int nSubMaterials   = MaterialData.SubMaterials.Count;
            int nTextures       = MaterialData.Textures.Count;

            pcmpOffset = bufSize;

            // Size of header
            bufSize += 0x38;

            materialsOffset = (bufSize - pcmpOffset);
            bufSize += (int)(nMaterials * 0x18);

            subMatTableOffset = (bufSize - pcmpOffset);
            bufSize += (int)(nSubMaterials * 0x8);

            subMaterialsOffset = (bufSize - pcmpOffset);
            bufSize += (int)(nSubMaterials * 0x20);

            texInfoTableOffset = (bufSize - pcmpOffset);
            bufSize += (int)(nTextures * 0x8);

            texInfoOffset = (bufSize - pcmpOffset);
            bufSize += (int)(nTextures * 0x20);

            bufSize += bufSize.Align(4096);

            ddsOffset = bufSize;
            ddsOffset2 = (ddsOffset - pcmpOffset);

            int[] texOffsets = new int[nTextures];

            for (int t = 0; t < nTextures; t++)
            {
                texOffsets[t] = (bufSize - ddsOffset);

                bufSize += MaterialData.Textures[t].Buffer.Length;
                bufSize += bufSize.Align(128);
            }

            int pcmpSize = (bufSize - pcmpOffset);

            bufSize += bufSize.Align(4096);

            // Now that we have our initialized buffer size, write ALL the data!
            byte[] buffer = new byte[bufSize];

            using (MemoryStream f = new MemoryStream(buffer))
            {
                f.Write(Magic);
                f.Write(UID);

                f.Write(nParts);
                f.Write(partsOffset);

                f.Write(nGroups);
                f.Write(groupsOffset);

                f.Write(nMeshes);
                f.Write(meshesOffset);

                f.Write((short)UID);
                f.Write((short)0x474A);

                f.Seek(0x4, SeekOrigin.Current);

                f.Write(ddsOffset);
                f.Write(pcmpOffset);

                if (writeModels)
                {
                    f.Write(nIndices);
                    f.Write(indicesSize);
                    f.Write(indicesOffset);

                    f.Write(0x1);
                    f.Write(fvfOffset);

                    // Write FVF data
                    f.Seek(fvfOffset, SeekOrigin.Begin);

                    f.Write(nVertices);
                    f.Write(vertsSize);
                    f.Write(vertsOffset);
                    f.Write(vertLength);

                    // Write indices
                    f.Seek(indicesOffset, SeekOrigin.Begin);

                    for (int i = 0; i < nIndices; i++)
                        f.Write((ushort)Indices[i]);

                    // Write vertices
                    f.Seek(vertsOffset, SeekOrigin.Begin);

                    for (int v = 0; v < nVertices; v++)
                        f.Write(VertexBuffers[v].GetBytes());

                    // Write part groups
                    f.Seek(partsOffset, SeekOrigin.Begin);

                    int gIdx = 0;

                    for (int p = 0; p < nParts; p++)
                    {
                        PartsGroup part = Parts[p];

                        f.Write(part.UID);
                        f.Write(part.Handle);

                        // skip float padding
                        f.Seek(0x10, SeekOrigin.Current);

                        f.Write(part.Unknown1);
                        f.Write(part.Unknown2);

                        f.Seek(0x8, SeekOrigin.Current);

                        // write list of 8 Point4D's
                        for (int t = 0; t < 8; t++)
                        {
                            f.WriteFloat(part.Transform[t].X);
                            f.WriteFloat(part.Transform[t].Y);
                            f.WriteFloat(part.Transform[t].Z);
                            f.WriteFloat(part.Transform[t].W);
                        }

                        for (int d = 0; d < 7; d++)
                        {
                            PartDefinition partDef = part.Parts.Find((def) => def.ID == d);

                            if (partDef.Group != null)
                            {
                                f.Write(groupsOffset + (gIdx++ * 0x58));

                                f.Seek(0x4, SeekOrigin.Current);

                                f.Write(partDef.Unknown);

                                f.Seek(0x8, SeekOrigin.Current);

                                f.Write(partDef.Reserved);

                                f.Seek(0x8, SeekOrigin.Current);
                            }
                            else
                            {
                                f.Seek(0x20, SeekOrigin.Current);
                            }
                        }
                    }

                    // Write mesh groups
                    f.Seek(groupsOffset, SeekOrigin.Begin);

                    int mIdx = 0;

                    for (int g = 0; g < nGroups; g++)
                    {
                        MeshGroup group = MeshGroups[g];

                        f.Write(meshesOffset + (mIdx * 0x38));

                        f.Seek(0x44, SeekOrigin.Current);

                        f.Write((short)group.Meshes.Count);

                        mIdx += group.Meshes.Count;

                        f.Seek(0xE, SeekOrigin.Current);
                    }

                    // Write meshes
                    f.Seek(meshesOffset, SeekOrigin.Begin);

                    for (int m = 0; m < nMeshes; m++)
                    {
                        IndexedMesh mesh = Meshes[m];

                        f.Write((int)mesh.PrimitiveType);

                        f.Write(mesh.BaseVertexIndex);
                        f.Write(mesh.MinIndex);
                        f.Write(mesh.NumVertices);

                        f.Write(mesh.StartIndex);
                        f.Write(mesh.PrimitiveCount);

                        f.Seek(0x18, SeekOrigin.Current);

                        f.Write((short)mesh.MaterialId);
                        f.Write((short)mesh.SourceUID);

                        f.Seek(0x4, SeekOrigin.Current);
                    }
                }

                // -- Write PCMP -- \\
                f.Seek(pcmpOffset, SeekOrigin.Begin);

                // 'PCMP'
                f.Write(0x504D4350);
                f.Write(0x3);

                f.Write(nMaterials);
                f.Write(materialsOffset);

                f.Write(nSubMaterials);
                f.Write(subMatTableOffset);

                f.Write(nSubMaterials);
                f.Write(subMaterialsOffset);

                f.Write(nTextures);
                f.Write(texInfoTableOffset);

                f.Write(nTextures);
                f.Write(texInfoOffset);

                f.Write(ddsOffset2);
                f.Write(pcmpSize);

                f.Seek((pcmpOffset + materialsOffset), SeekOrigin.Begin);

                int stIdx = 0;

                for (int m = 0; m < nMaterials; m++)
                {
                    PCMPMaterial material = MaterialData.Materials[m];

                    f.Write(subMatTableOffset + (stIdx * 0x8));
                    f.Write(material.SubMaterials.Count);

                    f.Write(material.Reserved1);
                    f.Write(material.Reserved2);
                    f.Write(material.Reserved3);
                    f.Write(material.Reserved4);

                    stIdx += material.SubMaterials.Count;
                }

                f.Seek((pcmpOffset + subMatTableOffset), SeekOrigin.Begin);

                int sIdx = 0;

                for (int st = 0; st < nSubMaterials; st++)
                {
                    f.Write(subMaterialsOffset + (sIdx++ * 0x20));
                    f.Seek(0x4, SeekOrigin.Current);
                }

                f.Seek((pcmpOffset + subMaterialsOffset), SeekOrigin.Begin);

                int ttIdx = 0;

                for (int s = 0; s < nSubMaterials; s++)
                {
                    PCMPSubMaterial subMat = MaterialData.SubMaterials[s];

                    f.Write(subMat.Flags);

                    f.Write(subMat.Mode);
                    f.Write(subMat.Type);

                    f.Seek(0x8, SeekOrigin.Current);

                    f.Write(texInfoTableOffset + (ttIdx * 0x8));
                    f.Write(subMat.Textures.Count);

                    ttIdx += subMat.Textures.Count;

                    f.Seek(0x8, SeekOrigin.Current);
                }

                f.Seek((pcmpOffset + texInfoTableOffset), SeekOrigin.Begin);

                int tIdx = 0;

                for (int tt = 0; tt < nTextures; tt++)
                {
                    f.Write(texInfoOffset + (tIdx++ * 0x20));
                    f.Seek(0x4, SeekOrigin.Current);
                }

                f.Seek((pcmpOffset + texInfoOffset), SeekOrigin.Begin);

                for (int t = 0; t < nTextures; t++)
                {
                    PCMPTexture texture = MaterialData.Textures[t];

                    f.Write((uint)0x1010101);
                    f.Write(texture.CRC32);

                    f.Write(texOffsets[t]);
                    f.Write(texture.Buffer.Length);
                    f.Write(texture.Type);

                    f.Write(texture.Width);
                    f.Write(texture.Height);

                    f.Seek(0x8, SeekOrigin.Current);

                    int holdPos = (int)f.Position;

                    f.Seek((ddsOffset + texOffsets[t]), SeekOrigin.Begin);
                    f.Write(texture.Buffer);

                    f.Seek(holdPos, SeekOrigin.Begin);
                }
            }

            BlockData.Buffer = buffer;
            */
        }

        public ModelPackagePC(BlockData blockData)
            : base(blockData)
        {

        }
    }
}
