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

using System.Xml;
using System.Xml.Linq;

using DSCript.Spooling;

namespace DSCript.Models
{
    public enum ModelPackageLoadLevel
    {
        /// <summary>
        /// Loads in the header only.
        /// </summary>
        FastLoad    = 0,

        /// <summary>
        /// Loads in the header and materials.
        /// </summary>
        Materials   = 1,

        /// <summary>
        /// Loads in the header, materials, and models.
        /// </summary>
        Models      = 2,

        /// <summary>
        /// Loads in everything.
        /// </summary>
        Default     = 3,
    }

    public class ModelPackage : ModelPackageResource
    {
        public static ModelPackageLoadLevel LoadLevel = ModelPackageLoadLevel.Default;

        public static readonly int FLAG_SpooledVehicleHacks = 0x10000000;

        public static PlatformType GetPlatformType(ChunkType context)
        {
            switch (context)
            {
            case ChunkType.ModelPackagePC:
            case ChunkType.ModelPackagePC_X:
                return PlatformType.PC;

            case ChunkType.ModelPackagePS2:     return PlatformType.PS2;
            case ChunkType.ModelPackageXbox:    return PlatformType.Xbox;
            case ChunkType.ModelPackageWii:     return PlatformType.Wii;
            }

            return PlatformType.Generic;
        }

        public string DisplayName { get; set; } = "Model Package";

        public bool IsOwnModelPackage()
        {
            var notASingleVVVFile = true;

            if (Version == 6)
                notASingleVVVFile = (UID != 0x2D);

            if (HasModels)
            {
                var uid = Models[0].UID;

                for (int i = 1; i < Models.Count; i++)
                {
                    if (Models[i].UID != uid)
                        return false;
                }
            }

            return notASingleVVVFile;
        }

        public bool IsOwnModelPackage(UID modelUID)
        {
            var notASingleVVVFile = true;

            if (Version == 6)
                notASingleVVVFile = (UID != 0x2D);

            foreach (var model in Models)
            {
                if (model.UID != modelUID)
                    return false;
            }

            return notASingleVVVFile;
        }

