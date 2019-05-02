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

using System.Xml;
using System.Xml.Linq;

namespace DSCript.Models
{
    public class ModelPackage : ModelPackageResource
    {
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
                    var decl = Deserialize<VertexBufferInfo>(stream);

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

                Serialize(stream, ref _decl);
            }
        }

        protected void ReadVertices(Stream stream, ref ModelPackageData detail, ref List<VertexBufferInfo> decls)
        {
            int vBufferIdx = 0;

            foreach (var decl in decls)
            {
                var vBufferType = decl.Type;

                if (vBufferType == 0xABCDEF)
                    throw new InvalidOperationException("Can't create vertices; one or more vertex buffers are uninitialized!");

                var vBuffer = VertexBuffers[vBufferIdx++];
                var vertexLength = vBuffer.Declaration.SizeOf;

                // Too early. Come back later.
                if (decl.VertexLength != vertexLength)
                    throw new InvalidOperationException($"Vertex buffer expected vertex size of {vertexLength} but got {decl.VertexLength}.");

                stream.Position = decl.VerticesOffset;

                var buffer = new byte[decl.VerticesLength];
                stream.Read(buffer, 0, decl.VerticesLength);

                vBuffer.CreateVertices(buffer, decl.VerticesCount);
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

                var _subModel = Deserialize<SubModelInfo>(stream);

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

                var _lodInstance = Deserialize<LodInstanceInfo>(stream);

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
                    throw new InvalidOperationException("Uh-oh! There's no vertex buffers for the models to use!");

                var _model = Deserialize<ModelInfo>(stream);

                var vBufferIdx = _model.BufferIndex;
                var vBuffer = vBuffers[vBufferIdx];
                
                if (vBuffer.Type == 0xABCDEF)
                {
                    // setup the type
                    vBuffer.Type = _model.BufferType;
                    vBuffers[vBufferIdx] = vBuffer;
                }

                VertexBuffer vb = null;

                // initialize vertex buffer?
                if (_model.BufferIndex == VertexBuffers.Count)
                {
                    vb = VertexBuffer.Create(detail.Version, vBuffer.Type);

                    VertexBuffers.Add(vb);
                }
                else
                {
                    vb = VertexBuffers[_model.BufferIndex];
                }

                var model = new Model() {
                    UID = _model.UID,
                    Scale = _model.Scale,
                    Flags = _model.Flags,

                    VertexBuffer = vb,
                    VertexType = _model.BufferType,

                    BoundingBox = stream.Read<BBox>(),
                };

                Models.Add(model);
                
                // 7 LODs per model
                for (int k = 0; k < 7; k++)
                {
                    var _lod = Deserialize<LodInfo>(stream);
                    
                    var lod = new Lod(k) {
                        Parent = model,
                        
                        Mask = _lod.Mask,
                        Flags = _lod.Flags,
                    };

                    model.Lods[k] = lod;

                    if (_lod.InstancesCount != 0)
                    {
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

                Serialize(stream, ref _subModel);
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

                    Info = new LodInstanceInfo.DebugInfo() {
                        Handle = (short)lodInst.Handle,
                        Reserved = lodInst.Reserved,
                    },
                };

                Serialize(stream, ref _lodInst);
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

                    BufferIndex = (short)VertexBuffers.IndexOf(model.VertexBuffer),
                    BufferType = (short)model.VertexBuffer.Type,

                    Flags = model.Flags,

                    Reserved = 0,
                };

                Serialize(stream, ref _model);

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

                    Serialize(stream, ref _lod);
                }
            }
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
                Textures = new List<TextureDataPC>(info.TexturesCount);

                var luSubstanceRefs = new SortedDictionary<int, ReferenceInfo<SubstanceDataPC>>();
                var luTextureRefs = new SortedDictionary<int, ReferenceInfo<TextureDataPC>>();

                var luSubstances = new Dictionary<int, SubstanceDataPC>();
                var luTextures = new Dictionary<int, TextureDataPC>();
                
                var luTextureOffsets = new Dictionary<TextureDataPC, int>();
                var luTextureSizes = new Dictionary<int, int>();

                //
                // Textures
                //

                stream.Position = (matDataOffset + info.TexturesOffset);

                for (int t = 0; t < info.TexturesCount; t++)
                {
                    var offset = (int)stream.Position;
                    var _tex = Deserialize<TextureInfo>(stream);

                    var tex = new TextureDataPC() {
                        UID = _tex.UID,
                        Hash = _tex.Hash,

                        Type = _tex.Type,

                        Width = _tex.Width,
                        Height = _tex.Height,

                        Flags = _tex.Flags,
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

                stream.Position = (matDataOffset + info.TextureLookupOffset);

                for (int t = 0; t < info.TextureLookupCount; t++)
                {
                    var offset = (int)stream.Position;
                    var lookup = Deserialize<ReferenceInfo<TextureDataPC>>(stream);
                    
                    lookup.Reference = luTextures[lookup.Offset];

                    luTextureRefs.Add(offset - matDataOffset, lookup);
                }

                //
                // Substances
                //

                stream.Position = (matDataOffset + info.SubstancesOffset);

                for (int s = 0; s < info.SubstancesCount; s++)
                {
                    var offset = (int)stream.Position;
                    var _substance = Deserialize<SubstanceInfo>(stream);

                    var substance = new SubstanceDataPC() {
                        Bin = _substance.Bin,

                        Flags = _substance.Flags,
                        
                        Mode = (_substance.R1 | (_substance.R2 << 8)),
                        Type = (_substance.R3 | (_substance.TextureFlags << 8)),
                    };

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

                stream.Position = (matDataOffset + info.SubstanceLookupOffset);

                for (int s = 0; s < info.SubstanceLookupCount; s++)
                {
                    var offset = (int)stream.Position;
                    var lookup = Deserialize<ReferenceInfo<SubstanceDataPC>>(stream);

                    lookup.Reference = luSubstances[lookup.Offset];

                    luSubstanceRefs.Add(offset - matDataOffset, lookup);
                }

                //
                // Materials
                //

                stream.Position = (matDataOffset + info.MaterialsOffset);

                for (int m = 0; m < info.MaterialsCount; m++)
                {
                    var _material = Deserialize<MaterialInfo>(stream);

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
                foreach (var lu in luTextureOffsets)
                {
                    var texture = lu.Key;

                    var offset = (texDataOffset + lu.Value);
                    var size = luTextureSizes[lu.Value];

                    // thanks, reflections!
                    if (size == 0)
                    {
                        var header = default(DDSHeader);

                        stream.Position = offset;

                        if (!DDSUtils.GetHeaderInfo(stream, ref header))
                            throw new InvalidDataException("Can't determine data size of texture!");

                        size = (DDSHeader.SizeOf + DDSUtils.GetDataSize(ref header) + 4);
                    }

                    var buffer = new byte[size];

                    stream.Position = offset;
                    stream.Read(buffer, 0, size);

                    texture.Buffer = buffer;
                }

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
                    Hash = texture.Hash,

                    DataOffset = textureDataLength,
                    DataSize = buffer.Length,

                    Type = texture.Type,

                    Width = (short)texture.Width,
                    Height = (short)texture.Height,

                    Flags = texture.Flags,

                    Reserved = 0,
                };

                Serialize(stream, ref _texture);
                
                luTextureOffsets.Add(texture, _texture.DataOffset);
                luTextureSizes.Add(_texture.DataOffset, _texture.DataSize);

                textureDataLength = Memory.Align(textureDataLength + _texture.DataSize, 128);
            }

            //
            // Texture references
            //

            stream.Position = (detail.MaterialDataOffset + info.TextureLookupOffset);

            foreach (var texture in Textures)
            {
                var offset = (int)(stream.Position - detail.MaterialDataOffset);
                var lookup = new ReferenceInfo<TextureDataPC>(texture, luTextures[texture]);

                Serialize(stream, ref lookup);

                luTextureRefs.Add(offset, lookup);
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

                var _substance = new SubstanceInfo() {
                    Bin = (byte)substance.Bin,

                    Flags = substance.Flags,

                    R1 = (byte)(substance.Mode & 0xFF),
                    R2 = (byte)((substance.Mode >> 8) & 0xFF),
                    R3 = (byte)(substance.Type & 0xFF),

                    TextureFlags = (byte)((substance.Type >> 8) & 0xFF),

                    TextureRefsCount = textureRefsCount,
                    TextureRefsOffset = textureRefsOffset,

                    Reserved = 0,
                };

                Serialize(stream, ref _substance);
            }

            //
            // Substance references
            //

            stream.Position = (detail.MaterialDataOffset + info.SubstanceLookupOffset);

            foreach (var substance in Substances)
            {
                var offset = (int)(stream.Position - detail.MaterialDataOffset);
                var lookup = new ReferenceInfo<SubstanceDataPC>(substance, luSubstances[substance]);

                Serialize(stream, ref lookup);

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

                Serialize(stream, ref _material);
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

            Serialize(stream, ref info);

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
                if (Platform == PlatformType.PS2)
                {
                    LoadPS2(stream);
                }
                else
                {
                    if (Platform == PlatformType.Wii)
                        StreamExtensions.UseBigEndian = true;

                    var detail = Deserialize<ModelPackageData>(stream);

                    UID = detail.UID;

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

                    if (Platform != PlatformType.Wii)
                        ReadMaterials(stream, ref detail);

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
                Serialize(stream, ref detail);

                buffer = stream.ToArray();
            }

            Array.Resize(ref buffer, Memory.Align(buffer.Length, 4096));

            Spooler.SetBuffer(buffer);
        }

        public void LoadMaterials(XmlElement elem)
        {
            throw new NotImplementedException();
        }

        public void SaveMaterials(XmlElement parent)
        {
            var xmlDoc = parent.OwnerDocument;
            
            foreach (var material in Materials)
            {
                var mat = xmlDoc.CreateElement("Material");

                if (material.Type == MaterialType.Animated)
                    mat.SetAttribute("AnimationSpeed", material.AnimationSpeed.ToString());
                
                foreach (var substance in material.Substances)
                {
                    var sub = xmlDoc.CreateElement("Substance");

                    sub.SetAttribute("Flags", substance.Flags.ToString("X8"));

                    sub.SetAttribute("Mode", substance.Mode.ToString("X4"));
                    sub.SetAttribute("Type", substance.Type.ToString("X4"));
                    
                    foreach (var texture in substance.Textures)
                    {
                        var tex = xmlDoc.CreateElement("Texture");

                        tex.SetAttribute("UID", texture.UID.ToString("X8"));
                        tex.SetAttribute("Hash", texture.Hash.ToString("X8"));
                        tex.SetAttribute("Type", texture.Type.ToString());
                        tex.SetAttribute("Width", texture.Width.ToString());
                        tex.SetAttribute("Height", texture.Height.ToString());
                        tex.SetAttribute("Flags", texture.Flags.ToString());

                        sub.AppendChild(tex);
                    }

                    mat.AppendChild(sub);
                }

                parent.AppendChild(mat);
            }
        }
    }
}
