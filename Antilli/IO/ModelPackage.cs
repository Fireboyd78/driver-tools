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

using DSCript;
using DSCript.IO;

namespace Antilli.IO
{
    public abstract class ModelPackage
    {
        public class ModelGroup
        {
            public class Entry
            {
                public int ID { get; set; }

                public ModelGroup Parent { get; set; }

                public uint Offset { get; set; }
                public uint Unknown8 { get; set; }

                public uint RenderFlag { get; set; }

                public MeshCollection MeshCollection { get; set; }
            }

            public uint UID { get; set; }
            public uint Handle { get; set; }

            /// <summary>An unknown int relatively offset @ 0x18</summary>
            public int Unknown18 { get; set; }

            /// <summary>An unknown int relatively offset @ 0x1C</summary>
            public int Unknown1C { get; set; }

            /// <summary>An unknown list of vec4's that begins relatively @ 0x28 and has a size of 0x80</summary>
            public List<Vector4> UnknownFloats { get; protected set; }

            public List<Entry> Groups { get; protected set; }

            public ModelGroup()
            {
                UnknownFloats = new List<Vector4>(8);
                Groups = new List<Entry>(7);
            }
        }

        public class MeshCollection
        {
            public ModelGroup.Entry Parent { get; set; }

            public uint Offset { get; set; }

            public List<MeshEntry> Meshes { get; protected set; }

            public MeshCollection(int count)
            {
                Meshes = new List<MeshEntry>(count);
            }
        }

        public class MeshEntry
        {
            public uint Magic
            {
                get { return 0x5; }
            }

            public MeshCollection Parent { get; set; }

            public int VOffset { get; set; }

            public int VIndex { get; set; }
            public int VCount { get; set; }
            public int TIndex { get; set; }
            public int TCount { get; set; }

            public int MaterialId { get; set; }
            public int TextureFlag { get; set; }
        }

        public class TriangleFace
        {
            public int Point1 { get; set; }
            public int Point2 { get; set; }
            public int Point3 { get; set; }

            public TriangleFace(int p1, int p2, int p3)
            {
                Point1 = p1;
                Point2 = p2;
                Point3 = p3;
            }
        }

        public class Mesh
        {
            public IList<VertexOld> Vertices { get; set; }
            public IList<TriangleFace> Faces { get; set; }

            public MeshEntry Data { get; set; }

            public Mesh(MeshEntry data, IList<VertexOld> vertexBuffer, IList<int> indexBuffer)
            {
                Data = data;

                Vertices = new List<VertexOld>();
                Faces = new List<TriangleFace>();

                for (int v = 0; v <= Data.VCount; v++)
                    Vertices.Add(vertexBuffer[v + Data.VIndex + Data.VOffset]);

                for (int i = 0; i < Data.TCount; i++)
                {
                    int i0, i1, i2;

                    if (i % 2 == 1.0)
                    {
                        i0 = indexBuffer[i + Data.TIndex];
                        i1 = indexBuffer[(i + 1) + Data.TIndex];
                        i2 = indexBuffer[(i + 2) + Data.TIndex];
                    }
                    else
                    {
                        i0 = indexBuffer[(i + 2) + Data.TIndex];
                        i1 = indexBuffer[(i + 1) + Data.TIndex];
                        i2 = indexBuffer[i + Data.TIndex];
                    }

                    if ((i0 != i1) && (i0 != i2) && (i1 != i2))
                        Faces.Add(new TriangleFace((i0 - Data.VIndex), (i1 - Data.VIndex), (i2 - Data.VIndex)));
                }
            }
        }

        public enum FVFType : int
        {
            /// <summary>Contains data for Position, Normals, Mapping, and Specular.</summary>
            Vertex12 = 0x30,
            
            /// <summary>Contains data for Position, Normals, Mapping, Blend Weights, and Specular.</summary>
            Vertex15 = 0x3C,

            /// <summary>Contains data for Position, Normals, Mapping, Blend Weights, Specular, and Unknown.</summary>
            Vertex16 = 0x40,

            /// <summary>Represents an unknown type.</summary>
            Unknown
        }

