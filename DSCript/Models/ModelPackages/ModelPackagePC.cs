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
        protected override void Load()
        {
            if (Spooler.Reserved != 6)
                throw new Exception("Bad MDPC version, cannot load ModelPackage!");

            using (var f = Spooler.GetMemoryStream())
            {
                if (f.ReadUInt32() != 6)
                    throw new Exception("Bad magic, cannot load ModelPackage!");

                UID                     = f.ReadInt32();

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

                if (numVertexBuffers > 0)
                    VertexBuffers = new List<VertexData>(numVertexBuffers);

                /* ------------------------------
                 * Read vertex buffer header(s) (Size: 0x1C)
                 * ------------------------------ */
                for (int vB = 0; vB < numVertexBuffers; vB++)
                {
                    f.Seek((vB * 0x1C), fvfOffset);

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

                IndexBuffer = new IndexData(nIndices);

                for (int i = 0; i < nIndices; i++)
                    IndexBuffer.Buffer[i] = f.ReadUInt16();

                /* ------------------------------
                 * Read model data
                 * ------------------------------ */
                var meshes = new Dictionary<long, MeshDefinition>(nMeshes);
                var groups = new Dictionary<long, MeshGroup>(nMeshGroups);

                Meshes = new List<MeshDefinition>(nMeshes);
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
                    var offset = f.Seek((i * 0x38), meshesOffset);

                    MeshDefinition mesh        = new MeshDefinition(this);
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
                    var offset = f.Seek((i * 0x58), meshGroupsOffset);
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
                        MeshDefinition mesh = meshes[mOffset + (k * 0x38)];
                        mesh.MeshGroup = group;

                        group.Meshes.Add(mesh);
                    }
                }

                /* ------------------------------
                 * Read parts groups (Size: 0x188)
                 * ------------------------------ */
                for (int i = 0; i < nParts; i++)
                {
                    var entryPoint = f.Seek((i * 0x188), partsOffset);

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
                    var vBufferId = f.ReadInt16();

                    part.VertexBuffer = VertexBuffers[vBufferId];

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
                            foreach (MeshDefinition mesh in mGroup.Meshes)
                                mesh.PartsGroup = entry.Parent;

                            // This is obviously a bad way to fix something that was clearly intentional...
                            // FIX IT
                            //if (entry.Unknown > 1)
                            //{
                            //    DSC.Log("Attempting to fix buggy parts group {0} @ 0x{1:X}", part.UID, entryPoint);
                            //
                            //    PartsGroup newPart = new PartsGroup() {
                            //        UID = part.UID,
                            //        Handle = part.Handle,
                            //        VertexBufferId = part.VertexBufferId,
                            //        Unknown1 = part.Unknown1,
                            //        Unknown2 = part.Unknown2,
                            //        Transform = part.Transform
                            //    };
                            //
                            //    Parts.Add(newPart);
                            //
                            //    PartDefinition pDef = new PartDefinition(k) {
                            //        Parent = newPart,
                            //        Unknown = 1,
                            //        Reserved = entry.Reserved
                            //    };
                            //
                            //    newPart.Parts.Add(pDef);
                            //
                            //    MeshGroup nMGroup = groups[gOffset + 0x58];
                            //
                            //    pDef.Group = nMGroup;
                            //    nMGroup.Parent = entry;
                            //
                            //    foreach (IndexedMesh mesh in nMGroup.Meshes)
                            //        mesh.PartsGroup = pDef.Parent;
                            //}
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

                var nMats           = f.ReadInt32();
                var matsOffset      = f.ReadUInt32();

                var table1Count     = f.ReadInt32();
                var table1Offset    = f.ReadUInt32();

                var nSubMats        = f.ReadInt32();
                var subMatsOffset   = f.ReadUInt32();

                var table2Count     = f.ReadInt32();
                var table2Offset    = f.ReadUInt32();

                var DDSInfoCount    = f.ReadInt32();
                var DDSInfoOffset   = f.ReadUInt32();

                var DDSOffset       = f.ReadUInt32();
                var Size            = f.ReadUInt32();

                var textures = new Dictionary<long, PCMPTexture>(DDSInfoCount);
                var subMaterials = new Dictionary<long, PCMPSubMaterial>(nSubMats);

                // Read backwards

                Textures = new List<PCMPTexture>(DDSInfoCount);

                // Textures (Size: 0x20)
                for (int t = 0; t < DDSInfoCount; t++)
                {
                    var baseOffset = f.Seek(DDSInfoOffset + (t * 0x20), pcmpOffset) - pcmpOffset;
                    
                    PCMPTexture textureInfo = new PCMPTexture();

                    Textures.Add(textureInfo);

                    //add to texture lookup
                    textures.Add(baseOffset, textureInfo);

                    textureInfo.Reserved = f.ReadUInt32();
                    textureInfo.CRC32   = f.ReadUInt32();

                    uint offset         = f.ReadUInt32();
                    int size            = f.ReadInt32();

                    textureInfo.Type    = f.ReadUInt32();

                    textureInfo.Width   = f.ReadUInt16();
                    textureInfo.Height  = f.ReadUInt16();

                    textureInfo.Unk5    = f.ReadUInt32();
                    textureInfo.Unk6    = f.ReadUInt32();

                    // get DDS from absolute offset (defined in MDPC header)
                    f.Seek(offset, ddsOffset);
                    textureInfo.Buffer  = f.ReadBytes(size);
                }

                SubMaterials = new List<PCMPSubMaterial>(nSubMats);

                // Submaterials (Size: 0x20)
                for (int s = 0; s < nSubMats; s++)
                {
                    var baseOffset = f.Seek(subMatsOffset + (s * 0x20), pcmpOffset) - pcmpOffset;

                    PCMPSubMaterial subMaterial = new PCMPSubMaterial() {
                        Flags = f.ReadUInt32(),
                        Mode = f.ReadUInt16(),
                        Type = f.ReadUInt16()
                    };

                    SubMaterials.Add(subMaterial);

                    //add to submaterial lookup
                    subMaterials.Add(baseOffset, subMaterial);

                    f.Seek(0x8, SeekOrigin.Current);

                    // table info
                    uint offset = f.ReadUInt32();
                    uint count = f.ReadUInt32();

                    // get texture from table
                    for (int t = 0; t < count; t++)
                    {
                        f.Seek(offset + (t * 0x8), pcmpOffset);
                        subMaterial.Textures.Add(textures[f.ReadUInt32()]);
                    }
                }

                Materials = new List<PCMPMaterial>(nMats);

                // Materials (Size: 0x18)
                for (int m = 0; m < nMats; m++)
                {
                    f.Seek(matsOffset + (m * 0x18), pcmpOffset);

                    // table info
                    uint offset = f.ReadUInt32();
                    uint count = f.ReadUInt32();

                    PCMPMaterial material = new PCMPMaterial() {
                        Reserved1 = f.ReadUInt32(),
                        Reserved2 = f.ReadUInt32(),
                        Reserved3 = f.ReadUInt32(),
                        Reserved4 = f.ReadUInt32()
                    };

                    Materials.Add(material);

                    // get submaterial from table
                    for (int s = 0; s < count; s++)
                    {
                        f.Seek(offset + (s * 0x8), pcmpOffset);
                        material.SubMaterials.Add(subMaterials[f.ReadUInt32()]);
                    }
                }

                // lookup tables no longer needed
                textures.Clear();
                subMaterials.Clear();
            }
        }

        protected override void Save()
        {
            int bufferSize          = 0;

            int nParts              = 0;
            int nGroups             = 0;
            int nMeshes             = 0;
            int nIndices            = 0;
            int nVertexBuffers      = 0;

            int partsOffset         = 0x80;
            int groupsOffset        = 0;
            int meshesOffset        = 0;
            
            int ddsOffset           = 0;
            int pcmpOffset          = 0;

            int indicesOffset       = 0;
            int indicesSize         = 0;
            
            int fvfOffset           = 0;
            int vBufferOffset       = 0;


            bool writeModels        = (VertexBuffers != null) && (Parts != null);

            // Size of header
            bufferSize = Memory.Align(0x44, 128);

            if (writeModels)
            {
                nParts              = Parts.Count;
                nGroups             = MeshGroups.Count;
                nMeshes             = Meshes.Count;

                nIndices            = IndexBuffer.Buffer.Length;
                indicesSize         = nIndices * 2;

                nVertexBuffers      = VertexBuffers.Count;

                // Add up size of parts groups
                bufferSize = Memory.Align(bufferSize + (nParts * 0x188), 128);
                groupsOffset = bufferSize;

                // Add up size of mesh groups (non-aligned)
                bufferSize += (nGroups * 0x58);
                meshesOffset = bufferSize;

                // Add up size of mesh definitions
                bufferSize = Memory.Align(bufferSize + (nMeshes * 0x38), 128);
                fvfOffset = bufferSize;

                // Add up size of vertex buffer(s) FVF data
                bufferSize += (nVertexBuffers * 0x1C);

                indicesOffset = bufferSize;
                bufferSize += indicesSize;

                bufferSize = Memory.Align(bufferSize, 4096);
                vBufferOffset = bufferSize;

                foreach (VertexData vBuffer in VertexBuffers)
                    bufferSize += (vBuffer.Buffer.Length * vBuffer.Length);
            }

            bufferSize = Memory.Align(bufferSize, 4096);

            // -- PCMP -- \\

            int nMaterials              = Materials.Count;
            int nSubMaterials           = SubMaterials.Count;
            int nTextures               = Textures.Count;

            int materialsOffset         = 0;
            int subMatTableOffset       = 0;
            int subMaterialsOffset      = 0;
            int texInfoTableOffset      = 0;
            int texInfoOffset           = 0;

            pcmpOffset = bufferSize;

            // Size of header
            bufferSize += 0x38;

            materialsOffset = (bufferSize - pcmpOffset);
            bufferSize += (nMaterials * 0x18);

            subMatTableOffset = (bufferSize - pcmpOffset);
            bufferSize += (nSubMaterials * 0x8);

            subMaterialsOffset = (bufferSize - pcmpOffset);
            bufferSize += (nSubMaterials * 0x20);

            texInfoTableOffset = (bufferSize - pcmpOffset);
            bufferSize += (nTextures * 0x8);

            texInfoOffset = (bufferSize - pcmpOffset);
            bufferSize += (nTextures * 0x20);

            bufferSize = Memory.Align(bufferSize, 4096);

            ddsOffset = bufferSize;

            Dictionary<uint, int> texOffsets = new Dictionary<uint, int>(nTextures);

            for (int t = 0; t < nTextures; t++)
            {
                PCMPTexture tex = Textures[t];

                uint crc32 = tex.CRC32;

                if (!texOffsets.ContainsKey(crc32))
                {
                    bufferSize = Memory.Align(bufferSize, 128);

                    texOffsets.Add(crc32, (bufferSize - ddsOffset));

                    bufferSize += tex.Buffer.Length;
                }
            }

            int pcmpSize = (bufferSize - pcmpOffset);

            bufferSize = Memory.Align(bufferSize, 4096);

            // Now that we have our initialized buffer size, write ALL the data!
            byte[] buffer = new byte[bufferSize];

            using (MemoryStream f = new MemoryStream(buffer))
            {
                f.Write(6);
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

                    f.Write(nVertexBuffers);
                    f.Write(fvfOffset);

                    // write vertex buffer(s) & FVF data
                    for (int vB = 0; vB < VertexBuffers.Count; vB++)
                    {
                        var vBuffer = VertexBuffers[vB];
                        
                        f.Seek(fvfOffset + (vB * 0x1C), SeekOrigin.Begin);

                        int nVerts = vBuffer.Buffer.Length;
                        int vertsSize = nVerts * vBuffer.Length;

                        f.Write(nVerts);
                        f.Write(vertsSize);
                        f.Write(vBufferOffset);
                        f.Write(vBuffer.Length);

                        // write vertices
                        f.Seek(vBufferOffset, SeekOrigin.Begin);

                        for (int v = 0; v < nVerts; v++)
                            f.Write(vBuffer.Buffer[v].GetBytes());

                        vBufferOffset += vertsSize;
                    }

                    // Write indices
                    f.Seek(indicesOffset, SeekOrigin.Begin);

                    for (int i = 0; i < nIndices; i++)
                        f.Write((ushort)IndexBuffer[i]);

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

                        var vBufferId = VertexBuffers.IndexOf(part.VertexBuffer);

                        if (vBufferId == -1)
                            throw new Exception("FATAL ERROR: Cannot get Vertex Buffer ID - CANNOT EXPORT MODEL PACKAGE!!!");

                        f.Write(vBufferId);
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

                                if (partDef.Unknown > 1)
                                {
                                    for (int i = 1; i < partDef.Unknown; i++)
                                        ++gIdx;
                                }

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

                f.Write((ddsOffset - pcmpOffset));
                f.Write(pcmpSize);

                f.Seek((pcmpOffset + materialsOffset), SeekOrigin.Begin);

                int stIdx = 0;

                for (int m = 0; m < nMaterials; m++)
                {
                    PCMPMaterial material = Materials[m];

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
                    PCMPSubMaterial subMat = SubMaterials[s];

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
                    PCMPTexture texture = Textures[t];

                    uint crc32 = texture.CRC32;

                    f.Write(0x1010101u);
                    f.Write(texture.CRC32);

                    f.Write(texOffsets[crc32]);
                    f.Write(texture.Buffer.Length);
                    f.Write(texture.Type);

                    f.Write((ushort)texture.Width);
                    f.Write((ushort)texture.Height);

                    f.Seek(0x8, SeekOrigin.Current);

                    int holdPos = (int)f.Position;

                    f.Seek((ddsOffset + texOffsets[crc32]), SeekOrigin.Begin);
                    
                    if (f.PeekByte() != 0x44)
                        f.Write(texture.Buffer);

                    f.Seek(holdPos, SeekOrigin.Begin);
                }
            }

            Spooler.SetBuffer(buffer);
        }
    }
}
