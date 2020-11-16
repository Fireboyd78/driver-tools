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

namespace DSCript.Models
{
    public class ModelPackage : ModelPackageResource
    {
        public static bool SkipModelsOnLoad = false;

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
                    var _lod = this.Deserialize<LodInfo>(stream);
                    
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

                    Info = new LodInstanceInfo.DebugInfo() {
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

                    BufferIndex = (short)VertexBuffers.IndexOf(model.VertexBuffer),
                    BufferType = (short)model.VertexBuffer.Type,

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

                    var texA2 = new TextureDataPC()
                    {
                        UID = texA1.UID,
                        Handle = texA1.Handle + 1,
                        Type = texA1.Type,
                        Flags = texA1.Flags,
                        Width = texA1.Width,
                        Height = texA1.Height,
                        Buffer = texA1.Buffer,
                    };

                    var texB1 = new TextureDataPC()
                    {
                        UID = texA2.UID,
                        Handle = texA2.Handle + 1,
                        Type = texA2.Type,
                        Flags = texA2.Flags,
                        Width = texA2.Width,
                        Height = texA2.Height,
                        Buffer = texA2.Buffer,
                    };

                    var texB2 = new TextureDataPC()
                    {
                        UID = texB1.UID,
                        Handle = texB1.Handle + 1,
                        Type = texB1.Type,
                        Flags = texB1.Flags,
                        Width = texB1.Width,
                        Height = texB1.Height,
                        Buffer = texB1.Buffer,
                    };

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

                    var texA2 = new TextureDataPC()
                    {
                        UID = texA1.UID,
                        Handle = texA1.Handle + 1,
                        Type = texA1.Type,
                        Flags = texA1.Flags,
                        Width = texA1.Width,
                        Height = texA1.Height,
                        Buffer = texA1.Buffer,
                    };

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
                        var child = new TextureDataPC()
                        {
                            UID = tex.UID,
                            Handle = tex.Handle + 1,
                            Type = tex.Type,
                            Flags = tex.Flags,
                            Width = tex.Width,
                            Height = tex.Height,
                            Buffer = tex.Buffer,
                        };

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
                            var clone = new TextureDataPC()
                            {
                                UID = tex.UID,
                                Handle = tex.Handle + num++,
                                Type = tex.Type,
                                Flags = tex.Flags,
                                Width = tex.Width,
                                Height = tex.Height,
                                Buffer = tex.Buffer,
                            };

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

                        // copy the red channel and prepare a mask
                        var clut = substance.Palettes[idx].Clone();
                        var mask = new PaletteData(clut.Count);

                        for (int k = 1; k < 4; k++)
                        {
                            // merge green/blue/alpha
                            clut.Merge(substance.Palettes[idx + k], k);

                            // setup mask
                            var m = (k - 1);

                            mask.Merge(substance.Palettes[idx + m], m);
                            mask.Blend(substance.Palettes[3], m);
                        }

                        // and finally, set the mask up by using the alpha
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

                    var format = (D3DFormat)((texture.Flags >> 8) & 0xFF);
                    var mipmaps = (texture.Flags >> 16) & 0xF;

                    var width = texture.Width;
                    var height = texture.Height;

                    if (DDSUtils.HasPalette(format))
                    {
                        var bpp = DDSUtils.GetBytesPerPixel(format);

                        if (bpp != 4)
                            throw new InvalidOperationException("can't process palettes!");

                        var palette = palettes[(useFirstPalette) ? 0 : n];

                        // override the data
                        data = DDSUtils.Depalettize(data, width, height, bpp, palette.Data);
                        mipmaps = 0; // ooooo that's dirty
                    }

                    var buffer = DDSUtils.EncodeTexture(format, width, height, mipmaps, data);

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
                        
                        Mode = (_substance.TS1 | (_substance.TS2 << 8)),
                        Type = (_substance.TS3 | (_substance.TextureFlags << 8)),
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

                    DataOffset = textureDataLength,
                    DataSize = buffer.Length,

                    Type = texture.Type,

                    Width = (short)texture.Width,
                    Height = (short)texture.Height,

                    Flags = texture.Flags,

                    Reserved = 0,
                };

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
                // initialize everything as empty
                Models = new List<Model>();
                LodInstances = new List<LodInstance>();
                SubModels = new List<SubModel>();

                VertexBuffers = new List<VertexBuffer>();

                Materials = new List<MaterialDataPC>();
                Substances = new List<SubstanceDataPC>();
                Palettes = new List<PaletteData>();
                Textures = new List<TextureDataPC>();

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

                    if ((Platform != PlatformType.Xbox) && !SkipModelsOnLoad)
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
                        tex.SetAttribute("Hash", texture.Handle.ToString("X8"));
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
