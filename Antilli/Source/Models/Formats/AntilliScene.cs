using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using DSCript;
using DSCript.Models;
using DSCript.Parser;
using DSCript.Spooling;

using Game = DSCript.Models;

namespace Antilli
{
    public class AntilliScene : IDetailProvider
    {
        public enum ModelType
        {
            World,

            Static,
            StaticLit,
            StaticUnlit,

            Vehicle,
            Character,

            HyperLow,
        }
        
        public class Model : IDetail
        {
            public UID UID;
            
            public Vector4 Scale;
            
            public ModelType Type;

            public BBox BoundingBox;

            public List<Lod> Lods { get; set; }
            
            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                UID = stream.Read<UID>();
                Scale = stream.Read<Vector4>();
                
                Type = (ModelType)stream.ReadInt32();
                
                BoundingBox = stream.Read<BBox>();

                var count = stream.ReadInt32();

                Lods = new List<Lod>(count);

                for (int i = 0; i < count; i++)
                {
                    var lod = provider.Deserialize<Lod>(stream);

                    Lods.Add(lod);
                }
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                var count = Lods.Count;

                stream.Write(UID);
                stream.Write(Scale);

                stream.Write((int)Type);

                stream.Write(BoundingBox);

                stream.Write(count);

                for (int i = 0; i < count; i++)
                {
                    var lod = Lods[i];

                    provider.Serialize(stream, ref lod);
                }
            }
        }

        public class Lod : IDetail
        {
            public int Type;

            public int Mask;
            public int Flags;

            public List<LodInstance> Instances { get; set; }

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                Type = stream.ReadByte();

                Mask = stream.ReadByte();
                Flags = stream.ReadByte();

                var count = stream.ReadByte();

                Instances = new List<LodInstance>(count);

                for (int i = 0; i < count; i++)
                {
                    var lodInst = provider.Deserialize<LodInstance>(stream);

                    Instances.Add(lodInst);
                }
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                var count = Instances.Count;

                stream.WriteByte(Type);

                stream.WriteByte(Mask);
                stream.WriteByte(Flags);

                stream.WriteByte(count);