        public ModelPackage ExtractModelsByUID(UID modelUID, int targetUID)
        {
            if (!HasModels)
                throw new Exception("Cannot extract models from a package with no models!");

            // copy spooler information
            var spooler = new SpoolableBuffer()
            {
                Context = Spooler.Context,
                Alignment = Spooler.Alignment,
                Description = Spooler.Description,
                Version = Spooler.Version,
            };

            // create new model package
            var package = SpoolableResourceFactory.Create<ModelPackage>(spooler);

            // initialize settings
            package.Version = Version;
            package.Platform = Platform;
            package.UID = targetUID;
            package.Flags = Flags;

            var updateUIDs = (targetUID != UID);
            var materialUID = (ushort)targetUID;
            var spooledVehicleHacks = false;

            if (updateUIDs && targetUID == 0xFF)
            {
                package.Flags |= FLAG_SpooledVehicleHacks;
                spooledVehicleHacks = true;
                materialUID = 0xFFFD;
            }

            //
            // STEP 1: Models
            //

            package.Models = new List<Model>();
            package.LodInstances = new List<LodInstance>();
            package.SubModels = new List<SubModel>();

            // for creating our new index buffer
            var gIndices = new List<int>();

            var luVertexBuffers = new Dictionary<int, VertexBuffer>();
            var luVertexCounts = new Dictionary<int, int>(); // Count = VertexOffset

            // collect materials information
            var materialRefs = new List<MaterialDataPC>();
            var luMaterials = new Dictionary<MaterialHandle, MaterialHandle>();
#if !SHITTY_REBASING
            var luOldVertexBuffer = new Dictionary<int, VertexBuffer>();
            var luLowestVertexBase = new Dictionary<int, int>();
            var luHighestVertexOffset = new Dictionary<int, int>();

            var lowestIndexOffset = -1;
            var highestIndexOffset = -1;
#endif
            // first pass: collect new models/instances/submodels, material references, initialize vertex buffers
            foreach (var _model in Models)
            {
                if (_model.UID != modelUID)
                    continue;

                // DEEP COPY: all new instances down the line
                var model = CopyCatFactory.GetCopy(_model, CopyClassType.DeepCopy);

                // prepare our vertex buffer lookups for the second pass
                if (!luVertexBuffers.ContainsKey(model.VertexType))
                {
                    luVertexBuffers[model.VertexType] = VertexBuffer.Create(Version, model.VertexType);
                    luVertexCounts[model.VertexType] = 0;
                }
#if !SHITTY_REBASING
                // buffer to retreive from
                if (!luOldVertexBuffer.ContainsKey(model.VertexType))
                    luOldVertexBuffer[model.VertexType] = _model.VertexBuffer;

                if (!luLowestVertexBase.ContainsKey(model.VertexType))
                    luLowestVertexBase[model.VertexType] = -1;
                if (!luHighestVertexOffset.ContainsKey(model.VertexType))
                    luHighestVertexOffset[model.VertexType] = -1;

                var lowestVertexBase = luLowestVertexBase[model.VertexType];
                var highestVertexOffset = luHighestVertexOffset[model.VertexType];
#endif
                // collect model
                package.Models.Add(model);

                foreach (var lod in model.Lods)
                {
                    foreach (var instance in lod.Instances)
                    {
                        // collect instance
                        package.LodInstances.Add(instance);

                        foreach (var submodel in instance.SubModels)
                        {
                            // collect submodel
                            package.SubModels.Add(submodel);
#if !SHITTY_REBASING
                            if (lowestVertexBase == -1 || (submodel.VertexBaseOffset < lowestVertexBase))
                            {
                                lowestVertexBase = submodel.VertexBaseOffset;
                                luLowestVertexBase[model.VertexType] = lowestVertexBase;
                            }

                            var vertexUpperEnd = submodel.VertexBaseOffset + submodel.VertexOffset + submodel.VertexCount;

                            if (vertexUpperEnd > highestVertexOffset)
                            {
                                highestVertexOffset = vertexUpperEnd;
                                luHighestVertexOffset[model.VertexType] = highestVertexOffset;
                            }

                            if (lowestIndexOffset == -1 || (submodel.IndexOffset < lowestIndexOffset))
                                lowestIndexOffset = submodel.IndexOffset;

                            var indexUpperEnd = submodel.IndexOffset + submodel.IndexCount;

                            if (indexUpperEnd > highestIndexOffset)
                                highestIndexOffset = indexUpperEnd;
#endif
                            var material = submodel.Material;

                            var ownedMaterial = false;
                            var packagedMaterial = false;

                            switch (material.UID)
                            {
                            // TODO: verify 100% what's a packaged material or not
                            case 0xFFFD:
                                packagedMaterial = true;
                                break;
                            case 0xF00D:
                            case 0xFFFB:
                            case 0xFFFC:
                            case 0xFFFE:
                            case 0xFFFF:
                                // global material
                                break;
                            case 0xCCCC:
                                // null material
                                break;
                            default:
                                if (material.UID == UID)
                                {
                                    ownedMaterial = true;
                                }
                                break;
                            }

                            // collect any materials we might own
                            if (ownedMaterial || packagedMaterial)
                            {
                                if (!luMaterials.ContainsKey(submodel.Material))
                                {
                                    // get the material ref
                                    var mtlRef = Materials[material.Handle];

                                    // adjust the new handle
                                    material.Handle = (ushort)materialRefs.Count;

                                    // update the UID as well?
                                    if (updateUIDs || spooledVehicleHacks)
                                        material.UID = materialUID;

                                    // add to lookup
                                    luMaterials.Add(submodel.Material, material);

                                    // store material ref
                                    materialRefs.Add(mtlRef);
                                }
                                
                                // remap the material
                                submodel.Material = luMaterials[submodel.Material];
                            }
                        }
                    }
                }
            }
#if !SHITTY_REBASING
            foreach (var kv in luVertexBuffers)
            {
                var idx = kv.Key;
                var newVertexBuffer = kv.Value;

                var oldVertexBuffer = luOldVertexBuffer[idx];
                var lowestVertexBase = luLowestVertexBase[idx];
                var highestVertexOffset = luHighestVertexOffset[idx];

                Debug.WriteLine($"VBUFFER {idx} : Lowest Vertex Base: {lowestVertexBase:X8}, Highest Vertex Offset: {highestVertexOffset:X8}");

                // collect vertices
                var decl = newVertexBuffer.Declaration;
                var count = (highestVertexOffset - lowestVertexBase) + 1;
                var offset = 0;
                var stride = decl.SizeOf;
                var buffer = new byte[count * stride];

                var vertices = oldVertexBuffer.Vertices;

                for (int v = lowestVertexBase; v <= highestVertexOffset; v++)
                {
                    if (v >= vertices.Count)
                    {
                        var newCount = (v - lowestVertexBase);

                        Debug.WriteLine($"**** Adjusting count from {count} to {newCount}");
                        count = newCount;
                        break;
                    }

                    var vertex = vertices[v];

                    Buffer.BlockCopy(vertex.Buffer, 0, buffer, offset, stride);
                    offset += stride;
                }

                newVertexBuffer.AddVertices(buffer, stride, count);
            }

            var indices = IndexBuffer.Indices;
            var nTotalIndices = indices.Length;

            // build index buffer (dunno how many we actually need lmao)
            for (int i = lowestIndexOffset; i < highestIndexOffset + 3; i++)
            {
                if (i >= nTotalIndices)
                    break;

                var idx = indices[i];

                gIndices.Add(idx);
            }
#endif
            // second pass: finalize vertex buffers
            foreach (var model in package.Models)
            {
#if SHITTY_REBASING
                // setup info for the vertex buffer we're appending to
                var vertexBuffer = luVertexBuffers[model.VertexType];
                var vertexBaseOffset = luVertexCounts[model.VertexType];
                var vertexOffset = 0;
#else
                var lowestVertexBase = luLowestVertexBase[model.VertexType];
                var highestVertexCount = luHighestVertexOffset[model.VertexType] - lowestVertexBase;
#endif
                // we need to attach it to our new vertex buffer/model package,
                // but first we need to collect all of its vertices/indices...
                foreach (var lod in model.Lods)
                {
                    foreach (var instance in lod.Instances)
                    {
                        foreach (var submodel in instance.SubModels)
                        {
#if SHITTY_REBASING
                            // watch out for strange scenarios like this
                            if (submodel.ModelPackage != this)
                                throw new Exception("WTF? I don't own this sub-model?!");

                            // rebase the submodel! :D
                            submodel.RebaseTo(package, model, instance, vertexBuffer, vertexBaseOffset, ref vertexOffset, ref gIndices);
#else
                            Debug.WriteLine("**** BEFORE REBASE ****");
                            Debug.WriteLine($"\tVertexBaseOffset: {submodel.VertexBaseOffset:X8}");
                            Debug.WriteLine($"\tVertexOffset: {submodel.VertexOffset:X8}");
                            Debug.WriteLine($"\tVertexCount: {submodel.VertexCount:X8}");
                            Debug.WriteLine($"\tIndexOffset: {submodel.IndexOffset:X8}");
                            Debug.WriteLine($"\tIndexCount: {submodel.IndexCount:X8}");

                            submodel.VertexBaseOffset -= lowestVertexBase;
                            submodel.IndexOffset -= lowestIndexOffset;
#endif
                            Debug.WriteLine("**** AFTER REBASE ****");
                            Debug.WriteLine($"\tVertexBaseOffset: {submodel.VertexBaseOffset:X8}");
                            Debug.WriteLine($"\tVertexOffset: {submodel.VertexOffset:X8}");
                            Debug.WriteLine($"\tVertexCount: {submodel.VertexCount:X8}");
                            Debug.WriteLine($"\tIndexOffset: {submodel.IndexOffset:X8}");
                            Debug.WriteLine($"\tIndexCount: {submodel.IndexCount:X8}");
                        }
                    }
                }

                // finalize vertex buffer
                model.VertexBuffer = luVertexBuffers[model.VertexType];
#if SHITTY_REBASING
                // update base offset
                luVertexCounts[model.VertexType] = (vertexBaseOffset + vertexOffset);
#endif
            }

            // compile index buffer
            var indexBuffer = new IndexBuffer(gIndices.Count);
            var indexOffset = 0;

            // ugly hacks lol
            foreach (var index in gIndices)
            {
                var idx = (short)index;

                if (idx < 0)
                    throw new Exception("You dun GOOFED !!!");

                indexBuffer[indexOffset++] = idx;
            }

            // finalize index buffer
            package.IndexBuffer = indexBuffer;

            // finalize vertex buffer(s)
            package.VertexBuffers = luVertexBuffers.Values.ToList();

            // cleanup thus far...
            luVertexBuffers.Clear();
            luVertexCounts.Clear();

            //
            // STEP 2: Materials
            //

            package.Materials = new List<MaterialDataPC>();
            package.Substances = new List<SubstanceDataPC>();
            package.Textures = new List<TextureDataPC>();

            foreach (var _material in materialRefs)
            {
                // DEEP COPY: all new instances down the line
                var material = CopyCatFactory.GetCopy(_material, CopyClassType.DeepCopy);

                // collect material
                package.Materials.Add(material);

                foreach (var substance in material.Substances)
                {
                    // collect substance
                    package.Substances.Add(substance);

                    foreach (var texture in substance.Textures)
                    {
                        // collect texture
                        package.Textures.Add(texture);
                    }
                }
            }

            //
            // STEP 3: [TODO] Remove all duplicate data - sounds like fun! xD
            //

            // ~Fin :)
            return package;
        }

