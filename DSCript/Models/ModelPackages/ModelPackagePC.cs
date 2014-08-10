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

                var nParts              = f.ReadInt32();
                var partsOffset         = f.ReadUInt32();

                var nMeshGroups         = f.ReadInt32();
                var meshGroupsOffset    = f.ReadUInt32();

                var nMeshes             = f.ReadInt32();
                var meshesOffset        = f.ReadUInt32();

                var uid2                = f.ReadUInt16();

                if (uid2 != UID)
                    DSC.Log("Unknown magic check failed - wanted {0}, got {1}", UID, uid2);

                // Skip junk
                f.Position += 0x6;

                var ddsOffset           = f.ReadInt32();
                var pcmpOffset          = f.ReadInt32();

                var nIndices            = f.ReadInt32();
                var indicesSize         = f.ReadUInt32();
                var indicesOffset       = f.ReadUInt32();

                var numVertexBuffers    = f.ReadInt32();

                var fvfOffset           = f.ReadUInt32();

                // skip packages with no models
                if (nParts > 0)
                {
                    Parts           = new List<PartsGroup>(nParts);
                    MeshGroups      = new List<MeshGroup>(nMeshGroups);
                    Meshes          = new List<MeshDefinition>(nMeshes);

                    VertexBuffers   = new List<VertexData>(numVertexBuffers);

                    /* ------------------------------
                     * Read vertex buffer header(s) (Size: 0x1C)
                     * ------------------------------ */
                    for (int vB = 0; vB < numVertexBuffers; vB++)
                    {
                        f.Position = fvfOffset + (vB * 0x1C);

                        var nVerts       = f.ReadInt32();
                        var vertsSize    = f.ReadUInt32();
                        var vertsOffset  = f.ReadUInt32();
                        var vertLength   = f.ReadInt32();

                        var vertexBuffer = new VertexData(nVerts, vertLength);

                        VertexBuffers.Add(vertexBuffer);

                        /* ------------------------------
                         * Read vertices in buffer
                         * ------------------------------ */
                        f.Position = vertsOffset;

                        for (int i = 0; i < nVerts; i++)
                            vertexBuffer.Buffer[i] = new Vertex(f.ReadBytes(vertLength), vertexBuffer.VertexType);
                    }

                    /* ------------------------------
                     * Read index buffer
                     * ------------------------------ */
                    f.Position = indicesOffset;

                    IndexBuffer = new IndexData(nIndices);

                    for (int i = 0; i < nIndices; i++)
                        IndexBuffer.Buffer[i] = f.ReadUInt16();

                    /* ------------------------------
                     * Read parts groups (Size: 0x188)
                     * ------------------------------ */
                    for (int p = 0; p < nParts; p++)
                    {
                        f.Position = partsOffset + (p * 0x188);

                        var pGroup = new PartsGroup() {
                            UID = f.ReadUInt32(),
                            Handle = f.ReadUInt32()
                        };

                        Parts.Add(pGroup);

                        // skip padding
                        f.Position += 0x10;

                        // INCOMING TRANSMISSION...
                        // RE: OPERATION S.T.E.R.N....
                        // ...
                        // YOUR ASSISTANCE HAS BEEN NOTED...
                        // ...
                        // <END OF TRANSMISSION>...
                        var vBufferId = f.ReadInt16();

                        pGroup.VertexBuffer = VertexBuffers[vBufferId];

                        pGroup.Unknown1 = f.ReadInt16();
                        pGroup.Unknown2 = f.ReadInt32();
                        pGroup.Unknown3 = f.ReadInt32();

                        // skip padding
                        f.Position += 0x4;

                        // read unknown list of 8 Point4Ds
                        for (int t = 0; t < 8; t++)
                        {
                            pGroup.Transform.Add(new Point4D() {
                                X = (double)f.ReadSingle(),
                                Y = (double)f.ReadSingle(),
                                Z = (double)f.ReadSingle(),
                                W = (double)f.ReadSingle()
                            });
                        }

                        var defStart = f.Position;

                        // 7 part definitions per group
                        for (int k = 0; k < 7; k++)
                        {
                            f.Position = defStart + (k * 0x20);

                            var partEntry = new PartDefinition(k) {
                                Parent = pGroup
                            };

                            pGroup.Parts.Add(partEntry);

                            var gOffset = f.ReadInt32();

                            // skip padding
                            f.Position += 0x4;

                            var gCount = f.ReadInt32();

                            // skip padding
                            f.Position += 0x14;

                            if (gCount == 0)
                                continue;

                            /* ------------------------------
                             * Read mesh groups (Size: 0x58)
                             * ------------------------------ */
                            for (int g = 0; g < gCount; g++)
                            {
                                f.Position = gOffset + (g * 0x58);

                                var mOffset = f.ReadInt32();

                                // skip padding
                                f.Position += 0x44;

                                var mCount = f.ReadInt16();

                                MeshGroup mGroup = new MeshGroup(mCount) {
                                    Parent = partEntry
                                };

                                partEntry.Groups.Add(mGroup);
                                MeshGroups.Add(mGroup);

                                /* ------------------------------
                                 * Read mesh definitions (Size: 0x38)
                                 * ------------------------------ */
                                for (int m = 0; m < mCount; m++)
                                {
                                    f.Position = mOffset + (m * 0x38);

                                    var mesh = new MeshDefinition(this) {
                                        PrimitiveType = (D3DPRIMITIVETYPE)f.ReadInt32(),
                                        BaseVertexIndex = f.ReadInt32(),
                                        MinIndex = f.ReadUInt32(),
                                        NumVertices = f.ReadUInt32(),
                                        StartIndex = f.ReadUInt32(),
                                        PrimitiveCount = f.ReadUInt32(),

                                        MeshGroup = mGroup,
                                        PartsGroup = pGroup
                                    };

                                    // skip padding
                                    f.Position += 0x18;

                                    mesh.MaterialId = f.ReadInt16();
                                    mesh.SourceUID = f.ReadUInt16();

                                    mGroup.Meshes.Add(mesh);
                                    Meshes.Add(mesh);
                                }
                            }
                        }
                    }
                }

                // Read PCMP
                if (pcmpOffset == 0)
                    return;

                // Skip the header
                f.Position = pcmpOffset + 0x8;

                var matCount        = f.ReadInt32();
                var matOffset       = f.ReadUInt32() + pcmpOffset;

                // don't need this
                f.Position += 0x8;

                var subMatCount     = f.ReadInt32();
                var subMatOffset    = f.ReadUInt32() + pcmpOffset;

                // or this
                f.Position += 0x8;

                var texInfoCount    = f.ReadInt32();
                var texInfoOffset   = f.ReadUInt32() + pcmpOffset;

                // don't need this either
                f.Position += 0x8;

                Materials       = new List<PCMPMaterial>(matCount);
                SubMaterials    = new List<PCMPSubMaterial>(subMatCount);
                Textures        = new List<PCMPTexture>(texInfoCount);

                var texLookup   = new Dictionary<int, byte[]>();

                // Materials (Size: 0x18)
                for (int m = 0; m < matCount; m++)
                {
                    f.Position = matOffset + (m * 0x18);

                    // table info
                    var mOffset = f.ReadInt32() + pcmpOffset;
                    var mCount  = f.ReadInt32();

                    var material = new PCMPMaterial();

                    Materials.Add(material);

                    // get submaterial(s)
                    for (int s = 0; s < mCount; s++)
                    {
                        f.Position  = mOffset + (s * 0x8);

                        var sOffset = f.ReadInt32() + pcmpOffset;

                        f.Position  = sOffset;

                        var subMat = new PCMPSubMaterial() {
                            Flags   = f.ReadUInt32(),
                            Mode    = f.ReadUInt16(),
                            Type    = f.ReadUInt16()
                        };

                        material.SubMaterials.Add(subMat);
                        SubMaterials.Add(subMat);

                        f.Position += 0x8;

                        var tOffset = f.ReadInt32() + pcmpOffset;
                        var tCount  = f.ReadInt32();

                        for (int t = 0; t < tCount; t++)
                        {
                            f.Position = tOffset + (t * 0x8);

                            var texOffset = f.ReadInt32() + pcmpOffset;

                            f.Position = texOffset;

                            var textureInfo = new PCMPTexture();
                            
                            subMat.Textures.Add(textureInfo);
                            Textures.Add(textureInfo);

                            f.Position += 0x4;

                            textureInfo.CRC32   = f.ReadUInt32();

                            var offset          = f.ReadInt32() + ddsOffset;
                            var size            = f.ReadInt32();

                            textureInfo.Type    = f.ReadInt32();

                            textureInfo.Width   = f.ReadInt16();
                            textureInfo.Height  = f.ReadInt16();

                            textureInfo.Unknown = f.ReadInt32();

                            if (!texLookup.ContainsKey(offset))
                            {
                                f.Position = offset;
                                texLookup.Add(offset, f.ReadBytes(size));
                            }

                            textureInfo.Buffer = texLookup[offset];
                        }
                    }
                }

                // lookup tables no longer needed
                texLookup.Clear();
            }
        }

        protected override void Save()
        {
            // init variables
            int bufferSize      = 0;

            int nParts          = 0;
            int nGroups         = 0;
            int nMeshes         = 0;
            int nIndices        = 0;
            int nVertexBuffers  = 0;

            int partsOffset     = 0x80;
            int groupsOffset    = 0;
            int meshesOffset    = 0;
        
            int ddsOffset       = 0;
            int pcmpOffset      = 0;

            int indicesOffset   = 0;
            int indicesSize     = 0;
        
            int fvfOffset       = 0;
            int vBufferOffset   = 0;

            bool writeModels    = (VertexBuffers != null) && (Parts != null);

            var deadMagic   = 0xCDCDCDCD;
            var deadCode    = BitConverter.GetBytes(deadMagic);

            // Size of header
            bufferSize = Memory.Align(0x44, 128);

            if (writeModels)
            {
                nParts          = Parts.Count;
                nGroups         = MeshGroups.Count;
                nMeshes         = Meshes.Count;

                nIndices        = IndexBuffer.Buffer.Length;
                indicesSize     = nIndices * 2;

                nVertexBuffers  = VertexBuffers.Count;

                // Add up size of parts groups
                bufferSize  = Memory.Align(bufferSize + (nParts * 0x188), 128);
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

                foreach (var vBuffer in VertexBuffers)
                    bufferSize += vBuffer.Size;
            }

            bufferSize = Memory.Align(bufferSize, 4096);

            // -- PCMP -- \\

            int nMaterials              = Materials.Count;
            int nSubMaterials           = SubMaterials.Count;
            int nTextures               = Textures.Count;

            int pcmpSize                = 0;

            int materialsOffset         = 0;
            int subMatTableOffset       = 0;
            int subMaterialsOffset      = 0;
            int texInfoTableOffset      = 0;
            int texInfoOffset           = 0;

            pcmpOffset = bufferSize;
            
            // Size of header
            pcmpSize += 0x38;

            materialsOffset = pcmpSize;
            pcmpSize += (nMaterials * 0x18);

            subMatTableOffset = pcmpSize;
            pcmpSize += (nSubMaterials * 0x8);

            subMaterialsOffset = pcmpSize;
            pcmpSize += (nSubMaterials * 0x20);

            texInfoTableOffset = pcmpSize;
            pcmpSize += (nTextures * 0x8);

            texInfoOffset = pcmpSize;
            pcmpSize += (nTextures * 0x20);

            pcmpSize = Memory.Align(pcmpSize, 4096);

            ddsOffset = pcmpSize;

            Dictionary<uint, int> texOffsets = new Dictionary<uint, int>(nTextures);

            for (int t = 0; t < nTextures; t++)
            {
                PCMPTexture tex = Textures[t];

                var crc32 = tex.CRC32;

                if (!texOffsets.ContainsKey(crc32))
                {
                    pcmpSize = Memory.Align(pcmpSize, 128);

                    texOffsets.Add(crc32, (pcmpSize - ddsOffset));

                    pcmpSize += tex.Buffer.Length;
                }
            }

            // add the PCMP size to the buffer size
            bufferSize += Memory.Align(pcmpSize, 4096);

            // Now that we have our initialized buffer size, write ALL the data!
            var buffer = new byte[bufferSize];

            using (var f = new MemoryStream(buffer))
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
                f.Write(0xFB, 0x95);

                f.Position += 0x4;

                f.Write(ddsOffset + pcmpOffset);
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
                        f.Position = fvfOffset + (vB * 0x1C);

                        var vBuffer = VertexBuffers[vB];

                        var nVerts      = vBuffer.Buffer.Length;
                        var vertsSize   = nVerts * vBuffer.Length;

                        f.Write(nVerts);
                        f.Write(vertsSize);
                        f.Write(vBufferOffset);
                        f.Write(vBuffer.Length);

                        // write vertices
                        f.Position = vBufferOffset;

                        for (int v = 0; v < nVerts; v++)
                            f.Write(vBuffer.Buffer[v].GetBytes());

                        vBufferOffset += vertsSize;
                    }

                    // Write indices
                    f.Position = indicesOffset;

                    for (int i = 0; i < nIndices; i++)
                        f.Write((ushort)IndexBuffer[i]);

                    int gIdx = 0;

                    for (int p = 0; p < nParts; p++)
                    {
                        f.Position = partsOffset + (p * 0x188);

                        var part = Parts[p];

                        f.Write(part.UID);
                        f.Write(part.Handle);

                        // skip float padding
                        f.Position += 0x10;

                        var vBufferId = VertexBuffers.IndexOf(part.VertexBuffer);

                        if (vBufferId == -1)
                            throw new Exception("FATAL ERROR: Cannot get Vertex Buffer ID - CANNOT EXPORT MODEL PACKAGE!!!");

                        f.Write((short)vBufferId);

                        f.Write(part.Unknown1);
                        f.Write(part.Unknown2);
                        f.Write(part.Unknown3);

                        f.Position += 0x4;

                        // write list of 8 Point4D's
                        for (int t = 0; t < 8; t++)
                        {
                            f.WriteFloat(part.Transform[t].X);
                            f.WriteFloat(part.Transform[t].Y);
                            f.WriteFloat(part.Transform[t].Z);
                            f.WriteFloat(part.Transform[t].W);
                        }

                        var defStart = f.Position;

                        for (int d = 0; d < 7; d++)
                        {
                            f.Position = defStart + (d * 0x20);

                            var partDef = part.Parts[d];

                            if (partDef == null || partDef.Groups == null)
                                continue;

                            var count = partDef.Groups.Count;

                            f.Write(groupsOffset + (gIdx * 0x58));

                            f.Position += 0x4;

                            f.Write(count);

                            gIdx += count;
                        }
                    }

                    int mIdx = 0;

                    for (int g = 0; g < nGroups; g++)
                    {
                        f.Position = groupsOffset + (g * 0x58);

                        var group = MeshGroups[g];

                        f.Write(meshesOffset + (mIdx * 0x38));

                        f.Fill(deadCode, 0x44);

                        f.Write((short)group.Meshes.Count);

                        f.Fill(deadCode, 0xE);

                        mIdx += group.Meshes.Count;
                    }

                    // Write meshes
                    for (int m = 0; m < nMeshes; m++)
                    {
                        f.Position = meshesOffset + (m * 0x38);

                        var mesh = Meshes[m];

                        f.Write((int)mesh.PrimitiveType);

                        f.Write(mesh.BaseVertexIndex);
                        f.Write(mesh.MinIndex);
                        f.Write(mesh.NumVertices);

                        f.Write(mesh.StartIndex);
                        f.Write(mesh.PrimitiveCount);

                        f.Fill(deadCode, 0x18);

                        f.Write((short)mesh.MaterialId);
                        f.Write((short)mesh.SourceUID);

                        f.Write(deadMagic);
                    }
                }

                f.Position = 0x44;

                f.Fill(deadCode, partsOffset - (int)f.Position);

                // -- Write PCMP -- \\
                f.Position = pcmpOffset;

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

                f.Write(ddsOffset);
                f.Write(pcmpSize); // unused, but this is how the developers did it!

                f.Seek(materialsOffset, pcmpOffset);

                int stIdx = 0;

                // write materials
                for (int m = 0; m < nMaterials; m++)
                {
                    var material = Materials[m];

                    f.Write(subMatTableOffset + (stIdx * 0x8));
                    f.Write(material.SubMaterials.Count);

                    f.Fill(deadCode, 0x10);

                    stIdx += material.SubMaterials.Count;
                }

                f.Seek(subMatTableOffset, pcmpOffset);

                int sIdx = 0;

                for (int st = 0; st < nSubMaterials; st++)
                {
                    f.Write(subMaterialsOffset + (sIdx++ * 0x20));
                    f.Write(deadMagic);
                }

                f.Seek(subMaterialsOffset, pcmpOffset);

                int ttIdx = 0;

                for (int s = 0; s < nSubMaterials; s++)
                {
                    var subMat = SubMaterials[s];

                    f.Write(subMat.Flags);

                    f.Write(subMat.Mode);
                    f.Write(subMat.Type);

                    f.Fill(deadCode, 0x8);

                    f.Write(texInfoTableOffset + (ttIdx * 0x8));
                    f.Write(subMat.Textures.Count);

                    ttIdx += subMat.Textures.Count;

                    f.Fill(deadCode, 0x8);
                }

                f.Seek(texInfoTableOffset, pcmpOffset);

                int tIdx = 0;

                for (int tt = 0; tt < nTextures; tt++)
                {
                    f.Write(texInfoOffset + (tIdx++ * 0x20));
                    f.Write(deadMagic);
                }

                for (int t = 0; t < nTextures; t++)
                {
                    f.Seek(texInfoOffset + (t * 0x20), pcmpOffset);

                    var texture = Textures[t];

                    var tOffset = texOffsets[texture.CRC32];

                    f.Write(deadMagic);
                    f.Write(texture.CRC32);

                    f.Write(tOffset);
                    f.Write(texture.Buffer.Length);
                    f.Write(texture.Type);

                    f.Write((short)texture.Width);
                    f.Write((short)texture.Height);

                    f.Write(texture.Unknown);
                    f.Write(deadMagic);

                    f.Seek(ddsOffset + tOffset, pcmpOffset);
                    
                    if (f.PeekInt32() == 0x0)
                        f.Write(texture.Buffer);
                }
            }

            Spooler.SetBuffer(buffer);
        }
    }
}
