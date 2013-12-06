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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using DSCript;

namespace Antilli.Models
{
    public enum ModelPackageType : uint
    {
        ModelPackagePC     = 0x6, // Driv3r
        ModelPackagePC_X   = 0x1, // Driver: Parallel Lines
        Unknown            = 0x7FFFFFFF
    }

    public abstract class ModelPackage
    {
        public static string GlobalTexturesName { get; set; }
        public static List<PCMPMaterial> GlobalTextures { get; set; }

        public static bool HasGlobalTextures
        {
            get { return GlobalTextures != null; }
        }

        public virtual ModelPackageType Type
        {
            get { return ModelPackageType.Unknown; }
        }

        public PackageType PackageType { get; protected set; }

        public uint Magic
        {
            get { return (uint)Type; }
        }

        public BlockData BlockData { get; set; }

        public List<PartsGroup> Parts { get; set; }
        public List<MeshGroup> MeshGroups { get; set; }
        public List<IndexedPrimitive> Meshes { get; set; }

        public VertexData Vertices { get; set; }
        public IndexData Indices { get; set; }

        public PCMPData MaterialData { get; set; }

        public bool HasTextures
        {
            get { return MaterialData != null; }
        }

        public virtual void Load()
        {
            throw new NotImplementedException();
        }

        public ModelPackage(BlockData blockData)
        {
            BlockData = blockData;
            Load();
        }
    }

    public class ModelPackagePC : ModelPackage
    {
        public override ModelPackageType Type
        {
            get { return ModelPackageType.ModelPackagePC; }
        }

        public override void Load()
        {
            using (BlockEditor blockEditor = new BlockEditor(BlockData))
            {
                BinaryReader f = blockEditor.Reader;

                if (f.ReadUInt32() != Magic)
                    throw new Exception("Bad magic, cannot load ModelPackage!");

                uint type = f.ReadUInt32();

                PackageType = Enum.IsDefined((typeof(PackageType)), type) ? (PackageType)type : PackageType.Unknown;

                int nParts = f.ReadInt32();
                uint partsOffset = f.ReadUInt32();

                int nMeshGroups = f.ReadInt32();
                uint meshGroupsOffset = f.ReadUInt32();

                int nMeshes = f.ReadInt32();
                uint meshesOffset = f.ReadUInt32();

                uint unkMagic = f.ReadUInt16();

                if (unkMagic != (uint)PackageType)
                    DSC.Log("Unknown magic check failed - wanted {0}, got {1}", (uint)PackageType, unkMagic);

                // Skip junk
                f.Seek(0x28, SeekOrigin.Begin);

                uint ddsOffset = f.ReadUInt32();
                uint pcmpOffset = f.ReadUInt32();

                // Skip loading models for vehicle globals
                if (PackageType == PackageType.VehicleGlobals)
                    goto LoadPCMP;

                int nIndices = f.ReadInt32();
                uint indicesSize = f.ReadUInt32();
                uint indicesOffset = f.ReadUInt32();

                uint unkFaceType = f.ReadUInt32();

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
                    mesh.TextureFlag = f.ReadUInt16();

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

                MaterialData = new Models.PCMPData(tGroupCount, tSubMatCount, DDSInfoCount);

                List<PCMPMaterial> materials = MaterialData.Materials;

                // Read everything backwards, it's easiest that way

                // Textures
                f.Seek(pcmpOffset + DDSInfoOffset, SeekOrigin.Begin);

                for (int t = 0; t < DDSInfoCount; t++)
                {
                    uint baseOffset = (uint)f.GetPosition() - pcmpOffset;

                    PCMPTextureInfo textureInfo = new PCMPTextureInfo() {
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
                        Unk1 = f.ReadUInt32(),
                        Unk2 = f.ReadUInt32(),
                        Unk3 = f.ReadUInt32(),
                        Unk4 = f.ReadUInt32()
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

        public ModelPackagePC(BlockData blockData) : base(blockData)
        {

        }
    }

    public class ModelPackagePC_X : ModelPackage
    {
        public override ModelPackageType Type
        {
            get { return ModelPackageType.ModelPackagePC_X; }
        }

        public override void Load()
        {
            using (BlockEditor blockEditor = new BlockEditor(BlockData))
            {
                BinaryReader f = blockEditor.Reader;

                if (f.ReadUInt32() != Magic)
                    throw new Exception("Bad magic, cannot load ModelPackage!");

                uint unknown1 = f.ReadUInt32();

                int nParts = f.ReadInt32();
                uint partsOffset = f.ReadUInt32();

                int nMeshGroups = f.ReadInt32();
                uint meshGroupsOffset = f.ReadUInt32();

                int nMeshes = f.ReadInt32();
                uint meshesOffset = f.ReadUInt32();

                // Skip padding
                f.Seek(0x8, SeekOrigin.Current);

                uint ddsOffset = f.ReadUInt32();
                uint pcmpOffset = f.ReadUInt32();

                int nIndices = f.ReadInt32();
                uint indicesSize = f.ReadUInt32();
                uint indicesOffset = f.ReadUInt32();

                uint unkFaceType = f.ReadUInt32();

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

                Meshes = new List<IndexedPrimitive>(nMeshes);

                for (int i = 0; i < nMeshes; i++)
                {
                    uint offset = (uint)f.GetPosition();

                    IndexedPrimitive mesh = new IndexedPrimitive() {
                        Offset = offset
                    };

                    Meshes.Add(mesh);

                    mesh.PrimitiveType = (D3DPRIMITIVETYPE)f.ReadInt32();

                    int unkIndex = f.ReadInt32();

                    f.Seek(0x4, SeekOrigin.Current);

                    mesh.PrimitiveCount = f.ReadInt32();

                    int indexOffset = f.ReadInt32();
                    mesh.StartIndex = (indexOffset != 0) ? ((indexOffset / 2) - 1) : 0;
                    
                    mesh.MinIndex = 0;

                    mesh.NumVertices = 0;
                    mesh.BaseVertexIndex = 0;
                    
                    // skip padding
                    f.Seek(0x1C, SeekOrigin.Current);

                    mesh.MaterialId = f.ReadInt16();
                    mesh.TextureFlag = f.ReadUInt16();

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

                //goto LoadPCMP;

                return;


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

                MaterialData = new Models.PCMPData(tGroupCount, tSubMatCount, DDSInfoCount);

                List<PCMPMaterial> materials = MaterialData.Materials;

                // Read everything backwards, it's easiest that way

                // Textures
                f.Seek(pcmpOffset + DDSInfoOffset, SeekOrigin.Begin);

                for (int t = 0; t < DDSInfoCount; t++)
                {
                    uint baseOffset = (uint)f.GetPosition() - pcmpOffset;

                    PCMPTextureInfo textureInfo = new PCMPTextureInfo() {
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
                        Unk1 = f.ReadUInt32(),
                        Unk2 = f.ReadUInt32(),
                        Unk3 = f.ReadUInt32(),
                        Unk4 = f.ReadUInt32()
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

        public ModelPackagePC_X(BlockData blockData) : base(blockData)
        {

        }
    }
}
