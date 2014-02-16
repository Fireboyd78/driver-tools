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

                uint unkFaceType        = f.ReadUInt32();

                /*if (unkFaceType != 0x1)
                    DSC.Log("Unknown face type check failed, errors may occur.");*/

                uint fvfOffset          = f.ReadUInt32();

                // skip packages with no models
                if (PackageType == PackageType.VehicleGlobals || fvfOffset == 0)
                    goto LoadPCMP;

                /* ------------------------------
                    * Read vertex header
                    * ------------------------------ */
                f.Seek(fvfOffset, SeekOrigin.Begin);

                int nVerts              = f.ReadInt32();
                uint vertsSize          = f.ReadUInt32();
                uint vertsOffset        = f.ReadUInt32();
                int vertLength          = f.ReadInt32();

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

                Meshes = new List<MeshDefinition>(nMeshes);

                for (int i = 0; i < nMeshes; i++)
                {
                    uint offset = (uint)f.GetPosition();

                    MeshDefinition mesh = new MeshDefinition() {
                        Offset = offset
                    };

                    Meshes.Add(mesh);

                    mesh.PrimitiveType = (D3DPRIMITIVETYPE)f.ReadInt32();

                    mesh.BaseVertexIndex = f.ReadInt32();
                    mesh.MinIndex = f.ReadUInt32();
                    mesh.NumVertices = f.ReadUInt32();

                    mesh.StartIndex = f.ReadUInt32();
                    mesh.PrimitiveCount = f.ReadUInt32();

                    // skip padding
                    f.Seek(0x18, SeekOrigin.Current);

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

                        MeshDefinition mesh = Meshes[v];
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

                    // skip padding
                    f.Seek(0x8, SeekOrigin.Current);

                    // read unknown list of 8 Point4Ds
                    for (int t = 0; t < 8; t++)
                        group.Transform.Add(new Point4D(
                            (double)f.ReadSingle(),
                            (double)f.ReadSingle(),
                            (double)f.ReadSingle(),
                            (double)f.ReadSingle()
                        ));

                    // there are 7 part definitions per group
                    for (int k = 0; k < 7; k++)
                    {
                        PartDefinition entry = new PartDefinition(k);

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

                            MeshGroup mGroup = MeshGroups[v];

                            entry.Group = mGroup;
                            mGroup.Parent = entry;
                        }
                    }
                }

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

                int tGroupCount     = f.ReadInt32();
                uint tGroupOffset   = f.ReadUInt32();

                int t2Count         = f.ReadInt32();
                uint t2Offset       = f.ReadUInt32();

                int tSubMatCount    = f.ReadInt32();
                uint tSubMatOffset  = f.ReadUInt32();

                int t4Count         = f.ReadInt32();
                uint t4Offset       = f.ReadUInt32();

                int DDSInfoCount    = f.ReadInt32();
                uint DDSInfoOffset  = f.ReadUInt32();

                uint DDSOffset      = f.ReadUInt32();
                uint Size           = f.ReadUInt32();

                MaterialData = new PCMPData();

                List<PCMPMaterial> materials = MaterialData.Materials;

                // Read everything backwards, it's easiest that way

                // Textures
                f.Seek(pcmpOffset + DDSInfoOffset, SeekOrigin.Begin);

                for (int t = 0; t < DDSInfoCount; t++)
                {
                    uint baseOffset = (uint)f.GetPosition() - pcmpOffset;

                    PCMPTexture textureInfo = new PCMPTexture() {
                        BaseOffset = baseOffset,

                        Unk1 = f.ReadByte(),
                        Unk2 = f.ReadByte(),
                        Unk3 = f.ReadByte(),
                        Unk4 = f.ReadByte(),

                        CRC32 = f.ReadUInt32(),
                        Offset = f.ReadUInt32(),
                        Size = f.ReadUInt32(),
                        Type = f.ReadUInt32(),

                        Width = f.ReadUInt16(),
                        Height = f.ReadUInt16(),

                        Unk5 = f.ReadUInt32(),
                        Unk6 = f.ReadUInt32()
                    };

                    long holdPosition = f.GetPosition();

                    f.Seek(pcmpOffset + DDSOffset + textureInfo.Offset, SeekOrigin.Begin);

                    textureInfo.Buffer = f.ReadBytes((int)textureInfo.Size);

                    f.Seek(holdPosition, SeekOrigin.Begin);

                    MaterialData.Textures.Add(textureInfo);
                }

                f.Seek(pcmpOffset + tSubMatOffset, SeekOrigin.Begin);

                // Submaterials
                for (int s = 0; s < tSubMatCount; s++)
                {
                    uint baseOffset = (uint)f.GetPosition() - pcmpOffset;

                    PCMPSubMaterial subMaterial = new PCMPSubMaterial() {
                        BaseOffset = baseOffset,

                        Unk1 = f.ReadUInt32(),
                        Unk2 = f.ReadUInt16(),
                        Unk3 = f.ReadUInt16()
                    };

                    f.Seek(0x8, SeekOrigin.Current);

                    uint texturesOffset = f.ReadUInt32();
                    uint texturesCount = f.ReadUInt32();

                    f.Seek(0x8, SeekOrigin.Current);

                    long holdPosition = f.GetPosition();

                    f.Seek(pcmpOffset + texturesOffset, SeekOrigin.Begin);

                    for (int t = 0; t < texturesCount; t++)
                    {
                        uint texOffset = f.ReadUInt32();

                        f.Seek(0x4, SeekOrigin.Current);

                        long hold = f.GetPosition();

                        int texIdx = MaterialData.Textures.FindIndex((tex) => tex.BaseOffset == texOffset);

                        if (texIdx == -1)
                            throw new Exception("Fatal error occurred while adding texture to submaterial!");

                        subMaterial.Textures.Add(MaterialData.Textures[texIdx]);

                        f.Seek(hold, SeekOrigin.Begin);
                    }

                    f.Seek(holdPosition, SeekOrigin.Begin);

                    MaterialData.SubMaterials.Add(subMaterial);
                }

                f.Seek(pcmpOffset + tGroupOffset, SeekOrigin.Begin);

                // Materials
                for (int m = 0; m < tGroupCount; m++)
                {
                    uint subMatsOffset = f.ReadUInt32();
                    uint subMatsCount = f.ReadUInt32();

                    PCMPMaterial material = new PCMPMaterial() {
                        Reserved1 = f.ReadUInt32(),
                        Reserved2 = f.ReadUInt32(),
                        Reserved3 = f.ReadUInt32(),
                        Reserved4 = f.ReadUInt32()
                    };

                    long holdPosition = f.GetPosition();

                    f.Seek(pcmpOffset + subMatsOffset, SeekOrigin.Begin);

                    for (int s = 0; s < subMatsCount; s++)
                    {
                        uint subMatOffset = f.ReadUInt32();

                        f.Seek(0x4, SeekOrigin.Current);

                        long hold = f.GetPosition();

                        int subIdx = MaterialData.SubMaterials.FindIndex((sub) => sub.BaseOffset == subMatOffset);

                        if (subIdx == -1)
                            throw new Exception("Fatal error occurred while adding submaterial to material!");

                        material.SubMaterials.Add(MaterialData.SubMaterials[subIdx]);

                        f.Seek(hold, SeekOrigin.Begin);
                    }

                    f.Seek(holdPosition, SeekOrigin.Begin);

                    MaterialData.Materials.Add(material);
                }
            }
        }

        public override void Compile()
        {
            int bufSize             = 0;

            int nParts              = 0;
            int nGroups             = 0;
            int nMeshes             = 0;
            int nIndices            = 0;
            int nVertices           = 0;

            int partsOffset         = 0x80;
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

            bool writeModels        = (Parts != null && Vertices.Buffer != null && Indices.Buffer != null);

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
                nVertices = Vertices.Buffer.Length;

                vertLength = (int)Vertices.VertexType;
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
                        f.Write(Vertices[v].GetBytes());

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
                        MeshDefinition mesh = Meshes[m];

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

                    stIdx += material.SubMaterials.Count;

                    f.Seek(0x4, SeekOrigin.Current);

                    f.Write((uint)0x41C80000);

                    f.Seek(0x8, SeekOrigin.Current);
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

                    f.Write(subMat.Unk1);

                    f.Write(subMat.Unk2);
                    f.Write(subMat.Unk3);

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

            BlockData.Data = buffer;
        }

        public ModelPackagePC(BlockData blockData)
            : base(blockData)
        {

        }
    }
}
