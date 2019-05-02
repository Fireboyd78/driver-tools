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

using Antilli.Parser;

namespace Antilli
{
    public class AntilliScene : IDetailProvider
    {
        public enum ModelType
        {
            Vehicle = 1,
            Character = 2,
            Static = 3,
            Prop = 4,
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
            public int Slot;

            public int Mask;
            public int Flags;

            public List<LodInstance> Instances { get; set; }

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                Slot = stream.ReadByte();
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

                stream.WriteByte(Slot);
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

        public static readonly MagicNumber Magic = "ANTILLI!";

        PlatformType IDetailProvider.Platform => PlatformType.Any;

        public int Version { get; set; }

        public int Flags { get; set; }
        
        public int UID { get; set; }
        
        public List<Model> Models { get; set; }

        public VertexBuffer VertexBuffer { get; set; }
        public IndexBuffer IndexBuffer { get; set; }

        TDetail IDetailProvider.Deserialize<TDetail>(Stream stream)
        {
            return Deserialize<TDetail>(stream);
        }

        void IDetailProvider.Serialize<TDetail>(Stream stream, ref TDetail detail)
        {
            Serialize(stream, ref detail);
        }

        protected TDetail Deserialize<TDetail>(Stream stream)
            where TDetail : IDetail, new()
        {
            var result = new TDetail();
            result.Deserialize(stream, this);

            return result;
        }

        protected void Serialize<TDetail>(Stream stream, ref TDetail detail)
            where TDetail : IDetail
        {
            detail.Serialize(stream, this);
        }

        protected ModelType GetModelType(int vertexType)
        {
            switch (vertexType)
            {
            case 1: return ModelType.Static;
            case 5: return ModelType.Vehicle;
            case 6: return ModelType.Character;
            }

            throw new InvalidDataException($"Vertex type {vertexType} not implemented!");
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

                Serialize(stream, ref model);
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

            var vertexDeclOffset = stream.ReadInt32();

            var indicesCount = stream.ReadInt32();
            var indicesLength = stream.ReadInt32();
            var indicesOffset = stream.ReadInt32();
            
            //
            // Models
            //

            stream.Position = modelsOffset;

            for (int i = 0; i < modelsCount; i++)
            {
                var model = Deserialize<Model>(stream);

                Models.Add(model);
            }
        }

        public AntilliScene()
        {
            Version = 3;
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
                        Slot = _lod.ID,

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
