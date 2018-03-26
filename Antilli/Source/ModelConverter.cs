using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using System.Xml;
using System.Xml.Linq;

using DSCript;
using DSCript.Models;
using DSCript.Spooling;

using COLLADA;

namespace Antilli
{
    /*
        TODO: move these elsewhere
    */

    [StructLayout(LayoutKind.Sequential, Size = 0x20)]
    public struct DDSPixelFormat
    {
        public int Size;
        public int Flags;
        public int FourCC;
        public int RGBBitCount;
        public int RBitMask;
        public int GBitMask;
        public int BBitMask;
        public int ABitMask;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x7C)]
    public unsafe struct DDSHeader
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(DDSHeader));

        public int Size;
        public int Flags;
        public int Height;
        public int Width;
        public int PitchOrLinearSize;
        public int Depth;
        public int MipMapCount;

        private fixed int Reserved1[11];

        public DDSPixelFormat PixelFormat;

        public int Caps;
        public int Caps2;
        public int Caps3;
        public int Caps4;

        private int Reserved2;
    }

    public static class DDSUtils
    {
        // ugly pink 8x8 texture (DXT1)
        private static readonly byte[] NullTex = {
            0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 0x07, 0x10, 0x08, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x44, 0x58, 0x54, 0x31, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0xF8, 0x17, 0xF8,
            0xFF, 0xFF, 0xFF, 0xFF, 0x18, 0xF8, 0x17, 0xF8, 0xFF, 0xFF, 0xFF, 0xFF,
            0x18, 0xF8, 0x17, 0xF8, 0xFF, 0xFF, 0xFF, 0xFF, 0x18, 0xF8, 0x17, 0xF8,
            0xFF, 0xFF, 0xFF, 0xFF
        };

        private static readonly byte[] RGBHeader = {
            0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 0x07, 0x10, 0x08, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00,
            0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        
        public static byte[] GetNullTex()
        {
            var result = new byte[NullTex.Length];
            Buffer.BlockCopy(NullTex, 0, result, 0, result.Length);

            return result;
        }

        public static byte[] MakeRGBATexture(Vector4 color)
        {
            var pixels = new byte[0x100];

            var r = (byte)(color.X * 255.999f);
            var g = (byte)(color.Y * 255.999f);
            var b = (byte)(color.Z * 255.999f);
            var a = (byte)(color.W * 255.999f);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i + 0] = b;
                pixels[i + 1] = g;
                pixels[i + 2] = r;
                pixels[i + 3] = a;
            }

            var result = new byte[RGBHeader.Length + pixels.Length];

            Buffer.BlockCopy(RGBHeader, 0, result, 0, RGBHeader.Length);
            Buffer.BlockCopy(pixels, 0, result, RGBHeader.Length, pixels.Length);

            return result;
        }

        public static bool GetHeaderInfo(byte[] buffer, ref DDSHeader header)
        {
            using (var ms = new MemoryStream(buffer))
            {
                if (ms.ReadInt32() == 0x20534444)
                {
                    var length = DDSHeader.SizeOf;

                    var data = new byte[length];
                    var ptr = GCHandle.Alloc(header, GCHandleType.Pinned);

                    ms.Read(data, 0, length);
                    Marshal.Copy(data, 0, ptr.AddrOfPinnedObject(), length);

                    return true;
                }
            }

            // not a valid DDS texture
            return false;
        }

        public static int GetTextureType(int fourCC)
        {
            switch (fourCC)
            {
            case 0x31545844: return 1;
            case 0x32545844: return 2;
            case 0x33545844: return 3;
            case 0x35545844: return 5;
            // RGB[:A] (non-spec)
            case 0:
                return 128;
            default:
                return 0;
            }
        }
    }

    public enum EffectType
    {
        Static,
        Vehicle,
        Character,
    }
    
    public class ModelConverter
    {
        // yes, it's just a fancy VertexBuffer :P
        public struct VerticesHolder
        {
            public Vector3[] Positions;
            public Vector3[] Normals;
            public Vector2[] TexCoords;

            public bool HasPositions
            {
                get { return Positions != null; }
            }

            public bool HasNormals
            {
                get { return Normals != null; }
            }

            public bool HasTexCoords
            {
                get { return TexCoords != null; }
            }
            
            private static Vector2 ReadVector2(COLLADAFloatArray array, int index)
            {
                return new Vector2() {
                    X = array.Data[index + 0],
                    Y = array.Data[index + 1],
                };
            }

            private static Vector3 ReadVector3(COLLADAFloatArray array, int index)
            {
                // HACK: assume we need to correct the directions!
                return new Vector3() {
                    X = -array.Data[index + 0],
                    Y = array.Data[index + 2],
                    Z = array.Data[index + 1],
                };
            }

            private static void ReadVectorData<T>(COLLADASource source, Func<COLLADAFloatArray, int, T> fnReadData, out T[] data)
            {
                var array = source.Array;
                var accessor = source.Technique.Accessor;

                var stride = accessor.Stride;

                data = new T[accessor.Count];

                var vIdx = 0;

                for (int v = 0; v < array.Count; v += stride)
                {
                    data[vIdx++] = fnReadData(array, v);
                }
            }

            public VertexData CreateVertex(VertexBuffer vertexBuffer, PolyMesh.Primitive prim)
            {
                var vertex = vertexBuffer.CreateVertex();

                if (HasPositions)
                    vertex.SetData(VertexUsageType.Position, 0, Positions[prim.Position]);
                if (HasNormals)
                    vertex.SetData(VertexUsageType.Normal, 0, Normals[prim.Normal]);
                if (HasTexCoords)
                    vertex.SetData(VertexUsageType.TextureCoordinate, 0, TexCoords[prim.TexCoord]);

                return vertex;
            }

            public static VerticesHolder Create(COLLADAMesh mesh)
            {
                var holder = new VerticesHolder();

                // this isn't how it's supposed to work,
                // but I'll probably fix this eventually (or not)
                foreach (var source in mesh.Sources)
                {
                    var sourceId = source.Info.Id;
                    var accessor = source.Technique.Accessor;

                    var count = accessor.Count;
                    var stride = accessor.Stride;

                    if (sourceId.Contains("-positions"))
                    {
                        ReadVectorData(source, ReadVector3, out holder.Positions);
                    }
                    else if (sourceId.Contains("-normals"))
                    {
                        ReadVectorData(source, ReadVector3, out holder.Normals);
                    }
                    else if (sourceId.Contains("-map"))
                    {
                        ReadVectorData(source, ReadVector2, out holder.TexCoords);
                    }
                    else
                    {
                        Debug.WriteLine($"WARNING: Could not determine source type for '{sourceId}', skipping...");
                    }
                }

                return holder;
            }
        }

        public struct PolyMesh
        {
            public struct Primitive
            {
                public int Position;
                public int Normal;
                public int TexCoord;
            }

            public Primitive[] Primitives;
            
            public int MaterialId;

            public int NumPrimitives;

            public static PolyMesh Create(COLLADAPolyList polyList)
            {
                var count = polyList.Count;

                var sPos = -1;
                var sNor = -1;
                var sTex = -1;
                
                foreach (var input in polyList.Inputs)
                {
                    var offset = input.Offset;

                    switch (offset)
                    {
                    case 0:
                        sPos = offset;
                        break;
                    case 1:
                        sNor = offset;
                        break;
                    case 2:
                        if (input.Set == 0)
                            sTex = offset;
                        break;
                    }
                }

                var hasPositions = (sPos != -1);
                var hasNormals = (sNor != -1);
                var hasTexCoords = (sTex != -1);

                if (!hasPositions || !hasNormals)
                    throw new InvalidOperationException("Can't find required semantic(s) in poly list!");
                
                var prims = new Primitive[count * 3];
                var minIndex = 0;

                for (int p = 0; p < count; p++)
                {
                    var vc = polyList.VCount[p];

                    if (vc != 3)
                        throw new InvalidOperationException("Non-triangulated poly list!");

                    var idx = (p * vc);

                    for (int t = 0; t < vc; t++)
                    {
                        var prim = new Primitive() {
                            Position = polyList.Indices[minIndex + sPos],
                            Normal = polyList.Indices[minIndex + sNor],
                        };

                        if (hasTexCoords)
                            prim.TexCoord = polyList.Indices[minIndex + sTex];

                        prims[idx + t] = prim;

                        minIndex += polyList.Inputs.Count;
                    }
                }

                return new PolyMesh() {
                    Primitives = prims,
                    NumPrimitives = count,
                    MaterialId = -1, // apply after
                };
            }
        }

        public class IndicesHolder
        {
            public List<short> Indices = new List<short>();

            public int Stride
            {
                get { return 3; }
            }
            
            public int Count
            {
                get { return Indices.Count; }
            }

            public int NumPrimitives
            {
                get { return (Count * Stride); }
            }

            public IndexData Compile()
            {
                return new IndexData(Count) {
                    Buffer = Indices.ToArray(),
                };
            }
        }
        
        public class GeometryHolder
        {
            public VerticesHolder Vertices;

            public List<PolyMesh> Meshes;

            public MeshGroup SubModel;

            public void LoadMesh(COLLADAMesh colladaMesh, COLLADAMaterialLibrary mtlLib)
            {
                Meshes = new List<PolyMesh>();

                Vertices = VerticesHolder.Create(colladaMesh);

                foreach (var polyList in colladaMesh.PolyLists)
                {
                    var polyMesh = PolyMesh.Create(polyList);

                    // assign material
                    polyMesh.MaterialId = mtlLib.Materials.FindIndex((m) => m.Info.Id == polyList.Material);

                    if (polyMesh.MaterialId == -1)
                    {
                        Debug.WriteLine($"WARNING: material '{polyList.Material}' was not found! Defaulting to 0");
                        polyMesh.MaterialId = 0;
                    }

                    Meshes.Add(polyMesh);
                }
            }

            public List<MeshDefinition> CompileMeshes(VertexBuffer vertexBuffer, IndicesHolder indexBuffer, bool shadowModelHack)
            {
                var meshes = new List<MeshDefinition>();

                var vertices = vertexBuffer.Vertices;
                var indices = indexBuffer.Indices;

                var lodInstance = SubModel.Parent;
                var model = lodInstance.Parent;

                // reset the old meshes (causes crash if we don't do this)
                SubModel.Meshes = new List<MeshDefinition>();
                
                foreach (var polyMesh in Meshes)
                {
                    var primitives = polyMesh.Primitives;
                    var numPrims = polyMesh.NumPrimitives;

                    var vertexOffset = vertexBuffer.Count;
                    var indexOffset = indexBuffer.Count;

                    for (int p = 0; p < numPrims; p++)
                    {
                        var primIdx = (p * 3);

                        var tmp = new short[3];

                        for (int t = 0; t < 3; t++)
                        {
                            var prim = primitives[primIdx + t];

                            var vertex = Vertices.CreateVertex(vertexBuffer, prim);
                            var index = (short)vertices.Count;

                            var vIdx = vertices.FindIndex(vertexOffset, (v) => {
                                return v.PossiblyEqual(ref vertex);
                            });

                            if (vIdx != -1)
                            {
                                // reuse existing data
                                //Debug.WriteLine($"{index} -> {vertexOffset}[{vIdx}]");

                                index = (short)vIdx;
                            }
                            else
                            {
                                vertices.Add(vertex);
                            }

                            tmp[t] = index;
                        }

                        indices.Add(tmp[0]);
                        indices.Add(tmp[1]);
                        indices.Add(tmp[2]);
                    }

                    var mesh = new MeshDefinition() {
                        PartsGroup = model,
                        MeshGroup = SubModel,

                        PrimitiveType = PrimitiveType.TriangleList,

                        VertexOffset = vertexOffset,
                        VertexCount = (vertices.Count - vertexOffset),

                        IndexOffset = indexOffset,
                        IndexCount = numPrims,

                        MaterialId = polyMesh.MaterialId,
                        SourceUID = 0xFFFD, // assume it's a self-contained vehicle texture
                    };

                    if (shadowModelHack)
                    {
                        mesh.VertexOffset = 0;
                        mesh.VertexCount = 0;
                        mesh.IndexOffset = 0;
                        mesh.IndexCount = 0;
                    }

                    //for (int i = 0; i < numPrims; i++)
                    //{
                    //    var offset = indexOffset + (i * 3);
                    //
                    //    int i0 = indices[offset];
                    //    int i1 = indices[offset + 1];
                    //    int i2 = indices[offset + 2];
                    //
                    //    Debug.WriteLine($"t {i0} {i1} {i2}");
                    //}
                    //
                    //Debug.WriteLine("----");

                    meshes.Add(mesh);
                }

                SubModel.Meshes.AddRange(meshes);

                return meshes;
            }

            public GeometryHolder()
            {
                Meshes = new List<PolyMesh>();
            }

            public GeometryHolder(MeshGroup subModel)
                : base()
            {
                SubModel = subModel;
            }
        }

        public class MaterialHolder
        {
            public string Name;
            public string TextureFile;

            public Vector4 Color;
            
            public MaterialHolder()
            {
                Name = String.Empty;
                TextureFile = String.Empty;

                Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            }
        }

        public string UID { get; set; }

        public EffectType EffectType { get; set; }

        public List<PartsGroup> Models { get; set; }
        public List<MeshGroup> SubModels { get; set; }

        public List<GeometryHolder> Geometries { get; set; }
        public List<MaterialHolder> Materials { get; set; }
        
        private static int GetVertexDeclType(EffectType effectType)
        {
            switch (effectType)
            {
            case EffectType.Vehicle:
                return 5;
            }

            throw new NotImplementedException($"Effect type '{effectType.ToString()}' is not implemented yet.");
        }

        private static int GetPartSlotIndex(string slotType)
        {
            switch (slotType)
            {
            case "H":       return 0;
            case "M":       return 1;
            case "L":       return 2;
            case "VL":      return 4;
            case "SHADOW":  return 5;
            }

            return -1;
        }

        private static int GetPartSlotType(string slotType)
        {
            switch (slotType)
            {
            case "H":       return 0x1E;
            case "M":       return 0x1C;
            case "L":       return 0x18;
            case "VL":      return 1;
            case "SHADOW":  return 0;
            }

            return -1;
        }
        
        private static Vector4 GetVectorFromTransform(float[] transform, int offset)
        {
            return new Vector4() {
                X = transform[offset + 0],
                Y = transform[offset + 1],
                Z = transform[offset + 2],
                W = transform[offset + 3],
            };
        }

        private static byte[] GetBufferFromHexString(string input)
        {
            var result = new byte[input.Length / 2];

            int offset = (input.Length - 2);
            var idx = 0;

            while (offset >= 0)
            {
                var val = input.Substring(offset, 2);

                result[idx++] = byte.Parse(val, NumberStyles.HexNumber);

                offset -= 2;
            }

            return result;
        }

        public bool LoadCOLLADA(COLLADADocument collada)
        {
            COLLADAVisualScene sceneRoot = null;

            foreach (var scene in collada.SceneLibrary.Scenes)
            {
                if (scene.Info.Id == "Scene")
                {
                    sceneRoot = scene;
                    break;
                }
            }

            if (sceneRoot == null)
                throw new InvalidOperationException("Could not find scene root!");

            if (sceneRoot.Nodes.Count != 1)
                throw new InvalidOperationException("Invalid scene -- expected a single root node");

            var rootNode = sceneRoot.Nodes[0];
            var rootId = rootNode.Info.Id;
            
            if (!rootId.StartsWith("root_"))
                throw new InvalidOperationException("Root node in scene does not conform to naming convention.");

            UID = rootId.Split('_')[1].PadLeft(16, '0');
            
            Debug.WriteLine($"Model UID: {UID}");

            if (rootNode.GeometryInstances.Count != 0)
                throw new InvalidOperationException("Root node cannot contain geometry.");

            var partIdx = 0;
            
            foreach (var childPart in rootNode.Children)
            {
                if (childPart.GeometryInstances.Count != 0)
                    throw new InvalidOperationException($"Child {partIdx} ('{childPart.Info.Id}') in root node is malformed.");

                var partsGroup = new PartsGroup() {
                    VertexType = -1
                };

                var parts = partsGroup.Parts;

                Models.Add(partsGroup);

                var geomQueue = new COLLADAGeometryInstance[7];

                foreach (var lodInstance in childPart.Children)
                {
                    var objName = lodInstance.Info.Id;

                    if (lodInstance.GeometryInstances.Count != 1)
                    {
                        Debug.WriteLine($"WARNING: Non-mesh object '{objName}' in child {partIdx} ('{childPart.Info.Id}'), skipping...");
                        continue;
                    }

                    var slotType = objName.Split('_').Last();

                    var slotIdx = GetPartSlotIndex(slotType);

                    if (slotIdx == -1)
                        throw new InvalidOperationException($"Could not determine slot index for '{objName}'!");
                    
                    // we need to build the buffers in the proper order
                    // so we'll process them after we get the basic layout setup
                    geomQueue[slotIdx] = lodInstance.GeometryInstances[0];

                    var partDef = new PartDefinition(slotIdx) {
                        Parent = partsGroup,
                        Type = GetPartSlotType(slotType),
                    };

                    parts[slotIdx] = partDef;

                    var subModel = new MeshGroup() {
                        Parent = partDef
                    };

                    partDef.Groups.Add(subModel);
                    
                    var transform = lodInstance.Matrix.Values;

                    for (int t = 0; t < 4; t++)
                        subModel.Transform[t] = GetVectorFromTransform(transform, (t * 4));
                }

                // gotta make yet another fucking queue
                var geomLists = new List<GeometryHolder>[7];

                for (int i = 0; i < 7; i++)
                    geomLists[i] = new List<GeometryHolder>();

                for (int p = 0; p < 7; p++)
                {
                    var part = partsGroup.Parts[p];

                    // nothing to see here
                    if (part == null)
                        continue;

                    var subModel = part.Groups[0];

                    // correct order
                    SubModels.Add(subModel);

                    var geomInst = geomQueue[p];

                    // very bad!
                    if (geomInst == null)
                        throw new InvalidOperationException($"Geometry is MISSING for part {p + 1}!");

                    var geom = collada.GeometryLibrary[geomInst.Url];

                    if (geom == null)
                        throw new InvalidOperationException($"Geometry URL '{geomInst.Url}' not found!");

                    var mesh = geom.Mesh;
                    var geometry = new GeometryHolder(subModel);

                    geometry.LoadMesh(mesh, collada.MaterialLibrary);

                    geomLists[p].Add(geometry);
                }

                foreach (var geomList in geomLists)
                {
                    Geometries.AddRange(geomList);
                    Geometries.Add(null);
                }
            }

            Materials = new List<MaterialHolder>();

            // now parse materials in ascending order (material id's were already assigned by this point)
            foreach (var material in collada.MaterialLibrary.Materials)
            {
                var mtl = new MaterialHolder() {
                    Name = material.Info.Name
                };

                var effect = collada.EffectLibrary.Effects.Find((e) => e.Info.Id == material.EffectInstance.Url.Substring(1));

                var profile = effect.Profile;
                var technique = profile.Technique;
                var shader = technique.Shader;

                // find texture...
                // (did I mention COLLADA is a pain in the ass?)
                if (!String.IsNullOrEmpty(shader.Diffuse.Texture))
                {
                    var sm = profile.Params.Find((p) => p.SubId == shader.Diffuse.Texture)?.Param as COLLADASampler2DParamValue;

                    if (sm != null)
                    {
                        var sampler = sm.Value;
                        var sr = profile.Params.Find((n) => n.SubId == sampler.Source)?.Param as COLLADASurfaceParamValue;

                        if (sr != null)
                        {
                            var surface = sr.Value;

                            if (surface.Type == "2D")
                            {
                                var im = collada.ImageLibrary.Images.Find((g) => g.Id == surface.InitFrom);

                                mtl.TextureFile = Path.GetFullPath(im.InitFrom);
                            }
                        }
                    }
                }
                else
                {
                    // try making a texture based on the color ;)
                    mtl.Color = shader.Diffuse.Color;
                }

                Materials.Add(mtl);
            }

            return true;
        }
        
        public ModelPackagePC ToModelPackage()
        {
            var modelPackage = SpoolableResourceFactory.Create<ModelPackagePC>();
            
            var vDeclType = GetVertexDeclType(EffectType);

            var vertexBuffer = VertexBuffer.CreateD3Buffer(vDeclType);
            var indexBuffer = new IndicesHolder();

            var meshList = new List<MeshDefinition>();

            var vertexBaseOffset = vertexBuffer.Count;
            
            foreach (var geom in Geometries)
            {
                if (geom == null)
                {
                    vertexBaseOffset = vertexBuffer.Count;
                    continue;
                }

                var subModel = geom.SubModel;
                var lodInstance = subModel.Parent;
                var model = lodInstance.Parent;
                
                // fixup vertex decl type if needed
                if (model.VertexType == -1)
                {
                    model.VertexBuffer = vertexBuffer;
                    model.VertexType = (short)vDeclType;
                    model.Flags = 1; // has vertex buffer
                }
                
                // populates vertex/index buffers and adds them to the submodel
                var meshes = geom.CompileMeshes(vertexBuffer, indexBuffer, (lodInstance.ID == 5));
                
                meshList.AddRange(meshes);

                // HACK: apply model package to meshes (REWRITE EVERYTHING!!!)
                foreach (var mesh in meshes)
                    mesh.ModelPackage = modelPackage;
            }
            
            // finally...compile the model package
            modelPackage.Parts = Models;
            modelPackage.MeshGroups = SubModels;
            modelPackage.Meshes = meshList;

            modelPackage.VertexBuffers = new List<VertexBuffer>() {
                vertexBuffer
            };

            modelPackage.IndexBuffer = indexBuffer.Compile();

            // temporarily force vehicle package
            modelPackage.UID = 0xFF;

            // generate uid shit
            var uidBuffer = GetBufferFromHexString(UID);

            var uid = BitConverter.ToInt32(uidBuffer, 0);
            var handle = BitConverter.ToInt32(uidBuffer, 4);

            // fixup models
            foreach (var model in Models)
            {
                model.UID = uid;
                model.Handle = handle;

                // no fucking clue what this is
                // with my luck, it's just padding :/
                model.Unknown = new Vector4() {
                    X = 0.0f,
                    Y = 0.0f,
                    Z = 0.0f,
                    W = 1.0f,
                };

                // TODO: properly calculate bounding box
                for (int i = 0; i < 8; i++)
                {
                    model.Transform[i] = new Vector4() {
                        X = 0.0f,
                        Y = 0.0f,
                        Z = 0.0f,
                        W = 1.0f,
                    };
                }
            }
            
            modelPackage.Materials = new List<MaterialDataPC>();
            modelPackage.Substances = new List<SubstanceDataPC>();
            modelPackage.Textures = new List<TextureDataPC>();

            // lastly, apply materials
            foreach (var material in Materials)
            {
                var tex = new TextureDataPC() {
                    Reserved = 0x01010101
                };

                tex.Buffer = File.Exists(material.TextureFile)
                    ? File.ReadAllBytes(material.TextureFile)
                    : DDSUtils.MakeRGBATexture(material.Color);

                var header = default(DDSHeader);

                if (DDSUtils.GetHeaderInfo(tex.Buffer, ref header))
                {
                    tex.Type = DDSUtils.GetTextureType(header.PixelFormat.FourCC);

                    tex.Width = header.Width;
                    tex.Height = header.Height;
                }
                else
                {
                    // TODO: throw exception and handle this
                    Debug.WriteLine("**** UNKNOWN TEXTURE FORMAT ****");
                }

                tex.CRC32 = (int)Memory.GetCRC32(tex.Buffer);

                var sub = new SubstanceDataPC() {
                    Flags = 0x406, // HACK: generic vehicle texture
                    Textures = new List<TextureDataPC>() {
                        tex
                    },
                };

                var mtl = new MaterialDataPC() {
                    Substances = new List<SubstanceDataPC>() {
                        sub
                    },
                };

                modelPackage.Materials.Add(mtl);
                modelPackage.Substances.Add(sub);
                modelPackage.Textures.Add(tex);
            }

            var resource = (ISpoolableResource)modelPackage;

            resource.Spooler = new SpoolableBuffer() {
                Context = (int)ChunkType.ModelPackagePC,
                Alignment = SpoolerAlignment.Align4096,
                Version = 6,
                Description = "Custom model package",
            };
            
            return modelPackage;
        }

        public ModelConverter()
        {
            Models = new List<PartsGroup>();
            SubModels = new List<MeshGroup>();
            Geometries = new List<GeometryHolder>();
        }
    }
}