        protected void ReadVertexDeclarations(Stream stream, ref ModelPackageData detail, out List<VertexBufferInfo> decls)
        {
            var vBuffersCount = detail.VertexDeclsCount;
            var vBuffersOffset = detail.VertexDeclsOffset;
            
            decls = new List<VertexBufferInfo>(vBuffersCount);

            if (vBuffersCount != 0)
            {
                VertexBuffers = new List<VertexBuffer>(vBuffersCount);

                stream.Position = vBuffersOffset;

                var declSize = detail.VertexDeclSize;

                // Populate vertex declarations
                for (int vB = 0; vB < vBuffersCount; vB++)
                {
                    var decl = this.Deserialize<VertexBufferInfo>(stream);

                    // mark uninitialized
                    decl.Type = 0xABCDEF;

                    decls.Add(decl);
                }
            }
            else
            {
                VertexBuffers = null;
            }
        }

        protected void WriteVertexDeclarations(Stream stream, ref ModelPackageData detail, ref List<VertexBufferInfo> decls)
        {
            stream.Position = detail.VertexDeclsOffset;

            for (int vB = 0; vB < detail.VertexDeclsCount; vB++)
            {
                var _decl = decls[vB];

                this.Serialize(stream, ref _decl);
            }
        }

        public static float toTwoByteFloat(int intVal)
        {
            int mant = intVal & 0x03ff;
            int exp = intVal & 0x7c00;
            if (exp == 0x7c00) exp = 0x3fc00;
            else if (exp != 0)
            {
                exp += 0x1c000;
                if (mant == 0 && exp > 0x1c400)
                    return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | exp << 13 | 0x3ff), 0);
            }
            else if (mant != 0)
            {
                exp = 0x1c400;
                do
                {
                    mant <<= 1;
                    exp -= 0x400;
                } while ((mant & 0x400) == 0);
                mant &= 0x3ff;
            }
            return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | (exp | mant) << 13), 0);
        }

        protected void ReadVertices(Stream stream, ref ModelPackageData detail, ref List<VertexBufferInfo> decls)
        {
            int vBufferIdx = 0;

            foreach (var decl in decls)
            {
                var vBuffer = VertexBuffers[vBufferIdx++];
                var vertexLength = vBuffer.Declaration.SizeOf;

                if (decl.VertexLength != vertexLength)
                {
                    if (decl.VertexLength > vertexLength)
                        throw new InvalidOperationException($"Vertex buffer expected vertex size of {vertexLength} but got {decl.VertexLength}.");
                }

                stream.Position = decl.VerticesOffset;

                var buffer = new byte[decl.VerticesLength];
                stream.Read(buffer, 0, decl.VerticesLength);

                if (Platform == PlatformType.Xbox)
                {
                    var newBuffer = new byte[vertexLength * decl.VerticesCount];

                    for (int v = 0; v < decl.VerticesCount; v++)
                    {
                        var offset = (v * vertexLength);

                        // try unpacking positions only?
                        var pos = BitConverter.ToUInt32(buffer, (v * decl.VertexLength));

                        /*
                         pDstVertices[i].n = ( ( ((DWORD)(pSrcVertices[i].n.z *  511.0f)) & 0x3ff ) << 22L ) |
                            ( ( ((DWORD)(pSrcVertices[i].n.y * 1023.0f)) & 0x7ff ) << 11L ) |
                            ( ( ((DWORD)(pSrcVertices[i].n.x * 1023.0f)) & 0x7ff ) <<  0L );*/
                        var x = (pos & 0x7ff) / 1023.0f;
                        var y = ((pos >> 11) & 0x7ff) / 1023.0f;
                        var z = ((pos >> 22) & 0x3ff) / 511.0f;

                        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, newBuffer, offset, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, newBuffer, offset + 4, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, newBuffer, offset + 8, 4);
                    }

                    vBuffer.SetVertices(newBuffer, vertexLength, decl.VerticesCount);
                }
                else
                {
                    vBuffer.SetVertices(buffer, decl.VertexLength, decl.VerticesCount);
                }
            }
        }

        protected void WriteVertices(Stream stream, ref ModelPackageData detail, out List<VertexBufferInfo> decls)
        {
            decls = new List<VertexBufferInfo>();

            var vBuffersOffset = detail.GetVertexBuffersOffset();

            stream.Position = vBuffersOffset;

            foreach (var vBuffer in VertexBuffers)
            {
                var offset = (int)stream.Position;

                var _vBuffer = new VertexBufferInfo() {
                    VerticesCount = vBuffer.Count,
                    VerticesOffset = offset,

                    Type = vBuffer.Type,

                    VertexLength = vBuffer.Declaration.SizeOf,
                };

                vBuffer.WriteTo(stream);

                _vBuffer.VerticesLength = (int)(stream.Position - offset);
                
                decls.Add(_vBuffer);
            }
            
            // make sure we update the materials offset
            detail.MaterialDataOffset = Memory.Align((int)stream.Position, 4096);
        }
        
        protected void ReadIndices(Stream stream, ref ModelPackageData detail)
        {
            var nIndices = detail.IndicesCount;

            if (nIndices != 0)
            {
                stream.Position = detail.IndicesOffset;

                var buffer = new byte[nIndices * 2];
                stream.Read(buffer, 0, buffer.Length);
                
                IndexBuffer = new IndexBuffer(buffer, nIndices);
            }
            else
            {
                IndexBuffer = null;
            }
        }

        protected void WriteIndices(Stream stream, ref ModelPackageData detail)
        {
            stream.Position = detail.IndicesOffset;

            var count = detail.IndicesCount;
            var size = (count * IndexBuffer.Length);

            var buffer = new byte[count * IndexBuffer.Length];

            Memory.Copy(IndexBuffer.Indices, 0, buffer, size);

            stream.Write(buffer, 0, size);
        }
        
        protected void ReadModels(Stream stream, ref ModelPackageData detail, ref List<VertexBufferInfo> vBuffers)
        {
            var luSubModels = new Dictionary<int, int>();
            var luLodInstances = new Dictionary<int, int>();
            
            //
            // Sub models
            //

            stream.Position = detail.SubModelsOffset;

            for (int i = 0; i < detail.SubModelsCount; i++)
            {
                var offset = (int)stream.Position;

                var _subModel = this.Deserialize<SubModelInfo>(stream);

                var subModel = new SubModel() {
                    ModelPackage        = this,

                    PrimitiveType       = (PrimitiveType)_subModel.PrimitiveType,

                    VertexBaseOffset    = _subModel.VertexBaseOffset,
                    VertexOffset        = _subModel.VertexOffset,
                    VertexCount         = _subModel.VertexCount,

                    IndexOffset         = _subModel.IndexOffset,
                    IndexCount          = _subModel.IndexCount,

                    Material            = _subModel.Material,
                };

                luSubModels.Add(offset, i);
                SubModels.Add(subModel);
            }

            //
            // Lod instances
            //

            stream.Position = detail.LodInstancesOffset;

            for (int i = 0; i < detail.LodInstancesCount; i++)
            {
                var offset = (int)stream.Position;

                var _lodInstance = this.Deserialize<LodInstanceInfo>(stream);

                var lodInstance = new LodInstance() {
                    Transform = _lodInstance.Transform,
                    UseTransform = (_lodInstance.UseTransform == 1),

                    Reserved = _lodInstance.Info.Reserved,
                    Handle = _lodInstance.Info.Handle,
                };

                luLodInstances.Add(offset, i);

                if (_lodInstance.SubModelsCount != 0)
                {
                    var subModelsIdx = -1;

                    if (luSubModels.TryGetValue(_lodInstance.SubModelsOffset, out subModelsIdx))
                    {
                        lodInstance.SubModels = SubModels.GetRange(subModelsIdx, _lodInstance.SubModelsCount);

                        foreach (var subModel in lodInstance.SubModels)
                            subModel.LodInstance = lodInstance;
                    }
                }
                
                LodInstances.Add(lodInstance);
            }
            
            //
            // Models
            //

            stream.Position = detail.ModelsOffset;

            for (int p = 0; p < detail.ModelsCount; p++)
            {
                if (VertexBuffers == null)
                {
                    //throw new InvalidOperationException("Uh-oh! There's no vertex buffers for the models to use!");
                    continue;
                }

                var _model = this.Deserialize<ModelInfo>(stream);

                var vBufferIdx = _model.BufferIndex;
                var vBuffer = vBuffers[vBufferIdx];

                // initialize vertex buffer?
                if (vBuffer.Type == 0xABCDEF)
                {
                    // setup the type
                    vBuffer.Type = _model.BufferType;
                    vBuffers[vBufferIdx] = vBuffer;

                    // create the buffer in the desired format
                    var vb = VertexBuffer.Create(detail.Version, vBuffer.Type);

                    // add our new buffer
                    VertexBuffers.Add(vb);
                }
                
                var model = new Model() {
                    UID = _model.UID,
                    Scale = _model.Scale,
                    Flags = _model.Flags,

                    VertexBuffer = VertexBuffers[vBufferIdx],
                    VertexType = _model.BufferType,

                    BoundingBox = stream.Read<BBox>(),
                };

                Models.Add(model);
                
                // 7 LODs per model
                for (int k = 0; k < 7; k++)
                {
                    var _lod = this.Deserialize<LodInfo>(stream);
                    
                    var lod = new Lod(k) {
                        Parent = model,
                        
                        Mask = _lod.Mask,
                        Flags = _lod.Flags,
                    };

                    model.Lods[k] = lod;

                    if (_lod.InstancesCount != 0)
                    {
                        if (k == 6)
                            Debug.WriteLine($"{model.UID}: secondary shadow has mask {lod.Mask:X8}, flags {lod.Flags:X8}");

                        var lodInstancesIdx = -1;

                        if (luLodInstances.TryGetValue(_lod.InstancesOffset, out lodInstancesIdx))
                        {
                            lod.Instances = LodInstances.GetRange(lodInstancesIdx, _lod.InstancesCount);

                            // setup the parents
                            foreach (var lodInst in lod.Instances)
                            {
                                lodInst.Parent = lod;

                                foreach (var subModel in lodInst.SubModels)
                                {
                                    subModel.LodInstance = lodInst;
                                    subModel.Model = model;
                                }
                            }
                        }
                    }   
                }
            }
        }

        protected void WriteModels(Stream stream, ref ModelPackageData detail, ref List<VertexBufferInfo> decls)
        {
            var luLodInstances = new Dictionary<LodInstance, int>();
            var luSubModels = new Dictionary<SubModel, int>();

            //
            // Sub models
            //

            stream.Position = detail.SubModelsOffset;

            foreach (var subModel in SubModels)
            {
                var offset = (int)stream.Position;

                luSubModels.Add(subModel, offset);

                var _subModel = new SubModelInfo() {
                    PrimitiveType = (int)subModel.PrimitiveType,

                    VertexBaseOffset = subModel.VertexBaseOffset,
                    VertexOffset = subModel.VertexOffset,
                    VertexCount = subModel.VertexCount,

                    IndexOffset = subModel.IndexOffset,
                    IndexCount = subModel.IndexCount,

                    Material = subModel.Material,
                };

                this.Serialize(stream, ref _subModel);
            }

            //
            // Lod instances
            //

            stream.Position = detail.LodInstancesOffset;

            foreach (var lodInst in LodInstances)
            {
                var offset = (int)stream.Position;

                luLodInstances.Add(lodInst, offset);

                var subModels = lodInst.SubModels;

                var subModelsCount = subModels.Count;
                var subModelsOffset = (subModelsCount > 0) ? luSubModels[subModels[0]] : 0;

                var _lodInst = new LodInstanceInfo() {
                    SubModelsOffset = subModelsOffset,
                    SubModelsCount = (short)subModelsCount,

                    Transform = lodInst.Transform,

                    UseTransform = (short)((lodInst.UseTransform) ? 1 : 0),

                    Info = new LodInstanceInfo.ExtraInfo() {
                        Handle = (short)lodInst.Handle,
                        Reserved = lodInst.Reserved,
                    },
                };

                this.Serialize(stream, ref _lodInst);
            }

            //
            // Models
            //

            stream.Position = detail.ModelsOffset;

            foreach (var model in Models)
            {
                var _model = new ModelInfo() {
                    UID = model.UID,

                    Scale = model.Scale,

                    BufferType = model.VertexType,
                    BufferIndex = (short)VertexBuffers.IndexOf(model.VertexBuffer),

                    Flags = model.Flags,

                    Reserved = 0,
                };

                this.Serialize(stream, ref _model);

                stream.Write(model.BoundingBox);

                var lods = model.Lods;

                foreach (var lod in lods)
                {
                    var lodInstances = lod.Instances;

                    var lodInstancesCount = lodInstances.Count;
                    var lodInstancesOffset = (lodInstancesCount > 0) ? luLodInstances[lodInstances[0]] : 0;

                    var _lod = new LodInfo() {
                        InstancesCount = lodInstancesCount,
                        InstancesOffset = lodInstancesOffset,

                        Mask = lod.Mask,

                        Flags = lod.Flags,
                        Reserved = 0,
                    };

                    this.Serialize(stream, ref _lod);
                }
            }
        }

        private bool ProcessTextures_Xbox()
        {
            // here we go...build DDS wrappers + depalettize textures
            var luSwizzledTextures = new HashSet<byte[]>();
            var luPalettedTextures = new HashSet<TextureDataPC>();

            // combine palettes and insert new textures
            foreach (var substance in Substances)
            {
                var nPalettes = substance.Palettes.Count;

                if (nPalettes == 0)
                    continue;

                var nCluts = 0;
                var textures = new List<TextureDataPC>();

                if (nPalettes == 8)
                {
                    var texA1 = substance.Textures[0];

                    var texA2 = CopyCatFactory.GetCopy(texA1, CopyClassType.DeepCopy);
                    texA2.Handle += 1;

                    var texB1 = CopyCatFactory.GetCopy(texA2, CopyClassType.DeepCopy);
                    texB1.Handle += 1;

                    var texB2 = CopyCatFactory.GetCopy(texB1, CopyClassType.DeepCopy);
                    texB2.Handle += 1;

                    textures = new List<TextureDataPC>()
                            {
                                //texA1,
                                texA2,
                                texB1,
                                texB2,
                            };

                    nCluts = 2;
                }
                else if (nPalettes == 4)
                {
                    var texA1 = substance.Textures[0];

                    var texA2 = CopyCatFactory.GetCopy(texA1, CopyClassType.DeepCopy);
                    texA2.Handle += 1;

                    textures = new List<TextureDataPC>()
                            {
                                //texA1,
                                texA2,
                            };

                    nCluts = 1;
                }
                else if (nPalettes > 1)
                {
                    var tex = substance.Textures[0];

                    for (int n = 1; n < nPalettes; n++)
                    {
                        var child = CopyCatFactory.GetCopy(tex, CopyClassType.DeepCopy);
                        child.Handle += 1;

                        textures.Add(child);
                        tex = child;
                    }
                }
                else
                {
                    var tex = substance.Textures[0];
                    var num = 1;

                    if (luPalettedTextures.Contains(tex))
                    {
                        var idx = Textures.IndexOf(tex);

                        while (idx != -1)
                        {
                            var clone = CopyCatFactory.GetCopy(tex, CopyClassType.DeepCopy);
                            clone.Handle += num++;

                            tex = clone;

                            var next = Textures.FindIndex((t) => t.GetHashCode() == tex.GetHashCode());

                            if (next == -1)
                            {
                                idx++;
                                break;
                            }

                            idx = next;
                        }

                        Textures.Insert(idx, tex);
                        substance.Textures[0] = tex;
                    }

                    // don't allow dupes
                    luPalettedTextures.Add(tex);
                }

                // replace palettes
                if (nCluts > 0)
                {
                    var cluts = new List<PaletteData>();

                    for (int n = 0; n < nCluts; n++)
                    {
                        var idx = (n * 4);

                        // clone the red clut
                        var clut = substance.Palettes[idx].Clone();

                        // merge in green/blue/alpha cluts
                        for (int k = 1; k < 4; k++)
                            clut.Merge(k, substance.Palettes[idx + k]);

                        // prepare the alpha mask
                        var mask = clut.Clone();

                        // blend each channel with the alpha clut
                        for (int m = 0; m < 4; m++)
                            mask.Blend(m, substance.Palettes[3]);

                        // average everything out and convert it to a mask
                        mask.ToAlphaMask();

                        cluts.Add(clut);
                        cluts.Add(mask);
                    }

                    var index = Palettes.IndexOf(substance.Palettes[0]);

                    foreach (var palette in substance.Palettes)
                        Palettes.Remove(palette);

                    Palettes.InsertRange(index, cluts);

                    substance.Palettes.Clear();
                    substance.Palettes = cluts;
                }

                // add additional textures
                if (textures.Count > 0)
                {
                    var tex = substance.Textures[0];
                    var idx = Textures.IndexOf(tex) + 1;

                    foreach (var texture in textures)
                    {
                        Textures.Insert(idx++, texture);
                        substance.Textures.Add(texture);
                    }
                }
            }

            // process textures into the DDS format
            foreach (var substance in Substances)
            {
                var palettes = substance.Palettes;
                var textures = substance.Textures;

                var numPalettes = palettes.Count;
                var numTextures = textures.Count;

                var useFirstPalette = false;

                if ((numPalettes > 0) && (numPalettes != numTextures))
                {
                    if (numPalettes > 1)
                        throw new InvalidOperationException("URGENT: FIXME!!!");

                    useFirstPalette = true;
                }

                for (int n = 0; n < numTextures; n++)
                {
                    var texture = textures[n];
                    var data = texture.Buffer;

                    if (luSwizzledTextures.Contains(data))
                        continue;

                    var format = TextureFormat.Unpack(texture.ExtraData);

                    var type = (D3DFormat)format.Type;
                    var width = format.USize;
                    var height = format.VSize;
                    var mipmaps = format.MipMaps;

                    if (DDSUtils.HasPalette(type))
                    {
                        var bpp = DDSUtils.GetBytesPerPixel(type);

                        if (bpp != 4)
                            throw new InvalidOperationException("can't process palettes!");

                        var palette = palettes[(useFirstPalette) ? 0 : n];

                        // override the data
                        data = DDSUtils.Depalettize(data, width, height, bpp, palette.Data);

                        switch (type)
                        {
                        case D3DFormat.DXT1:
                        case D3DFormat.DXT2:
                        case D3DFormat.DXT3:
                        case D3DFormat.DXT5:
                            // do not remove mipmaps...
                            break;
                        default:
                            mipmaps = 0; // ooooo that's dirty
                            break;
                        }
                    }

                    var buffer = DDSUtils.EncodeTexture(type, width, height, mipmaps, data);

                    texture.Buffer = buffer;

                    luSwizzledTextures.Add(buffer);
                }
            }

            // cleanup
            luSwizzledTextures.Clear();
            return true;
        }

        private bool ProcessTextures_Wii()
        {
            foreach (var texture in Textures)
            {
                texture.UID = 0x01010101;

                // mark of the devil!!!
                texture.Flags = -666;
            }

            return true;
        }

        private bool ProcessTextures_PS2()
        {
            // TODO
            return false;
        }

        private bool ProcessTextures()
        {
            switch (Platform)
            {
            case PlatformType.PS2:
                return ProcessTextures_PS2();
            case PlatformType.Xbox:
                return ProcessTextures_Xbox();
            case PlatformType.Wii:
                return ProcessTextures_Wii();
            }

            // assume absolutely nothing went wrong :)
            return true;
        }

        protected void ReadMaterials(Stream stream, ref ModelPackageData detail)
        {
            var matDataOffset = detail.MaterialDataOffset;
            var texDataOffset = detail.TextureDataOffset;
            
            if (matDataOffset != 0)
            {
                stream.Position = matDataOffset;
                
                var info = new MaterialPackageData(MaterialPackageType, stream, (Version != 6));
                
                if (info.DataSize == 0)
                {
                    texDataOffset = matDataOffset + info.TextureDataOffset;
                    info.DataSize = (Spooler.Size - matDataOffset);
                }

                Materials = new List<MaterialDataPC>(info.MaterialsCount);
                Substances = new List<SubstanceDataPC>(info.SubstancesCount);
                Palettes = new List<PaletteData>(info.PalettesCount);
                Textures = new List<TextureDataPC>(info.TexturesCount);

                var luSubstanceRefs = new SortedDictionary<int, ReferenceInfo<SubstanceDataPC>>();
                var luPaletteRefs = new SortedDictionary<int, ReferenceInfo<PaletteData>>();
                var luTextureRefs = new SortedDictionary<int, ReferenceInfo<TextureDataPC>>();

                var luSubstances = new Dictionary<int, SubstanceDataPC>();
                var luPalettes = new Dictionary<int, PaletteData>();
                var luTextures = new Dictionary<int, TextureDataPC>();

                var luPaletteOffsets = new Dictionary<PaletteData, int>();
                var luTextureOffsets = new Dictionary<TextureDataPC, int>();
                var luTextureSizes = new Dictionary<int, int>();

                //
                // Textures
                //

                stream.Position = (matDataOffset + info.TexturesOffset);

                for (int t = 0; t < info.TexturesCount; t++)
                {
                    var offset = (int)stream.Position;
                    var _tex = this.Deserialize<TextureInfo>(stream);

                    var tex = new TextureDataPC() {
                        UID = _tex.UID,
                        Handle = _tex.Handle,

                        Type = _tex.Type,

                        Flags = _tex.Flags,

                        Width = _tex.Width,
                        Height = _tex.Height,

                        ExtraData = _tex.Format.ToInt32(Version),
                    };

                    Textures.Add(tex);

                    luTextures.Add(offset - matDataOffset, tex);

                    if (!luTextureOffsets.ContainsKey(tex))
                    {
                        luTextureOffsets.Add(tex, _tex.DataOffset);

                        if (!luTextureSizes.ContainsKey(_tex.DataOffset))
                            luTextureSizes.Add(_tex.DataOffset, _tex.DataSize);
                    }
                }

                //
                // Texture references
                //

                stream.Position = (matDataOffset + info.TextureRefsOffset);

                for (int t = 0; t < info.TextureRefsCount; t++)
                {
                    var offset = (int)stream.Position;
                    var lookup = this.Deserialize<ReferenceInfo<TextureDataPC>>(stream);
                    
                    lookup.Reference = luTextures[lookup.Offset];

                    luTextureRefs.Add(offset - matDataOffset, lookup);
                }

                //
                // texture size HACKS
                //
                if ((Platform == PlatformType.Xbox) || (Platform == PlatformType.Wii))
                {
                    if (info.DataSize == 0)
                    {
                        if (info.PalettesCount > 0)
                            throw new InvalidOperationException("Can't calculate texture data size!");

                        info.DataSize = (Spooler.Size - matDataOffset);
                    }

                    var texDataSize = (info.DataSize - info.TextureDataOffset);

                    TextureDataPC last = null;

                    var dummy = new TextureDataPC() { UID = -1, Handle = -1 };

                    // temporarily use a dummy for the last entry
                    luTextureOffsets.Add(dummy, texDataSize);

                    foreach (var lu in luTextureOffsets)
                    {
                        var texture = lu.Key;

                        if (last != null)
                        {
                            var offset = luTextureOffsets[last];
                            var size = luTextureSizes[offset];

                            if (size == 0)
                                luTextureSizes[offset] = (luTextureOffsets[texture] - offset);
                        }

                        last = texture;
                    }

                    luTextureOffsets.Remove(dummy);
                    dummy = null;
                }

                if (info.HasPalettes)
                {
                    if (info.PackageType != MaterialPackageType.Xbox)
                    {
                        if ((info.PalettesCount + info.PaletteRefsCount) != 0)
                            throw new InvalidOperationException($"Palette format has not been documented yet for this platform ({Platform})!");
                    }

                    //
                    // Palettes
                    //

                    stream.Position = (matDataOffset + info.PalettesOffset);

                    for (int s = 0; s < info.PalettesCount; s++)
                    {
                        var offset = (int)stream.Position;
                        var _palette = this.Deserialize<PaletteInfo>(stream);

                        // setup for now until we can read it later
                        var palette = new PaletteData(ref _palette);

                        Palettes.Add(palette);

                        luPalettes.Add(offset - matDataOffset, palette);

                        if (!luPaletteOffsets.ContainsKey(palette))
                            luPaletteOffsets.Add(palette, _palette.DataOffset + info.DataSize);
                    }

                    //
                    // Palette references
                    //

                    stream.Position = (matDataOffset + info.PaletteRefsOffset);

                    for (int s = 0; s < info.PaletteRefsCount; s++)
                    {
                        var offset = (int)stream.Position;
                        var lookup = this.Deserialize<ReferenceInfo<PaletteData>>(stream);

                        lookup.Reference = luPalettes[lookup.Offset];

                        luPaletteRefs.Add(offset - matDataOffset, lookup);
                    }

                    // populate the palette data
                    foreach (var palette in Palettes)
                    {
                        stream.Position = matDataOffset + luPaletteOffsets[palette];

                        palette.Read(stream);
                    }
                }

                //
                // Substances
                //

                stream.Position = (matDataOffset + info.SubstancesOffset);

                for (int s = 0; s < info.SubstancesCount; s++)
                {
                    var offset = (int)stream.Position;
                    var _substance = this.Deserialize<SubstanceInfo>(stream);

                    var substance = new SubstanceDataPC() {
                        Bin = (RenderBinType)_substance.Bin,

                        Flags = _substance.Flags,

                        TS1 = _substance.TS1,
                        TS2 = _substance.TS2,
                        TS3 = _substance.TS3,

                        TextureFlags = _substance.TextureFlags,
                    };

                    if (_substance.PaletteRefsCount != 0)
                    {
                        substance.Palettes = luPaletteRefs
                            .Where((kv) => kv.Key >= _substance.PaletteRefsOffset)
                            .Take(_substance.PaletteRefsCount)
                            .Select((kv) => kv.Value.Reference)
                            .ToList();
                    }

                    substance.Textures = luTextureRefs
                        .Where((kv) => kv.Key >= _substance.TextureRefsOffset)
                        .Take(_substance.TextureRefsCount)
                        .Select((kv) => kv.Value.Reference)
                        .ToList();

                    Substances.Add(substance);

                    luSubstances.Add(offset - matDataOffset, substance);
                }

                //
                // Substance references
                //

                stream.Position = (matDataOffset + info.SubstanceRefsOffset);

                for (int s = 0; s < info.SubstanceRefsCount; s++)
                {
                    var offset = (int)stream.Position;
                    var lookup = this.Deserialize<ReferenceInfo<SubstanceDataPC>>(stream);

                    lookup.Reference = luSubstances[lookup.Offset];

                    luSubstanceRefs.Add(offset - matDataOffset, lookup);
                }

                //
                // Materials
                //

                stream.Position = (matDataOffset + info.MaterialsOffset);

                for (int m = 0; m < info.MaterialsCount; m++)
                {
                    var _material = this.Deserialize<MaterialInfo>(stream);

                    var material = new MaterialDataPC() {
                        Type = (MaterialType)_material.Type,
                        AnimationSpeed = _material.AnimationSpeed,
                    };

                    material.Substances = luSubstanceRefs
                        .Where((kv) => kv.Key >= _material.SubstanceRefsOffset)
                        .Take(_material.SubstanceRefsCount)
                        .Select((kv) => kv.Value.Reference)
                        .ToList();

                    Materials.Add(material);
                }

                // populate texture buffers
                foreach (var texture in Textures)
                {
                    var texOffset = luTextureOffsets[texture];

                    var offset = (texDataOffset + texOffset);
                    var size = luTextureSizes[texOffset];

                    if (size == 0)
                    {
                        switch (Platform)
                        {
                        case PlatformType.PC:
                        {
                            var header = default(DDSHeader);

                            stream.Position = offset;

                            if (DDSUtils.GetHeaderInfo(stream, ref header))
                                size = (DDSHeader.SizeOf + DDSUtils.GetDataSize(ref header) + 4);
                        } break;

                        case PlatformType.Xbox:
                        case PlatformType.Wii:
                            throw new InvalidOperationException("Texture size was not resolved!");
                        }
                    }

                    var data = new byte[size];

                    if (size != 0)
                    {
                        // copy image data
                        stream.Position = offset;
                        stream.Read(data, 0, size);
                    }

                    texture.Buffer = data;
                }

                ProcessTextures();

                // lookup tables no longer needed
                luSubstances.Clear();
                luTextures.Clear();

                luSubstanceRefs.Clear();
                luTextureRefs.Clear();

                luTextureOffsets.Clear();
                luTextureSizes.Clear();
            }
        }

        protected void WriteMaterials(Stream stream, ref ModelPackageData detail)
        {
            var luSubstances = new Dictionary<SubstanceDataPC, int>();
            var luTextures = new Dictionary<TextureDataPC, int>();

            var luSubstanceRefs = new SortedDictionary<int, ReferenceInfo<SubstanceDataPC>>();
            var luTextureRefs = new SortedDictionary<int, ReferenceInfo<TextureDataPC>>();

            var luTextureOffsets = new Dictionary<TextureDataPC, int>();
            var luTextureSizes = new Dictionary<int, int>();

            var info = new MaterialPackageData(MaterialPackageType, Materials.Count, Substances.Count, Textures.Count, (Version != 6));

            var textureDataOffset = info.TextureDataOffset;
            var textureDataLength = 0;

            detail.TextureDataOffset = (detail.MaterialDataOffset + textureDataOffset);

            //
            // Textures
            //

            stream.Position = (detail.MaterialDataOffset + info.TexturesOffset);

            foreach (var texture in Textures)
            {
                var offset = (int)(stream.Position - detail.MaterialDataOffset);

                luTextures.Add(texture, offset);

                var buffer = texture.Buffer;

                var _texture = new TextureInfo() {
                    UID = texture.UID,
                    Handle = texture.Handle,

                    Flags = (texture.Flags != -666) ? texture.Flags : 0,

                    DataOffset = textureDataLength,
                    DataSize = buffer.Length,

                    Type = texture.Type,

                    Width = (short)texture.Width,
                    Height = (short)texture.Height,

                    Reserved = 0,
                };

                if (texture.ExtraData != 0)
                {
                    _texture.Format = TextureFormat.Unpack(texture.ExtraData);
                    _texture.UsesFormat = true;
                }

                this.Serialize(stream, ref _texture);
                
                luTextureOffsets.Add(texture, _texture.DataOffset);
                luTextureSizes.Add(_texture.DataOffset, _texture.DataSize);

                textureDataLength = Memory.Align(textureDataLength + _texture.DataSize, 128);
            }

            //
            // Texture references
            //

            stream.Position = (detail.MaterialDataOffset + info.TextureRefsOffset);

            foreach (var texture in Textures)
            {
                var offset = (int)(stream.Position - detail.MaterialDataOffset);
                var lookup = new ReferenceInfo<TextureDataPC>(texture, luTextures[texture]);

                this.Serialize(stream, ref lookup);

                luTextureRefs.Add(offset, lookup);
            }

            if (info.HasPalettes)
            {
                // TODO?
                if (info.PalettesCount > 0)
                    Debug.WriteLine($"WARNING: Skipping palette data!");
            }

            //
            // Substances
            //

            stream.Position = (detail.MaterialDataOffset + info.SubstancesOffset);

            foreach (var substance in Substances)
            {
                var offset = (int)(stream.Position - detail.MaterialDataOffset);

                luSubstances.Add(substance, offset);

                var textures = substance.Textures;

                var textureRefsCount = textures.Count;
                var textureRefsOffset = (textureRefsCount > 0)
                    ? luTextureRefs.Single((kv) => kv.Value.Reference == textures[0]).Key : 0;

                var _substance = substance.GetData(false);

                _substance.TextureRefsCount = textureRefsCount;
                _substance.TextureRefsOffset = textureRefsOffset;

                _substance.Reserved = 0;

                this.Serialize(stream, ref _substance);
            }

            //
            // Substance references
            //

            stream.Position = (detail.MaterialDataOffset + info.SubstanceRefsOffset);

            foreach (var substance in Substances)
            {
                var offset = (int)(stream.Position - detail.MaterialDataOffset);
                var lookup = new ReferenceInfo<SubstanceDataPC>(substance, luSubstances[substance]);

                this.Serialize(stream, ref lookup);

                luSubstanceRefs.Add(offset, lookup);
            }

            //
            // Materials
            //

            stream.Position = (detail.MaterialDataOffset + info.MaterialsOffset);

            foreach (var material in Materials)
            {
                var offset = (int)(stream.Position - detail.MaterialDataOffset);

                var substances = material.Substances;

                var substanceRefsCount = substances.Count;
                var substanceRefsOffset = (substanceRefsCount > 0)
                    ? luSubstanceRefs.Single((kv) => kv.Value.Reference == substances[0]).Key : 0;

                var _material = new MaterialInfo() {
                    SubstanceRefsCount = substanceRefsCount,
                    SubstanceRefsOffset = substanceRefsOffset,

                    Type = (int)material.Type,

                    AnimationSpeed = material.AnimationSpeed,
                };

                this.Serialize(stream, ref _material);
            }

            //
            // Texture data
            //

            stream.Position = (detail.MaterialDataOffset + info.TextureDataOffset);

            foreach (var lu in luTextureOffsets)
            {
                var texture = lu.Key;

                var offset = (lu.Value + textureDataOffset);
                var size = luTextureSizes[lu.Value];

                stream.Position = (detail.MaterialDataOffset + offset);
                stream.Write(texture.Buffer, 0, size);
            }

            info.DataSize = (int)(stream.Position - detail.MaterialDataOffset);

            //
            // Header
            //

            stream.Position = detail.MaterialDataOffset;

            this.Serialize(stream, ref info);

            // cleanup
            luSubstances.Clear();
            luTextures.Clear();

            luSubstanceRefs.Clear();
            luTextureRefs.Clear();

            luTextureOffsets.Clear();
            luTextureSizes.Clear();
        }

        protected void LoadPS2(Stream stream)
        {
            // do nothing for now
            return;
        }

        protected override void Load()
        {
            switch (Spooler.Context)
            {
            case MGX_ModelPackagePC:
            case MGX_ModelPackageXN:
                Platform = PlatformType.PC;
                break;
            case MGX_ModelPackagePS2:
                Platform = PlatformType.PS2;
                break;
            case MGX_ModelPackageXBox:
                Platform = PlatformType.Xbox;
                break;
            case MGX_ModelPackageWii:
                Platform = PlatformType.Wii;
                break;
            }
            
            Version = Spooler.Version;

            using (var stream = Spooler.GetMemoryStream())
            {
                if (LoadLevel >= ModelPackageLoadLevel.Materials)
                {
                    // initialize everything as empty
                    Materials = new List<MaterialDataPC>();
                    Substances = new List<SubstanceDataPC>();
                    Palettes = new List<PaletteData>();
                    Textures = new List<TextureDataPC>();

                    if (LoadLevel >= ModelPackageLoadLevel.Models)
                    {
                        Models = new List<Model>();
                        LodInstances = new List<LodInstance>();
                        SubModels = new List<SubModel>();

                        VertexBuffers = new List<VertexBuffer>();
                    }
                }

                if (Platform == PlatformType.PS2)
                {
                    LoadPS2(stream);
                }
                else
                {
                    if (Platform == PlatformType.Wii)
                        StreamExtensions.UseBigEndian = true;

                    var detail = this.Deserialize<ModelPackageData>(stream);

                    UID = detail.UID;
                    Handle = detail.Handle;

                    if (LoadLevel >= ModelPackageLoadLevel.Materials)
                    {
                        if (Platform != PlatformType.Wii)
                            ReadMaterials(stream, ref detail);

                        if (LoadLevel >= ModelPackageLoadLevel.Models)
                        {
                            //if (Platform != PlatformType.Xbox)
                            {
                                Models = new List<Model>(detail.ModelsCount);
                                LodInstances = new List<LodInstance>(detail.LodInstancesCount);
                                SubModels = new List<SubModel>(detail.SubModelsCount);

                                // skip packages with no models
                                if (detail.ModelsCount > 0)
                                {
                                    List<VertexBufferInfo> decls = null;

                                    ReadVertexDeclarations(stream, ref detail, out decls);
                                    ReadModels(stream, ref detail, ref decls);

                                    ReadVertices(stream, ref detail, ref decls);
                                    ReadIndices(stream, ref detail);
                                }
                            }
                        }
                    }

                    if (Platform == PlatformType.Wii)
                        StreamExtensions.UseBigEndian = false;
                }
            }
        }
        
        protected override void Save()
        {
            if (Platform == PlatformType.Wii)
                throw new InvalidOperationException("Can't save Wii model packages!");

            // initialize with no models
            var detail = new ModelPackageData(Version, UID);

            if (Models.Count > 0)
            {
                detail = new ModelPackageData(Version, UID,
                    Models.Count, LodInstances.Count, SubModels.Count, IndexBuffer.Indices.Length, VertexBuffers.Count);
            }

            detail.Handle = (ushort)Handle;

            byte[] buffer = null;

            using (var stream = new MemoryStream())
            {
                if (detail.ModelsCount > 0)
                {
                    List<VertexBufferInfo> decls = null;

                    WriteIndices(stream, ref detail);

                    WriteVertices(stream, ref detail, out decls);
                    WriteVertexDeclarations(stream, ref detail, ref decls);

                    WriteModels(stream, ref detail, ref decls);
                }
                
                WriteMaterials(stream, ref detail);

                //
                // Header
                //

                stream.Position = 0;
                this.Serialize(stream, ref detail);

                buffer = stream.ToArray();
            }

            Debug.WriteLine($"buffer length: {buffer.Length:X8}");
            Array.Resize(ref buffer, Memory.Align(buffer.Length, 4096));
            Debug.WriteLine($">> aligned: {buffer.Length:X8}");

            Spooler.SetBuffer(buffer);
        }

        public void LoadMaterials(XmlElement elem)
        {
            throw new NotImplementedException();
        }

        public void SaveMaterials(XmlElement parent)
        {
            var xmlDoc = parent.OwnerDocument;

            parent.SetAttribute("Version", Version.ToString());

            foreach (var material in Materials)
            {
                var mat = xmlDoc.CreateElement("Material");

                if (material.Type == MaterialType.Animated)
                    mat.SetAttribute("AnimationSpeed", material.AnimationSpeed.ToString());
                
                foreach (var substance in material.Substances)
                {
                    var sub = xmlDoc.CreateElement("Substance");

                    sub.SetAttribute("Bin", substance.Bin.ToString());

                    sub.SetAttribute("Flags", substance.Flags.ToString("X8"));

                    sub.SetAttribute("TS1", substance.TS1.ToString());
                    sub.SetAttribute("TS2", substance.TS2.ToString());
                    sub.SetAttribute("TS3", substance.TS3.ToString());

                    sub.SetAttribute("TextureFlags", substance.TextureFlags.ToString("X2"));

                    foreach (var texture in substance.Textures)
                    {
                        var tex = xmlDoc.CreateElement("Texture");

                        tex.SetAttribute("UID", texture.UID.ToString("X8"));
                        tex.SetAttribute("Handle", texture.Handle.ToString("X8"));
                        tex.SetAttribute("Type", texture.Type.ToString());
                        tex.SetAttribute("Width", texture.Width.ToString());
                        tex.SetAttribute("Height", texture.Height.ToString());
                        tex.SetAttribute("Flags", texture.Flags.ToString());
                        tex.SetAttribute("ExtraData", texture.ExtraData.ToString("X8"));

                        sub.AppendChild(tex);
                    }

                    mat.AppendChild(sub);
                }

                parent.AppendChild(mat);
            }
        }
    }
}