                for (int i = 0; i < count; i++)
                {
                    var lodInst = Instances[i];

                    provider.Serialize(stream, ref lodInst);
                }
            }
        }

        public class LodInstance : IDetail
        {
            public Matrix44 Transform;

            public bool UseTransform;

            public int Handle;

            public List<SubModel> SubModels { get; set; }

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                Transform = stream.Read<Matrix44>();

                UseTransform = (stream.ReadByte() == 1);

                var count = stream.ReadByte();

                Handle = stream.ReadInt16();

                SubModels = new List<SubModel>(count);

                for (int i = 0; i < count; i++)
                {
                    var subModel = provider.Deserialize<SubModel>(stream);

                    SubModels.Add(subModel);
                }
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                var count = SubModels.Count;

                stream.Write(Transform);

                stream.WriteByte(UseTransform ? 1 : 0);
                stream.WriteByte(count);

                stream.Write((short)Handle);

                for (int i = 0; i < count; i++)
                {
                    var subModel = SubModels[i];

                    provider.Serialize(stream, ref subModel);
                }
            }
        }

        public class SubModel : IDetail
        {
            public PrimitiveType Type;

            public int VertexBaseOffset;

            public int VertexOffset;
            public int VertexCount;

            public int IndexOffset;
            public int IndexCount;

            public MaterialHandle Material;

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                Type = (PrimitiveType)stream.ReadInt32();

                VertexBaseOffset = stream.ReadInt32();
                VertexOffset = stream.ReadInt32();
                VertexCount = stream.ReadInt32();

                IndexOffset = stream.ReadInt32();
                IndexCount = stream.ReadInt32();

                Material = stream.Read<MaterialHandle>();
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                stream.Write((int)Type);

                stream.Write(VertexBaseOffset);
                stream.Write(VertexOffset);
                stream.Write(VertexCount);

                stream.Write(IndexOffset);
                stream.Write(IndexCount);

                stream.Write(Material);
            }
        }

        public class Material : IDetail
        {
            public MaterialType Type;

            public float AnimationSpeed;
            
            public List<Substance> Substances { get; set; }
            
            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                Type = (MaterialType)stream.ReadInt32();
                AnimationSpeed = stream.ReadSingle();

                var count = stream.ReadInt32();

                Substances = new List<Substance>(count);

                for (int i = 0; i < count; i++)
                {
                    var substance = provider.Deserialize<Substance>(stream);

                    Substances.Add(substance);
                }
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                stream.Write((int)Type);
                stream.Write(AnimationSpeed);

                var count = Substances.Count;

                stream.Write(count);

                for (int i = 0; i < count; i++)
                {
                    var substance = Substances[i];

                    provider.Serialize(stream, ref substance);
                }
            }
        }
        
        public class Substance : IDetail
        {
            public RenderBinType RenderBin;
            public int RenderFlags;

            public int TS1, TS2, TS3;
            public int TextureFlags;

            public List<Texture> Textures { get; set; }
            
            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                var info = stream.ReadInt32();
                
                RenderBin = (RenderBinType)(info & 0xFF);
                RenderFlags = ((info >> 8) & 0xFFFFFF);

                TS1 = stream.ReadByte();
                TS2 = stream.ReadByte();
                TS3 = stream.ReadByte();

                TextureFlags = stream.ReadByte();

                var count = stream.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    var texture = provider.Deserialize<Texture>(stream);

                    Textures.Add(texture);
                }
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                var info = ((int)RenderBin & 0xFF) | ((RenderFlags & 0xFFFFFF) << 8);
                var count = Textures.Count;

                stream.Write(info);

                stream.WriteByte(TS1);
                stream.WriteByte(TS2);
                stream.WriteByte(TS3);
                stream.WriteByte(TextureFlags);

                stream.Write(count);

                for (int i = 0; i < count; i++)
                {
                    var texture = Textures[i];

                    provider.Serialize(stream, ref texture);
                }
            }
        }

        public class Texture : IDetail
        {
            public int UID;
            public int Handle;
            
            public int Type;

            public short Width;
            public short Height;

            public int Flags;

            public byte[] Buffer { get; set; }

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                UID = stream.ReadInt32();
                Handle = stream.ReadInt32();

                Type = stream.ReadInt32();

                Width = stream.ReadInt16();
                Height = stream.ReadInt16();

                Flags = stream.ReadInt32();

                var length = stream.ReadInt32();
                var buffer = new byte[length];

                stream.Read(buffer, 0, length);
            }

            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                stream.Write(UID);
                stream.Write(Handle);

                stream.Write(Type);

                stream.Write(Width);
                stream.Write(Height);

                stream.Write(Flags);

                var length = Buffer.Length;

                stream.Write(length);
                stream.Write(Buffer, 0, length);
            }
        }

        public static readonly MagicNumber Magic = "ANTILLI!";

        PlatformType IDetailProvider.Platform => PlatformType.Generic;

        public int Version { get; set; }

        public int Flags { get; set; }
        
        public int UID { get; set; }
        
        public List<Model> Models { get; set; }

        public VertexBuffer VertexBuffer { get; set; }
        public IndexBuffer IndexBuffer { get; set; }
        
        protected ModelType GetModelType(int vertexType)
        {
            switch (vertexType)
            {
            case 0: return ModelType.World;
            case 1: return ModelType.Static;
            case 5: return ModelType.Vehicle;
            case 6: return ModelType.Character;
            case 7: return ModelType.HyperLow;
            }

            throw new InvalidDataException($"Vertex type {vertexType} not implemented!");
        }

        protected short GetVertexType(ModelType modelType)
        {
            switch (modelType)
            {
            case ModelType.World:       return 0;
            case ModelType.Static:      return 1;
            case ModelType.Vehicle:     return 5;
            case ModelType.Character:   return 6;
            case ModelType.HyperLow:    return 7;
            }

            throw new InvalidDataException($"Model type {modelType} not implemented!");
        }
        
        public void Serialize(Stream stream)
        {
            var modelsCount = Models.Count;
            var modelsOffset = 128;
            
            //
            // Models
            //

            stream.Position = modelsOffset;

            for (int i = 0; i < modelsCount; i++)
            {
                var model = Models[i];

                this.Serialize(stream, ref model);
            }

            //
            // Index buffer
            //

            var indices = IndexBuffer.Indices;

            var indicesOffset = (int)Memory.Align(stream.Position, 2048);
            var indicesCount = indices.Length;

            var indicesLength = (indicesCount * IndexBuffer.Length);
            
            stream.Position = indicesOffset;

            var buffer = new byte[indicesLength];

            Memory.Copy(IndexBuffer.Indices, 0, buffer, indicesLength);

            stream.Write(buffer, 0, indicesLength);

            //
            // Vertex buffer
            //

            var vertexBufferOffset = (int)Memory.Align(stream.Position, 4096);
            
            stream.Position = vertexBufferOffset;

            VertexBuffer.WriteTo(stream, true);
            
            //
            // Header
            //

            stream.Position = 0;

            stream.Write((long)Magic);

            stream.Write(Version);
            stream.Write(Flags);

            stream.Write(UID);

            stream.Write(modelsCount);
            stream.Write(modelsOffset);

            stream.Write(vertexBufferOffset);

            stream.Write(indicesCount);
            stream.Write(indicesLength);
            stream.Write(indicesOffset);
        }

        public void Deserialize(Stream stream)
        {
            if (stream.ReadInt64() != Magic)
                throw new InvalidDataException("Bad magic!");

            Version = stream.ReadInt32();
            Flags = stream.ReadInt32();

            UID = stream.ReadInt32();

            var modelsCount = stream.ReadInt32();
            var modelsOffset = stream.ReadInt32();

            var vertexBufferOffset = stream.ReadInt32();

            var indicesCount = stream.ReadInt32();
            var indicesLength = stream.ReadInt32();
            var indicesOffset = stream.ReadInt32();

            //
            // Models
            //

            Models = new List<Model>(modelsCount);

            stream.Position = modelsOffset;

            for (int i = 0; i < modelsCount; i++)
            {
                var model = this.Deserialize<Model>(stream);

                Models.Add(model);
            }

            //
            // Index Buffer
            //

            stream.Position = indicesOffset;

            var indexBuffer = stream.ReadBytes(indicesLength);

            IndexBuffer = new IndexBuffer(indexBuffer, indicesCount);

            //
            // Vertex buffer
            //

            stream.Position = vertexBufferOffset;

            // reads in declaration + vertex data
            VertexBuffer = VertexBuffer.CreateFromStream(stream);
        }

        public ModelPackage ToModelPackage(PlatformType platform, int version)
        {
            var package = SpoolableResourceFactory.Create<ModelPackage>();

            package.UID = UID;
            package.Platform = platform;
            package.Version = version;

            var gModels = new List<Game.Model>();
            var gLodInstances = new List<Game.LodInstance>();
            var gSubModels = new List<Game.SubModel>();

            var gMaterials = new List<Game.MaterialDataPC>();
            var gSubstances = new List<Game.SubstanceDataPC>();
            var gTextures = new List<Game.TextureDataPC>();

            var lookup = new Dictionary<int, int>();

            VertexBuffer vertexBuffer = null;

            foreach (var _model in Models)
            {
                var model = new Game.Model()
                {
                    UID = _model.UID,
                    Scale = _model.Scale,
                    BoundingBox = _model.BoundingBox,

                    VertexType = GetVertexType(_model.Type),
                };

                if (vertexBuffer == null)
                    vertexBuffer = VertexBuffer.Create(version, model.VertexType);

                model.VertexBuffer = vertexBuffer; // set the vertex buffer!

                gModels.Add(model);

                foreach (var _lod in _model.Lods)
                {
                    var lodType = _lod.Type;

                    var lod = new Game.Lod(lodType)
                    {
                        Mask = _lod.Mask,
                        Flags = _lod.Flags,
                    };

                    model.Lods[lodType] = lod;

                    foreach (var _lodInst in _lod.Instances)
                    {
                        var lodInst = new Game.LodInstance()
                        {
                            Parent = lod,

                            Transform = _lodInst.Transform,
                            UseTransform = _lodInst.UseTransform,

                            Handle = _lodInst.Handle,
                        };

                        lod.Instances.Add(lodInst);
                        gLodInstances.Add(lodInst);

                        foreach (var _subModel in _lodInst.SubModels)
                        {
                            var subModel = new Game.SubModel()
                            {
                                ModelPackage = package,
                                
                                Model = model,
                                LodInstance = lodInst,

                                PrimitiveType = _subModel.Type,

                                VertexBaseOffset = _subModel.VertexBaseOffset,
                                VertexOffset = _subModel.VertexOffset,
                                VertexCount = _subModel.VertexCount,

                                IndexOffset = _subModel.IndexOffset,
                                IndexCount = _subModel.IndexCount,

                                Material = _subModel.Material,
                            };

                            lodInst.SubModels.Add(subModel);
                            gSubModels.Add(subModel);
                        }
                    }
                }
            }

            // copy all vertices over to the new buffer
            VertexBuffer.CopyTo(vertexBuffer);

            package.VertexBuffers = new List<VertexBuffer>() {
                    // only one vertex buffer
                    vertexBuffer,
                };

            package.IndexBuffer = IndexBuffer;

            package.Models = gModels;
            package.LodInstances = gLodInstances;
            package.SubModels = gSubModels;

            package.Materials = new List<Game.MaterialDataPC>();
            package.Substances = new List<Game.SubstanceDataPC>();
            package.Textures = new List<Game.TextureDataPC>();

            //
            // TODO: Materials
            //

            var resource = (ISpoolableResource)package;

            resource.Spooler = new SpoolableBuffer()
            {
                Context = ModelPackageResource.GetChunkId(platform, version),
                Version = version,
                Alignment = SpoolerAlignment.Align4096,
                Description = "Antilli model package",
            };

            return package;
        }

        public AntilliScene()
        {
            Version = 3;
        }

        public AntilliScene(string filename)
        {
            using (var f = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // deserialize models
                Deserialize(f);

                //
                // TODO: Materials
                //
            }
        }

        public AntilliScene(ModelPackageResource modelPackage)
            : this()
        {
            UID = modelPackage.UID;
            Flags = modelPackage.Flags;

            Models = new List<Model>();

            VertexBuffer = modelPackage.VertexBuffers[0];
            IndexBuffer = modelPackage.IndexBuffer;

            foreach (var _model in modelPackage.Models)
            {
                var model = new Model() {
                    UID = _model.UID,
                    Scale = _model.Scale,
                    BoundingBox = _model.BoundingBox,

                    Type = GetModelType(_model.VertexType),

                    Lods = new List<Lod>(),
                };
                
                foreach (var _lod in _model.Lods)
                {
                    var lod = new Lod() {
                        Type = _lod.Type,

                        Mask = _lod.Mask,
                        Flags = _lod.Flags,

                        Instances = new List<LodInstance>(),
                    };

                    model.Lods.Add(lod);

                    foreach (var _lodInst in _lod.Instances)
                    {
                        var lodInst = new LodInstance() {
                            Transform = _lodInst.Transform,
                            UseTransform = _lodInst.UseTransform,

                            Handle = _lodInst.Handle,

                            SubModels = new List<SubModel>(),
                        };

                        lod.Instances.Add(lodInst);

                        foreach (var _subModel in _lodInst.SubModels)
                        {
                            var subModel = new SubModel() {
                                Type = _subModel.PrimitiveType,

                                VertexBaseOffset = _subModel.VertexBaseOffset,
                                VertexOffset = _subModel.VertexOffset,
                                VertexCount = _subModel.VertexCount,

                                IndexOffset = _subModel.IndexOffset,
                                IndexCount = _subModel.IndexCount,

                                Material = _subModel.Material,
                            };

                            lodInst.SubModels.Add(subModel);
                        }
                    }
                }

                Models.Add(model);
            }
        }
    }
}
