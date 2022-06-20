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

using FreeImageAPI;

namespace Antilli
{
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

            public IndexBuffer Compile()
            {
                return new IndexBuffer(Count) {
                    Indices = Indices.ToArray(),
                };
            }
        }
        
        public class GeometryHolder
        {
            public VerticesHolder Vertices;

            public List<PolyMesh> Meshes;

            public LodInstance Instance;

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

            public List<SubModel> CompileMeshes(VertexBuffer vertexBuffer, IndicesHolder indexBuffer, bool shadowModelHack)
            {
                var meshes = new List<SubModel>();

                var vertices = vertexBuffer.Vertices;
                var indices = indexBuffer.Indices;

                var lodInstance = Instance.Parent;
                var model = lodInstance.Parent;

                // reset the old meshes (causes crash if we don't do this)
                Instance.SubModels = new List<SubModel>();
                
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

                    var mesh = new SubModel() {
                        Model = model,
                        LodInstance = Instance,

                        PrimitiveType = PrimitiveType.TriangleList,

                        VertexOffset = vertexOffset,
                        VertexCount = (vertices.Count - vertexOffset),

                        IndexOffset = indexOffset,
                        IndexCount = numPrims,

                        Material = new MaterialHandle((ushort)polyMesh.MaterialId, 0xFFFD),
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

                Instance.SubModels.AddRange(meshes);

                return meshes;
            }

            public GeometryHolder()
            {
                Meshes = new List<PolyMesh>();
            }

            public GeometryHolder(LodInstance instance)
                : base()
            {
                Instance = instance;
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

        public int Version { get; set; }
        public EffectType EffectType { get; set; }

        public List<Model> Models { get; set; }
        public List<LodInstance> Instances { get; set; }

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

        private static int GetLodNameType(string lodName)
        {
            var lodType = Lod.GetLodNameType(lodName);

            if (lodType != -1)
                return lodType;

            // backwards compat. list
            switch (lodName)
            {
            case "H":       return 0;
            case "M":       return 1;
            case "L":       return 2;
            case "VL":      return 4;
            case "SHADOW":  return 5;
            }

            // still not found
            return -1;
        }

        private static int GetLodNameTypeMask(string lodType)
        {
            var lodIndex = GetLodNameType(lodType);

            if (lodIndex != -1)
                return Lod.GetLodTypeMask(lodIndex);

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

                var model = new Model() {
                    VertexType = -1
                };

                var lods = model.Lods;

                Models.Add(model);

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

                    var slotIdx = GetLodNameType(slotType);

                    if (slotIdx == -1)
                        throw new InvalidOperationException($"Could not determine slot index for '{objName}'!");
                    
                    // we need to build the buffers in the proper order
                    // so we'll process them after we get the basic layout setup
                    geomQueue[slotIdx] = lodInstance.GeometryInstances[0];

                    var partDef = new Lod(slotIdx) {
                        Parent = model,
                        Mask = GetLodNameTypeMask(slotType),
                    };

                    lods[slotIdx] = partDef;

                    var instance = new LodInstance() {
                        Parent = partDef
                    };

                    partDef.Instances.Add(instance);
                    
                    var transform = lodInstance.Matrix.Values;

                    for (int t = 0; t < 4; t++)
                        instance.Transform.SetRow(t, GetVectorFromTransform(transform, (t * 4)));
                }

                // gotta make yet another fucking queue
                var geomLists = new List<GeometryHolder>[7];

                for (int i = 0; i < 7; i++)
                    geomLists[i] = new List<GeometryHolder>();

                for (int p = 0; p < 7; p++)
                {
                    var part = model.Lods[p];

                    // nothing to see here
                    if (part == null)
                        continue;

                    var instance = part.Instances[0];

                    // correct order
                    Instances.Add(instance);

                    var geomInst = geomQueue[p];

                    // very bad!
                    if (geomInst == null)
                        throw new InvalidOperationException($"Geometry is MISSING for part {p + 1}!");

                    var geom = collada.GeometryLibrary[geomInst.Url];

                    if (geom == null)
                        throw new InvalidOperationException($"Geometry URL '{geomInst.Url}' not found!");

                    var mesh = geom.Mesh;
                    var geometry = new GeometryHolder(instance);

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
                if (!String.IsNullOrEmpty(shader.Diffuse.Value.Texture))
                {
                    var sm = profile.Params.Find((p) => p.SubId == shader.Diffuse.Value.Texture)?.Param as COLLADASampler2DParamValue;

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
                    mtl.Color = shader.Diffuse.Value.Color;
                }

                Materials.Add(mtl);
            }

            return true;
        }

        private static FREE_IMAGE_FORMAT? FIFormat(Stream stream, out bool alpha)
        {
            var format = FreeImage.GetFileTypeFromStream(stream);

            alpha = false;

            switch (format)
            {
            case FREE_IMAGE_FORMAT.FIF_BMP:
            case FREE_IMAGE_FORMAT.FIF_GIF:
            case FREE_IMAGE_FORMAT.FIF_JPEG:
                // no alpha
                break;
            case FREE_IMAGE_FORMAT.FIF_DDS:
            case FREE_IMAGE_FORMAT.FIF_PNG:
            case FREE_IMAGE_FORMAT.FIF_TIFF:
            case FREE_IMAGE_FORMAT.FIF_TARGA:
                alpha = true;
                break;
            default:
                // unsupported format
                return null;
            }

            return format;
        }

        private static byte[] CopyAlphaAToAlphaB(byte[] bufferA, byte[] bufferB, bool blackout = false)
        {
            byte[] result = null;

            using (var streamA = new MemoryStream(bufferA))
            using (var streamB = new MemoryStream(bufferB))
            {
                bool alphaA, alphaB;

                var formatA = FIFormat(streamA, out alphaA) ?? FREE_IMAGE_FORMAT.FIF_UNKNOWN;
                var formatB = FIFormat(streamB, out alphaB) ?? FREE_IMAGE_FORMAT.FIF_UNKNOWN;

                // supported formats?
                if ((formatA == FREE_IMAGE_FORMAT.FIF_UNKNOWN) || (formatB == FREE_IMAGE_FORMAT.FIF_UNKNOWN))
                    return null;

                // formats can have alpha? (TODO: verify results)
                if (!alphaA || !alphaB)
                    return null;

                FIBITMAP dibA = FreeImage.LoadFromStream(streamA, ref formatA);
                FIBITMAP dibB = FreeImage.LoadFromStream(streamB, ref formatB);

                // images loaded?
                if (dibA.IsNull || dibB.IsNull)
                    return null;

                var dibAlphaA = FreeImage.GetChannel(dibA, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
                var dibAlphaB = FreeImage.GetChannel(dibB, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);

                // alpha channels present?
                if (dibAlphaA.IsNull || dibAlphaB.IsNull)
                    return null;

                // get a copy of B
                var dib = FreeImage.Clone(dibB);

                // replace copy's alpha with A's alpha
                FreeImage.SetChannel(dib, dibAlphaA, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);

                if (blackout)
                {
                    // TODO: fill RGB with black
                }

                using (var ms = new MemoryStream())
                {
                    // save the result
                    if (FreeImage.SaveToStream(dib, ms, FREE_IMAGE_FORMAT.FIF_TARGA))
                        result = ms.ToArray();
                }
            }

            return result;
        }

        private static TextureDataPC CopyTextureAlphaAToB(TextureDataPC textureA, TextureDataPC textureB, bool blackout = false)
        {
            var bufferA = textureA.Buffer; // texture with alpha channel we want
            var bufferB = textureB.Buffer; // texture we want to set alpha channel of

            // textureB is our basis for the new texture,
            // so let's make a copy of it ;)
            var texture = CopyCatFactory.GetCopy(textureB, CopyClassType.DeepCopy);

            // do the work necessary
            texture.Buffer = CopyAlphaAToAlphaB(bufferA, bufferB);

            // Driv3rize it?
            if (texture.UID == 0x01010101)
                texture.Handle = (int)Memory.GetCRC32(texture.Buffer);

            // return our new texture
            return texture;
        }

        public static ModelPackage Convert(ModelPackage modelPackage, int targetVersion, int targetUID = -1)
        {
            var spooler = new SpoolableBuffer() {
                Context = ModelPackageResource.GetChunkId(modelPackage.Platform, targetVersion),
                Version = targetVersion,
                Alignment = SpoolerAlignment.Align4096,
                Description = "Custom model package",
            };

            var version = modelPackage.Version;
            var fixups = (version != targetVersion);

            var resource = SpoolableResourceFactory.Create<ModelPackage>(spooler);

            // fuck
            if (modelPackage.VertexBuffers.Count != 1)
                throw new InvalidOperationException("Can't recompile model package -- must have exactly ONE vertex buffer!");

            // preserve flags
            var flags = modelPackage.Flags;
            var spooledVehicleHacks = false;

            if ((flags & ModelPackage.FLAG_SpooledVehicleHacks) != 0)
            {
                flags &= ~ModelPackage.FLAG_SpooledVehicleHacks;
                spooledVehicleHacks = true;
            }

            if (targetUID == -1)
            {
                // converting to DPL?
                //if (fixups && targetVersion == 1)
                //        throw new Exception("Converting a model package to Driver: Parallel Lines requires a valid target UID!");

                if (fixups && (targetVersion == 1))
                {
                    // D3 to DPL
                    targetUID = 0x1234;
                }
                else
                {
                    // reuse the UID
                    targetUID = modelPackage.UID;
                }
            }

            resource.UID = targetUID;
            resource.Platform = modelPackage.Platform;
            resource.Version = targetVersion;

            resource.Flags = flags;
            
            var gModels = new List<Model>();
            var gLodInstances = new List<LodInstance>();
            var gSubModels = new List<SubModel>();

            var gMaterials = new List<MaterialDataPC>();
            var gSubstances = new List<SubstanceDataPC>();
            var gTextures = new List<TextureDataPC>();

            var gVertices = new List<int>();
            var gIndices = new List<short>();

            var gVertexList = new List<Vertex>();
            var gVertexCount = 0;

            var luVertices = new Dictionary<int, int>();

            var lookup = new Dictionary<int, int>();

            var vBuffer = modelPackage.VertexBuffers[0];
            VertexBuffer vertexBuffer = null;

            var indices = modelPackage.IndexBuffer.Indices;

            // HACKS: was trying to figure out why DPL kept crashing..
            // ... it only fucking wants triangle fans!
#if MARK_HAS_FINISHED_HIS_INCREDIBLE_TRIANGLE_FAN_GENERATOR
            var convertToTris = (targetVersion == 6);
            var convertToFans = (targetVersion == 1);
#elif I_WANNA_MAKE_DPL_LOOK_LIKE_SHIT_AND_SPEW_GARBAGE_AT_ME
            var convertToTris = (targetVersion == 6);
            var convertToFans = false;
#else
            var convertToTris = true;
            var convertToFans = false;
#endif

            var scaleVerts = (version == 1 && targetVersion == 6);
            var d3Scaling = (version == 6 && targetVersion == 1);

            // copy materials
            gMaterials.AddRange(modelPackage.Materials);
            gSubstances.AddRange(modelPackage.Substances);
            gTextures.AddRange(modelPackage.Textures);

            var mtlLookup = new Dictionary<MaterialHandle, int>();

            foreach (var _model in modelPackage.Models)
            {
                if (vertexBuffer == null)
                    vertexBuffer = VertexBuffer.Create(targetVersion, _model.VertexType);
#if !OLD_METHOD
                var vertexBaseOffset = gVertexList.Count;
#endif
                var modelScale = _model.Scale;

                if (d3Scaling)
                    modelScale = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

                var model = new Model() {
                    UID = _model.UID,

                    VertexBuffer = vertexBuffer,
                    VertexType = _model.VertexType,

                    Flags = _model.Flags,
                    Scale = modelScale,

                    BoundingBox = _model.BoundingBox,
                };

                gModels.Add(model);
                
                var lods = new List<Lod>();

                // iterate through each Lod
                foreach (var _lod in _model.Lods)
                {
                    var lod = new Lod(_lod.Type) {
                        Parent = model,
                        Mask = Lod.GetLodTypeMask(_lod.Type, targetVersion),
                    };

                    // reuse the existing mask
                    if (lod.Mask == -1)
                        lod.Mask = _lod.Mask;

                    if (version == 6 && targetVersion == 1)
                    {
                        if (_lod.Instances.Count != 0)
                        {
                            lod.Flags = (lod.Mask != 0) ? 1 : 0;
                        }
                        else
                        {
                            lod.Mask = 0;
                            lod.Flags = 0;
                        }
                    }
                    
                    lods.Add(lod);

                    var instances = new List<LodInstance>();
                    
                    foreach (var _instance in _lod.Instances)
                    {
                        var instance = new LodInstance() {
                            Parent = lod,

                            Handle = _instance.Handle,
                            Reserved = _instance.Reserved,
                            Transform = _instance.Transform,
                            UseTransform = _instance.UseTransform,
                        };

                        instances.Add(instance);
                        gLodInstances.Add(instance);

                        var subModels = new List<SubModel>();

                        foreach (var _subModel in _instance.SubModels)
                        {
#if !OLD_METHOD
                            var vertexOffset = (gVertexList.Count - vertexBaseOffset); // == num vertices added so far to the model

                            var subModel = new SubModel()
                            {
                                Model = model,
                                LodInstance = instance,
                                ModelPackage = resource,

                                PrimitiveType = PrimitiveType.TriangleList,

                                VertexBaseOffset = vertexBaseOffset,

                                VertexOffset = vertexOffset,
                                IndexOffset = gIndices.Count,
                            };

                            var vertexCount = 0;
                            var indexCount = 0;

                            var tris = _subModel.CollectVertexTris(ref lookup, ref gVertices, indices, out vertexCount);

                            for (int t = 0; t < tris.Count; t++)
                            {
                                var tri = tris[t];
                                var vIdx = gVertices[tri];
                                
                                if (!luVertices.ContainsKey(vIdx))
                                {
                                    var vertex = vBuffer.Vertices[vIdx].ToVertex();

                                    if (!d3Scaling)
                                        vertex.ApplyScale(modelScale);

                                    gVertexList.Add(vertex);
                                    luVertices.Add(vIdx, gVertexList.Count - 1);
                                }

                                gIndices.Add((short)luVertices[vIdx]);
                                indexCount++;
                            }

                            subModel.VertexCount = vertexCount;
                            subModel.IndexCount = indexCount / 3;
#else
                            var tris = new List<int>();
                            var vertices = _subModel.CollectVertices(out tris);
                            
                            var vertexOffset = gVertices_DATA.Count;
                            var vertexCount = 0;
                            var indexOffset = gIndices.Count;
                            var indexCount = (tris.Count / 3);

                            if (convertToTris)
                            {
                                // convert to triangles
                                for (int t = 0; t < tris.Count; t++)
                                {
                                    var idx = tris[t];
                                    var vIdx = vertices[idx];

                                    if (!lookup.ContainsKey(vIdx))
                                    {
                                        var vertex = _subModel.VertexBuffer.Vertices[vIdx].ToVertex();

                                        if (!d3Scaling)
                                            vertex.ApplyScale(modelScale);

                                        lookup.Add(vIdx, (vertexOffset + vertexCount));

                                        gVertices_DATA.Add(vertex);
                                        vertexCount++;
                                    }

                                    // append to buffer
                                    gIndices.Add((short)lookup[vIdx]);
                                }
                            }
                            else if (convertToFans) // unreachable code... not implemented
                            {

                            }
                            else if (scaleVerts) // unreachable code... here for reference only
                            {
                                // scale all the triangle fans
                                foreach (var vIdx in vertices)
                                {
                                    if (!lookup.ContainsKey(vIdx))
                                    {
                                        var vertex = _subModel.VertexBuffer.Vertices[vIdx].ToVertex();

                                        vertex.ApplyScale(modelScale);

                                        lookup.Add(vIdx, vertexOffset++);
                                        gVertices_DATA.Add(vertex);
                                    }
                                }
                            }
#endif
                            var material = _subModel.Material;

                            if (fixups)
                            {
                                // retarget material handles
                                switch (version)
                                {
                                case 1:
                                    if (targetVersion == 6)
                                    {
                                        //
                                        // Driv3r format
                                        //
                                        if (material.UID == modelPackage.UID)
                                        {
                                            material.UID = 0xFFFD;
                                        }
                                        else if (material.UID != 0xCCCC && !spooledVehicleHacks)
                                        {
                                            goto RETARGET_GLOBALS;
                                        }
                                    }
                                    break;
                                case 6:
                                    if (targetVersion == 1)
                                    {
                                        //
                                        // DPL format
                                        //
                                        if (material.UID == 0xFFFD)
                                        {
                                            // fixup the UID to match model package
                                            material.UID = (ushort)targetUID;
                                        }
                                        else if (material.UID != 0xCCCC)
                                        {
                                            if (material == 0 && modelPackage.UID == 0xFF)
                                            {
                                                // fix the damn shadows...
                                                material.UID = 0xCCCC;
                                            }
                                            else
                                            {
                                                // actually retarget it
                                                goto RETARGET_GLOBALS;
                                            }
                                        }
                                    }
                                    break;
                                RETARGET_GLOBALS:
                                    // compile any used globals into our model package
                                    if (!mtlLookup.ContainsKey(material))
                                    {
                                        MaterialDataPC mtl = null;

                                        if (MaterialManager.Find(_subModel.Material, out mtl) > 0)
                                        {
                                            mtlLookup.Add(material, gMaterials.Count);
                                            gMaterials.Add(mtl);

                                            foreach (var substance in mtl.Substances)
                                            {
                                                gSubstances.Add(substance);
                                                gTextures.AddRange(substance.Textures);
                                            }
                                        }
                                    }

                                    if (mtlLookup.ContainsKey(material))
                                    {
                                        material.Handle = (ushort)mtlLookup[material];

                                        if (targetVersion == 6)
                                        {
                                            material.UID = 0xFFFD;
                                        }
                                        else
                                        {
                                            material.UID = (ushort)targetUID;
                                        }
                                    }
                                    else
                                    {
                                        // oopsie woopsie! :D
                                        material.UID = 0xFFFF;
                                        material.Handle = 0;
                                    }
                                    break;
                                }
                            }

#if !OLD_METHOD
                            subModel.Material = material;
#else
                            var subModel = new SubModel() {
                                Model = model,
                                LodInstance = instance,
                                ModelPackage = resource,

                                Material = material,
                            };

                            if (convertToTris)
                            {
                                subModel.PrimitiveType = PrimitiveType.TriangleList;

                                subModel.VertexOffset = vertexOffset;
                                subModel.VertexCount = vertexCount;

                                subModel.IndexOffset = indexOffset;
                                subModel.IndexCount = indexCount;
                            }
                            else if (convertToFans) // unreachable code... not implemented
                            {
                                subModel.PrimitiveType = PrimitiveType.TriangleFan;

                                subModel.VertexCount = vertexCount;
                                subModel.IndexOffset = indexOffset;

                                // these are ALWAYS zero !!!
                                subModel.VertexBaseOffset = 0;
                                subModel.VertexOffset = 0;
                                subModel.IndexCount = 0;
                            }
                            else  // unreachable code... here for reference only
                            {
                                subModel.PrimitiveType = _subModel.PrimitiveType;

                                subModel.VertexBaseOffset = _subModel.VertexBaseOffset;

                                subModel.VertexOffset = _subModel.VertexOffset;
                                subModel.VertexCount = _subModel.VertexCount;

                                subModel.IndexOffset = _subModel.IndexOffset;
                                subModel.IndexCount = _subModel.IndexCount;
                            }
#endif
                            subModels.Add(subModel);
                            gSubModels.Add(subModel);
                        }

                        instance.SubModels = subModels;
                    }

                    lod.Instances = instances;
                }

                model.Lods = lods;
            }

            if (convertToTris || convertToFans)
            {
                // compile buffers
                vertexBuffer.SetVertices(gVertexList);

                resource.VertexBuffers = new List<VertexBuffer>() {
                    vertexBuffer
                };

                var indexBuffer = new IndicesHolder()
                {
                    Indices = gIndices
                };

                resource.IndexBuffer = indexBuffer.Compile();
            }
            else if (scaleVerts) // unreachable code... here for reference only
            {
                // re-sort the vertices in their original order
                var verts = new Vertex[vBuffer.Count];

                for (int v = 0; v < vBuffer.Count; v++)
                {
                    var vIdx = lookup[v];

                    verts[v] = gVertexList[vIdx];
                }

                // set the vertex buffer
                vertexBuffer.SetVertices(verts.ToList());

                resource.VertexBuffers = new List<VertexBuffer>() { vertexBuffer };
                resource.IndexBuffer = new IndexBuffer(0)
                {
                    Indices = modelPackage.IndexBuffer.Indices
                };

                // clean up our mess
                gVertexList.Clear();
            }
            else
            {
                // directly copy the vertex buffer
                vBuffer.CopyTo(vertexBuffer);

                resource.VertexBuffers = new List<VertexBuffer>() { vertexBuffer };
                resource.IndexBuffer = new IndexBuffer(0)
                {
                    Indices = modelPackage.IndexBuffer.Indices
                };
            }

            resource.Models = gModels;
            resource.LodInstances = gLodInstances;
            resource.SubModels = gSubModels;

            if (fixups)
            {
                // recompile substances/textures
                gSubstances = new List<SubstanceDataPC>();
                gTextures = new List<TextureDataPC>();

                switch (version)
                {
                case 1:
                    if (targetVersion == 6)
                    {
                        //
                        // Driv3r format
                        //
                        foreach (var material in gMaterials)
                        {
                            foreach (var substance in material.Substances)
                            {
                                substance.TS3 = 0;

                                // related to specular/alpha maps
                                if (substance.TS1 == 1 && substance.TS2 == 1)
                                {
                                    // most Driv3r vehicles use this combination
                                    substance.TS1 = 2;
                                    substance.TS2 = 1;
                                }

                                // clear specular flag
                                if ((substance.Flags & 0x40) != 0)
                                    substance.Flags &= ~0x40;

                                // fix emissive flag
                                if ((substance.Flags & 0xC0) != 0)
                                {
                                    substance.Flags &= ~0xC0;
                                    substance.Flags |= 0x180;
                                }

                                // DPL saves space by using 2 textures instead of 4
                                // so for Driv3r, we need to expand out the other 2
                                if (substance.TextureFlags == (int)SubstanceExtraFlags.DPL_DamageAndColorMask)
                                {
                                    substance.TextureFlags = (int)SubstanceExtraFlags.DamageAndColorMask_AlphaMaps;

                                    var texA1 = substance.Textures[0]; // clean + alpha specular
                                    var texB1 = substance.Textures[1]; // damage + alpha color map

                                    // create clean alpha color map
                                    var texA2 = CopyTextureAlphaAToB(texB1, texA1, true);

                                    texA2.UID = texA1.UID;
                                    texA2.Handle = texA1.Handle + 0x12345;

                                    // create damage alpha color map
                                    var texB2 = CopyCatFactory.GetCopy(texA2, CopyClassType.DeepCopy);

                                    texB2.UID = texB1.UID;
                                    texB2.Handle = texB1.Handle + 0x12345;

                                    // finally, copy the clean alpha specular to the damage texture
                                    texB1 = CopyTextureAlphaAToB(texA1, texB1);

                                    // allow damage + color mask to work
                                    substance.Textures = new List<TextureDataPC>() {
                                        texA1,
                                        texA2,
                                        texB1,
                                        texB2,
                                    };
                                }
                                else if (substance.TextureFlags == (int)SubstanceExtraFlags.DPL_ColorMask)
                                {
                                    substance.TextureFlags = (int)SubstanceExtraFlags.ColorMask;
                                }
                                else if (substance.TextureFlags == (int)SubstanceExtraFlags.DPL_Damage)
                                {
                                    substance.TextureFlags = (int)SubstanceExtraFlags.Damage;
                                }
                                else if (substance.TextureFlags == (int)SubstanceExtraFlags.BumpMap)
                                {
                                    // assuming this based on Tanner's character model
                                    substance.TS1 = 1;
                                    substance.TS2 = 2;
                                }

                                gSubstances.Add(substance);

                                foreach (var texture in substance.Textures)
                                {
                                    gTextures.Add(texture);
                                }
                            }
                        }
                    }
                    break;
                case 6:
                    if (targetVersion == 1)
                    {
                        //
                        // DPL format
                        //
                        foreach (var material in gMaterials)
                        {
                            foreach (var substance in material.Substances)
                            {
                                var specular = false;

                                // add specular flag
                                if ((substance.TS1 == 1 || substance.TS1 == 2) && (substance.TS2 == 2 || substance.TS2 == 1))
                                {
                                    substance.Flags |= 0x40;
                                    specular = true;
                                }

                                // fix emissive flag
                                if ((substance.Flags & 0x180) != 0)
                                {
                                    substance.Flags &= ~0x180;
                                    substance.Flags |= 0xC0;
                                }

                                // clear out these things (TODO: figure out exactly how to set them)
                                substance.TS1 = 0;
                                substance.TS2 = 0;
                                substance.TS3 = 0;

                                var textures = new List<TextureDataPC>();

                                var texFlags = SubstanceExtraFlags.None;

                                // figure out substance texture flags
                                if ((substance.TextureFlags & (int)SubstanceExtraFlags.DamageAndColorMask_AlphaMaps) != 0)
                                {
                                    var texA1 = substance.Textures[0]; // clean + alpha specular
                                    var texA2 = substance.Textures[1]; // clean alpha color map
                                    var texB1 = substance.Textures[2]; // damage + alpha specular
                                    var texB2 = substance.Textures[3]; // damage alpha color map

                                    // copy the clean alpha map to the damage texture
                                    texB1 = CopyTextureAlphaAToB(texA2, texB1);

                                    textures.Add(texA1); // clean + alpha specular
                                    textures.Add(texB1); // damage + alpha color map

                                    substance.TS1 = 1;
                                    substance.TS2 = 1;
                                    substance.TS3 = 220;
                                    substance.Textures = textures;

                                    texFlags = SubstanceExtraFlags.DPL_DamageAndColorMask;
                                }
                                else if ((substance.TextureFlags & (int)SubstanceExtraFlags.ColorMask) != 0)
                                {
                                    var texA1 = substance.Textures[0]; // clean + alpha specular
                                    var texA2 = substance.Textures[1]; // alpha color map

                                    textures.Add(texA1); // clean + alpha specular
                                    textures.Add(texA2); // alpha color map

                                    substance.TS1 = 1;
                                    substance.TS2 = 1;
                                    substance.TS3 = 220;
                                    substance.Textures = textures;

                                    texFlags = SubstanceExtraFlags.DPL_ColorMask;
                                }
                                else if ((substance.TextureFlags & (int)SubstanceExtraFlags.Damage) != 0)
                                {
                                    var texA1 = substance.Textures[0]; // clean + alpha specular
                                    var texB1 = substance.Textures[1]; // damage + alpha specular

                                    textures.Add(texA1); // clean + alpha specular
                                    textures.Add(texB1); // damage + alpha specular

                                    substance.TS1 = 1;
                                    substance.TS2 = 1;
                                    substance.TS3 = 220;
                                    substance.Textures = textures;

                                    texFlags = SubstanceExtraFlags.DPL_Damage;
                                }
                                else if ((substance.TextureFlags & (int)SubstanceExtraFlags.BumpMap) != 0)
                                {
                                    // assuming this based on TK's character model
                                    substance.TS1 = 1;
                                    substance.TS2 = 0;
                                    substance.TS3 = 0;
                                }

                                // set the texture flags
                                substance.TextureFlags = (int)texFlags;

                                gSubstances.Add(substance);

                                foreach (var texture in substance.Textures)
                                {
                                    gTextures.Add(texture);
                                }
                            }
                        }
                    }
                    break;
                }
            }

            resource.Materials = gMaterials;
            resource.Substances = gSubstances;
            resource.Textures = gTextures;
            resource.Palettes = new List<PaletteData>();

            return resource;
        }
        
        public ModelPackage ToModelPackage()
        {
            var modelPackage = SpoolableResourceFactory.Create<ModelPackage>();
            
            var vDeclType = GetVertexDeclType(EffectType);
            
            var vertexBuffer = VertexBuffer.Create(Version, vDeclType);
            var indexBuffer = new IndicesHolder();

            var subModels = new List<SubModel>();

            var vertexBaseOffset = vertexBuffer.Count;
            
            foreach (var geom in Geometries)
            {
                if (geom == null)
                {
                    vertexBaseOffset = vertexBuffer.Count;
                    continue;
                }

                var instance = geom.Instance;
                var lod = instance.Parent;
                var model = lod.Parent;
                
                // fixup vertex decl type if needed
                if (model.VertexType == -1)
                {
                    model.VertexBuffer = vertexBuffer;
                    model.VertexType = (short)vDeclType;
                    model.Flags = 1; // has vertex buffer
                }
                
                // populates vertex/index buffers and adds them to the submodel
                var meshes = geom.CompileMeshes(vertexBuffer, indexBuffer, (lod.Type == 5));
                
                subModels.AddRange(meshes);

                // HACK: apply model package to meshes (REWRITE EVERYTHING!!!)
                foreach (var mesh in meshes)
                    mesh.ModelPackage = modelPackage;
            }
            
            // finally...compile the model package
            modelPackage.Models = Models;
            modelPackage.LodInstances = Instances;
            modelPackage.SubModels = subModels;

            modelPackage.VertexBuffers = new List<VertexBuffer>() {
                vertexBuffer
            };

            modelPackage.IndexBuffer = indexBuffer.Compile();

            // temporarily force vehicle package
            modelPackage.UID = 0xFF;
            modelPackage.Version = Version;

            // generate uid shit
            var uidBuffer = GetBufferFromHexString(UID);

            var uid = BitConverter.ToInt32(uidBuffer, 0);
            var handle = BitConverter.ToInt32(uidBuffer, 4);

            // fixup models
            foreach (var model in Models)
            {
                model.UID = new UID(uid, handle);
                
                model.Scale = new Vector4() {
                    X = 1.0f,
                    Y = 1.0f,
                    Z = 1.0f,
                    W = 1.0f,
                };

                // TODO: properly calculate bounding box
                model.BoundingBox.SetDefaults();
            }
            
            modelPackage.Materials = new List<MaterialDataPC>();
            modelPackage.Substances = new List<SubstanceDataPC>();
            modelPackage.Textures = new List<TextureDataPC>();
            modelPackage.Palettes = new List<PaletteData>();

            // lastly, apply materials
            foreach (var material in Materials)
            {
                var tex = new TextureDataPC() {
                    UID = 0x01010101
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

                tex.Handle = (int)Memory.GetCRC32(tex.Buffer);

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
                Context = ChunkType.ModelPackagePC,
                Alignment = SpoolerAlignment.Align4096,
                Version = 6,
                Description = "Custom model package",
            };
            
            return modelPackage;
        }

        public ModelConverter()
        {
            Models = new List<Model>();
            Instances = new List<LodInstance>();
            Geometries = new List<GeometryHolder>();
        }
    }
}
