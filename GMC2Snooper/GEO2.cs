using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GMC2Snooper
{
    public struct GEO2LodData : IClassDetail<Lod>
    {
        public Vector4 Scale;

        public short LodInstanceCount;
        public short LodMask;

        public int LodInstanceDataOffset;
        public int NumTriangles;
        public int Reserved;

        public Lod ToClass()
        {
            return new Lod() {
                IsDummy = (LodInstanceCount == 0),

                Mask = LodMask,
                NumTriangles = NumTriangles,

                Scale = Scale,

                Instances = new List<LodInstance>(LodInstanceCount),
            };
        }

        public GEO2LodData(Stream stream)
        {
            Scale = stream.Read<Vector4>();

            LodInstanceCount = stream.ReadInt16();
            LodMask = stream.ReadInt16();

            LodInstanceDataOffset = stream.ReadInt32();
            NumTriangles = stream.ReadInt32();
            Reserved = stream.ReadInt32();
        }
    }

    public struct GEO2LodInstanceData
    {
        public int RotationOffset;
        public int TranslationOffset;

        public int SubModelOffset;

        public GEO2LodInstanceData(Stream stream)
        {
            RotationOffset = stream.ReadInt32();
            TranslationOffset = stream.ReadInt32();
            SubModelOffset = stream.ReadInt32();
        }
    }
    
    public struct GEO2SubModelData : IClassDetail<SubModel>
    {
        public Vector3 BoxOffset;

        public short TextureId;
        public short TextureSource;

        public Vector3 BoxScale;

        public int Unknown_1C; // always zero?

        public short DataSizeDiv; // size / 10

        public byte Type; // same as GEO2.Type?
        public byte Flags;

        public int DataOffset;

        public int Unknown_28; // always zero?
        public int Unknown_2C; // always zero?

        public SubModel ToClass()
        {
            return new SubModel() {
                HasBoundBox = true,
            
                BoxOffset = BoxOffset,
                BoxScale = BoxScale,
            
                TextureId = TextureId,
                TextureSource = TextureSource,
            
                Type = Type,
                Flags = Flags,
            
                Unknown1 = Unknown_1C,
                Unknown2 = Unknown_28,
            };
        }

        public GEO2SubModelData(Stream stream)
        {
            BoxOffset = stream.Read<Vector3>();

            TextureId = stream.ReadInt16();
            TextureSource = stream.ReadInt16();

            BoxScale = stream.Read<Vector3>();

            Unknown_1C = stream.ReadInt32();

            DataSizeDiv = stream.ReadInt16();

            Type = (byte)stream.ReadByte();
            Flags = (byte)stream.ReadByte();

            DataOffset = stream.ReadInt32();

            Unknown_28 = stream.ReadInt32();
            Unknown_2C = stream.ReadInt32();
        }
    }

    public struct GEO2SubModelDataV2 : IClassDetail<SubModel>
    {
        public short TextureId;
        public short TextureSource;

        public int Unknown_04; // always zero?

        public short DataSizeDiv; // size / 10

        public byte Type;
        public byte Flags;

        public int DataOffset;

        public int Unknown_10; // always zero?

        public SubModel ToClass()
        {
            return new SubModel() {
                HasBoundBox = false,

                TextureId = TextureId,
                TextureSource = TextureSource,

                Type = Type,
                Flags = Flags,

                Unknown1 = Unknown_04,
                Unknown2 = Unknown_10,
            };
        }

        public GEO2SubModelDataV2(Stream stream)
        {
            TextureId = stream.ReadInt16();
            TextureSource = stream.ReadInt16();

            Unknown_04 = stream.ReadInt32();

            DataSizeDiv = stream.ReadInt16();

            Type = (byte)stream.ReadByte();
            Flags = (byte)stream.ReadByte();

            DataOffset = stream.ReadInt32();

            Unknown_10 = stream.ReadInt32();
        }
    }

    public struct GEO2ModelData
    {
        public static readonly int Magic = 0x324F4547; // 'GEO2'

        public byte LodCount;
        public byte LodInstanceCount;
        public byte SubModelCount;
        public byte Type;

        public int Handle;
        public int UID;

        public Vector3 BoxOffset;

        public int Unknown_1C; // always zero?

        public Vector3 BoxScale;

        public int Unknown_2C;
        public int Unknown_30; // always zero?
        public int Unknown_34; // non-zero in .DAM files (offset?)

        // zero-padding?
        public int Unknown_38;
        public int Unknown_3C;
        
        public GEO2ModelData(Stream stream)
        {
            var magic = stream.ReadInt32();

            if (magic != Magic)
                throw new InvalidOperationException($"Invalid GEO2 data!");

            LodCount = (byte)stream.ReadByte();
            LodInstanceCount = (byte)stream.ReadByte();
            SubModelCount = (byte)stream.ReadByte();
            Type = (byte)stream.ReadByte();

            Handle = stream.ReadInt32();
            UID = stream.ReadInt32();

            BoxOffset = stream.Read<Vector3>();

            Unknown_1C = stream.ReadInt32();

            BoxScale = stream.Read<Vector3>();

            Unknown_2C = stream.ReadInt32();
            Unknown_30 = stream.ReadInt32();
            Unknown_34 = stream.ReadInt32();

            // might just be alignment padding (0x38 > 0x40)
            Unknown_38 = stream.ReadInt32();
            Unknown_3C = stream.ReadInt32();
        }
    }

    // defines directional transforms
    // e.g. { 0,0,1,0 } for Y means Z is up
    public struct TransformAxis
    {
        public Vector4 X;
        public Vector4 Y;
        public Vector4 Z;

        public TransformAxis(Stream stream)
        {
            X = stream.Read<Vector4>();
            Y = stream.Read<Vector4>();
            Z = stream.Read<Vector4>();
        }
    }

    public class Lod
    {
        // holds no actual data
        public bool IsDummy { get; set; }

        public int Mask { get; set; }
        public int NumTriangles { get; set; }

        public Vector4 Scale { get; set; }

        public List<LodInstance> Instances { get; set; }
    }

    public class LodInstance
    {
        public TransformAxis Rotation { get; set; }
        public Vector4 Translation { get; set; }

        public bool HasRotation { get; set; }
        public bool HasTranslation { get; set; }

        public SubModel Model { get; set; }
    }

    public class SubModel
    {
        public bool HasBoundBox { get; set; }
        
        public Vector3 BoxOffset { get; set; }
        public Vector3 BoxScale { get; set; }

        // TODO: Link to actual texture data
        public int TextureId { get; set; }
        public int TextureSource { get; set; }

        public int Type { get; set; }
        public int Flags { get; set; }

        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }

        // TODO: Read actual model data!
        public byte[] DataBuffer { get; set; }
    }
    
    public class Model
    {
        public int Type { get; set; }
        public bool IsVersion2 { get; set; }

        public int UID { get; set; }
        public int Handle { get; set; }

        public Vector3 BoxOffset { get; set; }
        public Vector3 BoxScale { get; set; }

        public List<Lod> Lods { get; set; }
        public List<LodInstance> LodInstances { get; set; }
        public List<SubModel> SubModels { get; set; }

        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }
        
        public void LoadBinary(Stream stream)
        {
            var baseOffset = stream.Position;

            var data = new GEO2ModelData(stream);

            Type = (data.Type & 0x7F);
            IsVersion2 = (data.Type & 0x80) != 0;

            Handle = data.Handle;
            UID = data.UID;

            BoxOffset = data.BoxOffset;
            BoxScale = data.BoxScale;

            Unknown1 = data.Unknown_2C;
            Unknown2 = data.Unknown_34;
            
            var lodsOffset = (int)(stream.Position - baseOffset);

            Lods = new List<Lod>(data.LodCount);
            LodInstances = new List<LodInstance>(data.LodInstanceCount);
            SubModels = new List<SubModel>(data.SubModelCount);
            
            // process data
            for (int i = 0; i < data.LodCount; i++)
            {
                stream.Position = baseOffset + (lodsOffset + (i * 0x20));

                var _lod = new GEO2LodData(stream);
                var lod = _lod.ToClass();

                Lods.Add(lod);
                
                // nothing to see here, move along
                if (lod.IsDummy)
                    continue;

                for (int l = 0; l < _lod.LodInstanceCount; l++)
                {
                    stream.Position = baseOffset + (_lod.LodInstanceDataOffset + (l * 0xC));

                    var _lodInstance = new GEO2LodInstanceData(stream);

                    if (_lodInstance.SubModelOffset == 0)
                        throw new InvalidOperationException("Invalid LOD instance!");

                    var lodInstance = new LodInstance();

                    lod.Instances.Add(lodInstance);
                    LodInstances.Add(lodInstance);

                    if (_lodInstance.RotationOffset != 0)
                    {
                        stream.Position = (baseOffset + _lodInstance.RotationOffset);

                        lodInstance.Rotation = new TransformAxis(stream);
                        lodInstance.HasRotation = true;
                    }

                    if (_lodInstance.TranslationOffset != 0)
                    {
                        stream.Position = (baseOffset + _lodInstance.TranslationOffset);

                        lodInstance.Translation = stream.Read<Vector4>();
                        lodInstance.HasTranslation = true;
                    }
                    
                    stream.Position = (baseOffset + _lodInstance.SubModelOffset);
                    
                    IClassDetail<SubModel> subModelDetail = null;

                    var dataOffset = 0;
                    var dataLength = 0;

                    if (IsVersion2)
                    {
                        var _subModel = new GEO2SubModelDataV2(stream);

                        if (_subModel.DataOffset == 0)
                            throw new InvalidOperationException("Invalid sub-model (V2)!");
                        
                        dataOffset = _subModel.DataOffset;
                        dataLength = (_subModel.DataSizeDiv * 10);

                        subModelDetail = _subModel;
                    }
                    else
                    {
                        var _subModel = new GEO2SubModelData(stream);

                        if (_subModel.DataOffset == 0)
                            throw new InvalidOperationException("Invalid sub-model!");
                        
                        dataOffset = _subModel.DataOffset;
                        dataLength = (_subModel.DataSizeDiv * 10);

                        subModelDetail = _subModel;
                    }

                    var subModel = subModelDetail.ToClass();
                    
                    stream.Position = (baseOffset + dataOffset);
                    
                    var buffer = new byte[dataLength];
                    stream.Read(buffer, 0, dataLength);

                    subModel.DataBuffer = buffer;

                    lodInstance.Model = subModel;
                    SubModels.Add(subModel);
                }
            }
        }
    }
}