        public class VertexData
        {
            public int Count { get; set; }

            public uint Size { get; set; }
            public uint Offset { get; set; }

            public int VLength { get; set; }

            public FVFType VertexType
            {
                get
                {
                    return Enum.IsDefined(typeof(FVFType), VLength) ? (FVFType)VLength : FVFType.Unknown;
                }
            }

            public IList<VertexOld> Buffer { get; set; }

            /// <summary>Provides direct-access to the buffer for retrieving data. Indexes cannot be set.</summary>
            /// <param name="idx">Index into buffer</param>
            public VertexOld this[int idx]
            {
                get
                {
                    return Buffer[idx];
                }
            }

            public VertexData()
            {
                Buffer = new List<VertexOld>();
            }
        }

        public class IndexData
        {
            public int Count { get; set; }

            public uint Size { get; set; }
            public uint Offset { get; set; }

            public IList<int> Buffer { get; set; }

            /// <summary>Provides direct-access to the buffer for retrieving data. Indexes cannot be set.</summary>
            /// <param name="idx">Index into buffer</param>
            public int this[int idx]
            {
                get
                {
                    return Buffer[idx];
                }
            }

            public IndexData()
            {
                Buffer = new List<int>();
            }
        }

        /// <summary>Gets or sets the Block associated with this Model Package</summary>
        public SubChunkBlock Block { get; set; }

        /// <summary>An unsigned-integer representing the header of a Model Package</summary>
        public virtual uint Magic
        {
            get { return 0; }
        }

        public List<ModelGroup> Groups { get; set; }
        public List<MeshCollection> MeshCollections { get; set; }
        public List<MeshEntry> MeshEntries { get; set; }

        public List<Mesh> Meshes { get; set; }

        public VertexData Vertex { get; set; }
        public IndexData Index { get; set; }

        public uint DDSOffset { get; set; }
        public uint PCMPOffset { get; set; }
    }

    /// <summary>A class representing the model format found in DRIV3R</summary>
    public class ModelPackagePC : ModelPackage
    {
        public override uint Magic
        {
            get
            {
                return 0x6;
            }
        }

        /// <summary>Gets or sets the type of model package this is</summary>
        public MDPCTypes Type { get; set; }

