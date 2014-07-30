using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows.Media;
using System.Windows.Media.Media3D;

using DSCript;
using DSCript.Spooling;

using D3DPRIMITIVETYPE = DSCript.Models.D3DPRIMITIVETYPE;
using FVFType = DSCript.Models.FVFType;

using IndexData = DSCript.Models.IndexData;

using Vertex = DSCript.Models.Vertex;
using VertexData = DSCript.Models.VertexData;

using PCMPData = DSCript.Models.PCMPData;
using PCMPMaterial = DSCript.Models.PCMPMaterial;
using PCMPSubMaterial = DSCript.Models.PCMPSubMaterial;
using PCMPTexture = DSCript.Models.PCMPTexture;

namespace Antilli
{
    public class ModelHierarchy : SpoolableResource<SpoolableBuffer>
    {
        protected override void Load()
        {
            throw new NotImplementedException();
        }

        protected override void Save()
        {
            throw new NotImplementedException();
        }
    }

    public sealed class Driv3rModel : SpoolableResource<SpoolableBuffer>
    {
        public class PartsGroup
        {
            public int UID { get; set; }
            public int Handle { get; set; }

            /// <summary>
            /// Gets or sets the Vertex Buffer to use when accessing vertices.
            /// </summary>
            public VertexData VertexBuffer { get; set; }

            public short Unknown1 { get; set; }
            public int Unknown2 { get; set; }

            public List<Point4D> Transform { get; set; }
            public List<PartDefinition> Parts { get; set; }

            public PartsGroup()
            {
                Parts = new List<PartDefinition>(7);
                Transform = new List<Point4D>(8);
            }
        }

        public class PartDefinition
        {
            public int ID { get; set; }

            public uint Unknown { get; set; }
            public uint Reserved { get; set; }

            public PartsGroup Parent { get; set; }
            public MeshGroup Group { get; set; }

            public PartDefinition(int id)
            {
                ID = id;
            }
        }

        public class MeshGroup
        {
            public PartDefinition Parent { get; set; }

            public List<MeshDefinition> Meshes { get; set; }

            public MeshGroup(int nMeshes)
            {
                Meshes = new List<MeshDefinition>(nMeshes);
            }
        }

        public class MeshDefinition
        {
            public Driv3rModel ModelPackage { get; set; }

            public IndexData IndexBuffer
            {
                get { return ModelPackage.Indices; }
            }

            public VertexData VertexBuffer
            {
                // this is just ridiculous :/
                get { return Parent.Parent.Parent.VertexBuffer; }
            }

            public MeshGroup Parent { get; set; }

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
            public int MinIndex { get; set; }

            /// <summary>
            /// Number of vertices used during this call. The first vertex is located at index: BaseVertexIndex + MinIndex.
            /// </summary>
            public int NumVertices { get; set; }

            /// <summary>
            /// Index of the first index to use when accessing the vertex buffer. Beginning at StartIndex to index vertices from the vertex buffer.
            /// </summary>
            public int StartIndex { get; set; }

            /// <summary>
            /// Number of primitives to render. The number of vertices used is a function of the primitive count and the primitive type.
            /// </summary>
            public int PrimitiveCount { get; set; }

            /// <summary>
            /// The material used for this mesh.
            /// </summary>
            public int MaterialId { get; set; }

            /// <summary>
            /// The UID of the package containing the material.
            /// </summary>
            public int SourceUID { get; set; }

            public PCMPMaterial GetMaterial()
            {
                //if (ModelFile != null)
                //{
                //    if (SourceUID == (uint)PackageType.VehicleGlobals && ModelFile.HasSpooledFile)
                //        return ModelFile.SpooledFile.StandaloneTextures[MaterialId];
                //}
                //
                //if (SourceUID == ModelPackage.UID || SourceUID == 0xFFFD)
                //{
                //    return ModelPackage.MaterialData.Materials[MaterialId];
                //}
                //else if (ModelFile != null)
                //{
                //    ModelPackage mPak = ModelFile.Models.Find((m) => m.UID == SourceUID);
                //
                //    if (mPak != null)
                //        return mPak.MaterialData.Materials[MaterialId];
                //}

                // broken :(
                return null;
            }

            public List<Vertex> GetVertices()
            {
                var vBuffer = VertexBuffer.Buffer;

                List<Vertex> vertices = new List<Vertex>((int)NumVertices);

                for (uint v = 0; v <= NumVertices; v++)
                {
                    var vIdx = BaseVertexIndex + MinIndex + v;

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
                int nVerts = (int)NumVertices;

                for (uint v = 0; v <= NumVertices; v++)
                {
                    var vIdx = BaseVertexIndex + MinIndex + v;

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
                ushort[] indices = ModelPackage.Indices.Buffer;

                Int32Collection tris = new Int32Collection();

                for (int i = 0; i < PrimitiveCount; i++)
                {
                    var idx = StartIndex;
                    var vIdx = BaseVertexIndex;

                    uint i0, i1, i2;

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
        }

        protected override void Load()
        {
            // verify the spooler is a ModelPackagePC
            if (Spooler.Magic != (int)ChunkType.ModelPackagePC)
                throw new Exception("Cannot load model package - the spooler has an invalid magic number.");
            if (Spooler.Reserved != 6)
                throw new Exception("Cannot load model package - the reserved byte is invalid.");

            // verify the spooler isn't empty
            if (Spooler.Size == 0)
                throw new Exception("Cannot load model package - the spooler has an empty buffer.");

            using (var f = Spooler.GetMemoryStream())
            {
                if (f.ReadInt32() != 6)
                    throw new Exception("Cannot load model package - the magic number is invalid.");

                UID = f.ReadInt32();

                var parts = new {
                    Count = f.ReadInt32(),
                    Offset = f.ReadInt32()
                };

                var meshGroups = new {
                    Count = f.ReadInt32(),
                    Offset = f.ReadInt32()
                };

                var meshes = new {
                    Count = f.ReadInt32(),
                    Offset = f.ReadInt32()
                };

                var uidChk = f.ReadInt16();

                if (uidChk != UID)
                    DSC.Log("Unknown magic check failed - wanted {0}, got {1}", UID, uidChk);

                // skip junk
                f.Position += 6;

                var ddsOffset = f.ReadInt32();
                var pcmpOffset = f.ReadInt32();

                var indices = new {
                    Count = f.ReadInt32(),
                    Size = f.ReadInt32(),
                    Offset = f.ReadInt32()
                };

                var vBuffer = new {
                    Count = f.ReadInt32(),
                    Offset = f.ReadInt32()
                };

                if (vBuffer.Count > 0)
                    VertexBuffers = new List<VertexData>(vBuffer.Count);

                /* ------------------------------
                 * Read vertex buffer header(s) (Size: 0x1C)
                 * ------------------------------ */
                for (int vB = 0; vB < vBuffer.Count; vB++)
                {
                    f.Seek((vB * 0x1C), vBuffer.Offset);

                    var nVerts      = f.ReadInt32();
                    var vertsSize   = f.ReadInt32();
                    var vertsOffset = f.ReadInt32();
                    var vertLength  = f.ReadInt32();

                    var vertexBuffer = new VertexData(nVerts, vertLength);

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
                f.Seek(indices.Offset, SeekOrigin.Begin);

                Indices = new IndexData(indices.Count);

                for (int i = 0; i < indices.Count; i++)
                    Indices.Buffer[i] = f.ReadUInt16();

                /* ------------------------------
                 * Read model data
                 * ------------------------------ */
                var meshLookup  = new Dictionary<long, MeshDefinition>(meshes.Count);
                var groupLookup = new Dictionary<long, MeshGroup>(meshGroups.Count);

                Meshes      = new List<MeshDefinition>(meshes.Count);
                MeshGroups  = new List<MeshGroup>(meshGroups.Count);
                PartsGroups = new List<PartsGroup>(parts.Count);

                // To collect the data for our meshes, we will read everything backwards:
                // - 1) Meshes
                // - 2) MeshGroups
                // - 3) PartsGroups
                //
                // This will help prevent redundant loops, and everything is read once, not twice!

                /* ------------------------------
                 * Read meshes (Size: 0x38)
                 * ------------------------------ */
                f.Seek(meshes.Offset, SeekOrigin.Begin);
                for (int i = 0; i < meshes.Count; i++)
                {
                    var offset = f.Seek((i * 0x38), meshes.Offset);

                    var mesh = new MeshDefinition() {
                        ModelPackage    = this,
                        PrimitiveType   = (D3DPRIMITIVETYPE)f.ReadInt32(),
                        BaseVertexIndex = f.ReadInt32(),
                        MinIndex        = f.ReadInt32(),
                        NumVertices     = f.ReadInt32(),

                        StartIndex      = f.ReadInt32(),
                        PrimitiveCount  = f.ReadInt32(),
                    };

                    // skip padding
                    f.Seek(0x18, SeekOrigin.Current);

                    mesh.MaterialId = f.ReadInt16();
                    mesh.SourceUID  = f.ReadInt16();

                    // add to mesh lookup
                    meshLookup.Add(offset, mesh);
                    Meshes.Add(mesh);
                }

                /* ------------------------------
                 * Read mesh groups (Size: 0x58)
                 * ------------------------------ */
                for (int i = 0; i < meshGroups.Count; i++)
                {
                    var offset  = f.Seek((i * 0x58), meshGroups.Offset);
                    var mOffset = f.ReadInt32();

                    // skip padding
                    f.Seek(0x44, SeekOrigin.Current);

                    var count   = f.ReadInt16();

                    MeshGroup group = new MeshGroup(count);
                    MeshGroups.Add(group);

                    // add to mesh groups lookup
                    groupLookup.Add(offset, group);

                    // Add meshes to group
                    for (uint k = 0; k < count; k++)
                    {
                        var mesh = meshLookup[mOffset + (k * 0x38)];
                        mesh.Parent = group;

                        group.Meshes.Add(mesh);
                    }
                }

                /* ------------------------------
                 * Read parts groups (Size: 0x188)
                 * ------------------------------ */
                for (int i = 0; i < parts.Count; i++)
                {
                    long entryPoint = f.Seek((i * 0x188), parts.Offset);

                    PartsGroup part = new PartsGroup() {
                        UID     = f.ReadInt32(),
                        Handle  = f.ReadInt32()
                    };

                    PartsGroups.Add(part);

                    // skip unknown float padding
                    f.Seek(0x10, SeekOrigin.Current);

                    // INCOMING TRANSMISSION...
                    // RE: OPERATION S.T.E.R.N....
                    // ...
                    // YOUR ASSISTANCE HAS BEEN NOTED...
                    // ...
                    // <END OF TRANSMISSION>...
                    part.VertexBuffer = VertexBuffers[f.ReadInt16()];

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
                            MeshGroup mGroup = groupLookup[gOffset];

                            entry.Group = mGroup;
                            mGroup.Parent = entry;

                            // TODO: Not have such ugly code!
                            foreach (MeshDefinition mesh in mGroup.Meshes)
                                mesh.Parent = mGroup;

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
                meshLookup.Clear();
                groupLookup.Clear();

                goto LoadPCMP;

                // Read PCMP
            LoadPCMP:
                if (pcmpOffset == 0)
                    return;

                f.Seek(pcmpOffset, SeekOrigin.Begin);

                var pMagic = f.ReadInt32();

                if (pMagic != PCMPData.Magic)
                    throw new Exception("Bad textures magic, cannot load ModelPackage!");

                if (f.ReadInt32() != 0x3)
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

                var textureLookup = new Dictionary<long, PCMPTexture>(DDSInfoCount);
                var subMatLookup = new Dictionary<long, PCMPSubMaterial>(nSubMats);

                // Read backwards

                // Textures (Size: 0x20)
                for (int t = 0; t < DDSInfoCount; t++)
                {
                    var baseOffset = f.Seek((DDSInfoOffset + (t * 0x20)), pcmpOffset) - pcmpOffset;

                    PCMPTexture textureInfo = new PCMPTexture();

                    MaterialData.Textures.Add(textureInfo);

                    //add to texture lookup
                    textureLookup.Add(baseOffset, textureInfo);

                    textureInfo.Reserved = f.ReadUInt32();
                    textureInfo.CRC32 = f.ReadUInt32();

                    uint offset         = f.ReadUInt32();
                    int size            = f.ReadInt32();

                    textureInfo.Type = f.ReadUInt32();

                    textureInfo.Width = f.ReadUInt16();
                    textureInfo.Height = f.ReadUInt16();

                    textureInfo.Unk5 = f.ReadUInt32();
                    textureInfo.Unk6 = f.ReadUInt32();

                    // get DDS from absolute offset (defined in MDPC header)
                    f.Seek(offset, ddsOffset);
                    textureInfo.Buffer = f.ReadBytes(size);
                }

                // Submaterials (Size: 0x20)
                for (int s = 0; s < nSubMats; s++)
                {
                    var baseOffset = f.Seek((subMatsOffset + (s * 0x20)), pcmpOffset) - pcmpOffset;

                    PCMPSubMaterial subMaterial = new PCMPSubMaterial() {
                        Flags = f.ReadUInt32(),
                        Mode = f.ReadUInt16(),
                        Type = f.ReadUInt16()
                    };

                    MaterialData.SubMaterials.Add(subMaterial);

                    //add to submaterial lookup
                    subMatLookup.Add(baseOffset, subMaterial);

                    f.Seek(0x8, SeekOrigin.Current);

                    // table info
                    uint offset = f.ReadUInt32();
                    uint count = f.ReadUInt32();

                    // get texture from table
                    for (int t = 0; t < count; t++)
                    {
                        f.Seek((offset + (t * 0x8)), pcmpOffset);
                        subMaterial.Textures.Add(textureLookup[f.ReadUInt32()]);
                    }
                }

                // Materials (Size: 0x18)
                for (int m = 0; m < nMats; m++)
                {
                    f.Seek((matsOffset + (m * 0x18)), pcmpOffset);

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
                        f.Seek((offset + (s * 0x8)), pcmpOffset);
                        material.SubMaterials.Add(subMatLookup[f.ReadUInt32()]);
                    }
                }

                // lookup tables no longer needed
                textureLookup.Clear();
                subMatLookup.Clear();
            }
        }

        protected override void Save()
        {
            throw new NotImplementedException();
        }

        public int UID { get; set; }

        public List<PartsGroup> PartsGroups { get; set; }
        public List<MeshGroup> MeshGroups { get; set; }
        public List<MeshDefinition> Meshes { get; set; }


        public List<VertexData> VertexBuffers { get; set; }
        public IndexData Indices { get; set; }

        public PCMPData MaterialData { get; set; }

        public bool HasMaterials
        {
            get { return (MaterialData != null); }
        }

        public bool IsLoaded
        {
            get { return (Meshes != null || HasMaterials); }
        }
    }

    public sealed class Driv3rVehicle
    {
        public ModelHierarchy Hierarchy { get; set; }
        public Driv3rModel ModelPackage { get; set; }
    }

    public class ModelFileChunker : FileChunker
    {
        public List<Driv3rModel> ModelPackages { get; private set; }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            if (sender is SpoolableBuffer && (ChunkType)sender.Magic == ChunkType.ModelPackagePC)
                ModelPackages.Add(SpoolableResourceFactory.Create<Driv3rModel>(sender));
        }

        public ModelFileChunker()
        {
            ModelPackages = new List<Driv3rModel>();
        }

        public ModelFileChunker(string filename) : this()
        {
            Load(filename);
        }
    }

    public class VehicleFileChunker : ModelFileChunker
    {
        public List<ModelHierarchy> Hierarchies { get; private set; }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            if (sender is SpoolableBuffer)
            {
                switch ((ChunkType)sender.Magic)
                {
                case ChunkType.VehicleHierarchy:
                    Hierarchies.Add(SpoolableResourceFactory.Create<ModelHierarchy>(sender));
                    break;
                
                default:
                    // default handler
                    base.OnSpoolerLoaded(sender, e);
                    break;
                }
            }
        }

        /// <summary>
        /// Returns the vehicle container chunk for the specified vehicle id, if applicable.
        /// </summary>
        /// <param name="vehicleId">The vehicle id.</param>
        /// <returns>A vehicle container chunk corresponding to the vehicle id; if nothing is found, null.</returns>
        public SpoolablePackage GetVehicleContainerChunk(int vehicleId)
        {
            // vehicle container chunks are in the root chunk
            // if they're not there, then they don't exist
            var spooler = Content.Children.FirstOrDefault((s) => s.Magic == vehicleId) as SpoolablePackage;
            return spooler;
        }

        /// <summary>
        /// Returns whether or not this is a VVV file.
        /// </summary>
        public bool IsMissionVehicleFile
        {
            get { return (Hierarchies.Count > ModelPackages.Count); }
        }

        private VehicleFileChunker() : base()
        {
            Hierarchies = new List<ModelHierarchy>();
        }

        public VehicleFileChunker(string filename) : this()
        {
            Load(filename);
        }
    }
}