        public void Read(ChunkReader chunkFile)
        {
            using (BinaryReader f = new BinaryReader(new FileStream(chunkFile.Filename, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                f.Seek(Block.BaseOffset, SeekOrigin.Begin);

                if (f.ReadUInt32() != Magic)
                    throw new Exception("Bad magic, cannot load ModelPackage!");

                uint type = f.ReadUInt32();

                Type = Enum.IsDefined((typeof(MDPCTypes)), type) ? (MDPCTypes)type : MDPCTypes.Unknown;

                int nGroups = f.ReadInt32();
                uint GroupsOffset = f.ReadUInt32();

                int nMeshCols = f.ReadInt32();
                uint MeshColsOffset = f.ReadUInt32();

                int nMeshes = f.ReadInt32();
                uint MeshesOffset = f.ReadUInt32();

                Meshes = new List<Mesh>(nMeshes);

                if (f.ReadUInt16() != (uint)Type)
                    throw new Exception("Magic check failed, cannot load ModelPackage!");

                // Skip past irrelevant stuff
                f.Seek(Block.BaseOffset + 0x28, SeekOrigin.Begin);

                DDSOffset = f.ReadUInt32();
                PCMPOffset = f.ReadUInt32();

                Index = new IndexData() {
                    Count = f.ReadInt32(),
                    Size = f.ReadUInt32(),
                    Offset = f.ReadUInt32()
                };

                if (f.ReadUInt32() != 0x1)
                    Console.WriteLine("Unknown face type check failed, errors may occur.");

                uint vHdrOffset = f.ReadUInt32();

                // seek to vertex header
                f.Seek(Block.BaseOffset + vHdrOffset, SeekOrigin.Begin);

                Vertex = new VertexData() {
                    Count = f.ReadInt32(),
                    Size = f.ReadUInt32(),
                    Offset = f.ReadUInt32(),
                    VLength = f.ReadInt32()
                };

                /* ------------------------------
                 * Read indices
                 * ------------------------------ */

                f.Seek(Block.BaseOffset + Index.Offset, SeekOrigin.Begin);

                for (int i = 0; i < Index.Count; i++)
                    Index.Buffer.Add(f.ReadUInt16());

                if (Index.Buffer.Count == Index.Count)
                    Console.WriteLine("Finished reading {0} index entries.", Index.Buffer.Count);
                else
                    Console.WriteLine("The number of indicies does not match the defined count. This may cause unknown errors.");

                /* ------------------------------
                 * Read vertices
                 * ------------------------------ */

                f.Seek(Block.BaseOffset + Vertex.Offset, SeekOrigin.Begin);

                for (int i = 0; i < Vertex.Count; i++)
                {
                    switch (Vertex.VertexType)
                    {
                    case FVFType.Vertex12:
                        {
                            VertexOld vert = new VertexOld();

                            vert.Position.X = f.ReadSingle();
                            vert.Position.Y = f.ReadSingle();
                            vert.Position.Z = f.ReadSingle();

                            vert.Normals.X = f.ReadSingle();
                            vert.Normals.Y = f.ReadSingle();
                            vert.Normals.Z = f.ReadSingle();

                            vert.UVMap.U = f.ReadSingle();
                            vert.UVMap.V = f.ReadSingle();

                            vert.Specular.R = f.ReadSingle();
                            vert.Specular.G = f.ReadSingle();
                            vert.Specular.B = f.ReadSingle();
                            vert.Specular.A = f.ReadSingle();

                            Vertex.Buffer.Add(vert);
                        }
                        break;
                    case FVFType.Vertex15:
                        {
                            Vertex15 vert = new Vertex15();

                            vert.Position.X = f.ReadSingle();
                            vert.Position.Y = f.ReadSingle();
                            vert.Position.Z = f.ReadSingle();

                            vert.Normals.X = f.ReadSingle();
                            vert.Normals.Y = f.ReadSingle();
                            vert.Normals.Z = f.ReadSingle();

                            vert.UVMap.U = f.ReadSingle();
                            vert.UVMap.V = f.ReadSingle();

                            vert.BlendWeights.X = f.ReadSingle();
                            vert.BlendWeights.Y = f.ReadSingle();
                            vert.BlendWeights.Z = f.ReadSingle();

                            vert.Specular.R = f.ReadSingle();
                            vert.Specular.G = f.ReadSingle();
                            vert.Specular.B = f.ReadSingle();
                            vert.Specular.A = f.ReadSingle();

                            Vertex.Buffer.Add(vert);
                        }
                        break;
                    case FVFType.Vertex16:
                        {
                            Vertex16 vert = new Vertex16();

                            vert.Position.X = f.ReadSingle();
                            vert.Position.Y = f.ReadSingle();
                            vert.Position.Z = f.ReadSingle();

                            vert.Normals.X = f.ReadSingle();
                            vert.Normals.Y = f.ReadSingle();
                            vert.Normals.Z = f.ReadSingle();

                            vert.UVMap.U = f.ReadSingle();
                            vert.UVMap.V = f.ReadSingle();

                            vert.BlendWeights.X = f.ReadSingle();
                            vert.BlendWeights.Y = f.ReadSingle();
                            vert.BlendWeights.Z = f.ReadSingle();

                            vert.Specular.R = f.ReadSingle();
                            vert.Specular.G = f.ReadSingle();
                            vert.Specular.B = f.ReadSingle();
                            vert.Specular.A = f.ReadSingle();

                            vert.Unknown = f.ReadSingle();

                            Vertex.Buffer.Add(vert);
                        }
                        break;
                    case FVFType.Unknown:
                        throw new Exception("Unknown vertex format, cannot read ModelPackage!");
                    }
                }

                if (Vertex.Buffer.Count == Vertex.Count)
                    Console.WriteLine("Finished reading {0} vertex entries.", Vertex.Buffer.Count);
                else
                    Console.WriteLine("The number of vertices does not match the defined count. This may cause unknown errors.");

                /* ------------------------------
                 * Read model groups
                 * ------------------------------ */

                f.Seek(Block.BaseOffset + GroupsOffset, SeekOrigin.Begin);

                Groups = new List<ModelGroup>(nGroups);

                for (int i = 0; i < Groups.Capacity; i++)
                {
                    ModelGroup group = new ModelGroup() {
                        UID = f.ReadUInt32(),
                        Handle = f.ReadUInt32()
                    };

                    // skip float padding
                    f.Seek(0x10, SeekOrigin.Current);

                    group.Unknown18 = f.ReadInt32();
                    group.Unknown1C = f.ReadInt32();

                    // skip padding
                    f.Seek(0x8, SeekOrigin.Current);

                    // read big-ass float list...
                    for (int v = 0; v < group.UnknownFloats.Capacity; v++)
                        group.UnknownFloats.Add(new Vector4(
                            f.ReadSingle(),
                            f.ReadSingle(),
                            f.ReadSingle(),
                            f.ReadSingle()
                            ));

                    for (int k = 0; k < group.Groups.Capacity; k++)
                    {
                        ModelGroup.Entry entry = new ModelGroup.Entry() {
                            ID = k
                        };

                        entry.Offset = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x4, SeekOrigin.Current);

                        entry.Unknown8 = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x8, SeekOrigin.Current);

                        entry.RenderFlag = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x8, SeekOrigin.Current);

                        entry.Parent = group;
                        group.Groups.Add(entry);
                    }

                    Groups.Add(group);
                }

                /* ------------------------------
                 * Read mesh collections
                 * ------------------------------ */

                for (int i = 0; i < Groups.Count; i++)
                {
                    for (int k = 0; k < Groups[i].Groups.Count; k++)
                    {
                        ModelGroup.Entry entry = Groups[i].Groups[k];

                        if (entry.Offset == 0)
                            break;

                        f.Seek(Block.BaseOffset + entry.Offset, SeekOrigin.Begin);

                        uint mOffset = f.ReadUInt32();

                        // skip padding
                        f.Seek(0x44, SeekOrigin.Current);

                        short count = f.ReadInt16();

                        // skip unknown short
                        f.Seek(0x2, SeekOrigin.Current);

                        entry.MeshCollection = new MeshCollection(count){
                            Offset = mOffset,
                            Parent = entry
                        };

                        // seek to mesh collection
                        f.Seek(Block.BaseOffset + mOffset, SeekOrigin.Begin);

                        for (int j = 0; j < entry.MeshCollection.Meshes.Capacity; j++)
                        {
                            MeshEntry mesh = new MeshEntry();

                            if (f.ReadUInt32() != mesh.Magic)
                                throw new Exception("Bad mesh entry magic, cannot load ModelPackage!");

                            mesh.VOffset = f.ReadInt32();
                            mesh.VIndex = f.ReadInt32();
                            mesh.VCount = f.ReadInt32();

                            mesh.TIndex = f.ReadInt32();
                            mesh.TCount = f.ReadInt32();

                            // skip padding
                            f.Seek(0x18, SeekOrigin.Current);

                            mesh.MaterialId = f.ReadInt16();
                            mesh.TextureFlag = f.ReadInt16();

                            //skip padding
                            f.Seek(0x4, SeekOrigin.Current);

                            mesh.Parent = entry.MeshCollection;
                            Meshes.Add(new Mesh(mesh, Vertex.Buffer, Index.Buffer));
                            
                            entry.MeshCollection.Meshes.Add(mesh);
                        }
                    }
                }

                Console.WriteLine("Done!");



            }
        }

        /// <summary>Creates a new instance of the ModelPackage class to allow reading/manipulation of DRIV3R models</summary>
        /// <param name="modelPackage">The Block representing the Model Package's data</param>
        public ModelPackagePC(SubChunkBlock modelPackage)
        {
            if (modelPackage.Type != ChunkType.ModelPackagePC)
                throw new Exception("This is not a ModelPackage and cannot be loaded.");

            Block = modelPackage;
        }
    }
}
