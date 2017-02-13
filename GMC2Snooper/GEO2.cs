using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GMC2Snooper
{
    public struct GEO2LodData
    {
        public Vector4 Transform;

        public short LodInstanceCount;
        public short Unknown_12; // flags/count?

        public int LodInstanceDataOffset;
        public int Unknown_18; // count? 
        public int Unknown_20; // always zero?

        public GEO2LodData(Stream stream)
        {
            Transform = new Vector4() {
                X = stream.ReadSingle(),
                Y = stream.ReadSingle(),
                Z = stream.ReadSingle(),
                W = stream.ReadSingle(),
            };

            LodInstanceCount = stream.ReadInt16();
            Unknown_12 = stream.ReadInt16();

            LodInstanceDataOffset = stream.ReadInt32();
            Unknown_18 = stream.ReadInt32();
            Unknown_20 = stream.ReadInt32();
        }
    }

    public struct GEO2LodInstanceData
    {
        public int TransformAxisOffset; // global transform?
        public int Unknown_04; // always zero?

        public int SubModelOffset;

        public GEO2LodInstanceData(Stream stream)
        {
            TransformAxisOffset = stream.ReadInt32();
            Unknown_04 = stream.ReadInt32();
            SubModelOffset = stream.ReadInt32();
        }
    }
    
    public struct GEO2SubModelData
    {
        public Vector3 Transform1;

        public short TextureId;
        public short TextureSource;

        public Vector3 Transform2;

        public int Unknown_1C; // always zero?

        public short DataSizeDiv; // size / 10

        public byte Type; // same as GEO2.Type?
        public byte Flags;

        public int DataOffset;

        public int Unknown_28; // always zero?
        public int Unknown_2C; // always zero?

        public GEO2SubModelData(Stream stream)
        {
            Transform1 = new Vector3() {
                X = stream.ReadSingle(),
                Y = stream.ReadSingle(),
                Z = stream.ReadSingle(),
            };

            TextureId = stream.ReadInt16();
            TextureSource = stream.ReadInt16();

            Transform2 = new Vector3() {
                X = stream.ReadSingle(),
                Y = stream.ReadSingle(),
                Z = stream.ReadSingle(),
            };

            Unknown_1C = stream.ReadInt32();

            DataSizeDiv = stream.ReadInt16();

            Type = (byte)stream.ReadByte();
            Flags = (byte)stream.ReadByte();

            DataOffset = stream.ReadInt32();

            Unknown_28 = stream.ReadInt32();
            Unknown_2C = stream.ReadInt32();
        }
    }

    public struct GEO2SubModelDataV2
    {
        public short TextureId;
        public short TextureSource;

        public int Unknown_04; // always zero?

        public short DataSizeDiv; // size / 10

        public byte Type; // same as GEO2.Type?
        public byte Flags;

        public int DataOffset;

        public int Unknown_10; // always zero?

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

        public Vector3 Transform1;

        public int Unknown_1C; // always zero?

        public Vector3 Transform2;

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

            Transform1 = new Vector3() {
                X = stream.ReadSingle(),
                Y = stream.ReadSingle(),
                Z = stream.ReadSingle(),
            };

            Unknown_1C = stream.ReadInt32();

            Transform2 = new Vector3() {
                X = stream.ReadSingle(),
                Y = stream.ReadSingle(),
                Z = stream.ReadSingle(),
            };

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
            X = new Vector4() {
                X = stream.ReadSingle(),
                Y = stream.ReadSingle(),
                Z = stream.ReadSingle(),
                W = stream.ReadSingle(),
            };

            Y = new Vector4() {
                X = stream.ReadSingle(),
                Y = stream.ReadSingle(),
                Z = stream.ReadSingle(),
                W = stream.ReadSingle(),
            };

            Z = new Vector4() {
                X = stream.ReadSingle(),
                Y = stream.ReadSingle(),
                Z = stream.ReadSingle(),
                W = stream.ReadSingle(),
            };
        }
    }

    public class LodEntry
    {
        // holds no actual data
        public bool IsDummy { get; set; }

        public int Flags { get; set; }
        public int Reserved { get; set; }

        public Vector4 Transform { get; set; }

        public List<SubModel> SubModels { get; set; }

    }

    public class SubModel
    {
        public bool HasTransform { get; set; }
        public bool HasVectorData { get; set; }

        public TransformAxis Transform { get; set; }

        // need to figure out the names
        public Vector3 V1 { get; set; }
        public Vector3 V2 { get; set; }

        // TODO: Link to actual texture data
        public int TextureId { get; set; }
        public int TextureSource { get; set; }

        public int Type { get; set; }
        public int Flags { get; set; }

        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }

        // TODO: Read actual model data!
        public byte[] ModelDataBuffer { get; set; }
    }
    
    public class ModelDefinition
    {
        public int Type { get; set; }

        public int UID { get; set; }
        public int Handle { get; set; }

        public Vector3 Transform1 { get; set; }
        public Vector3 Transform2 { get; set; }

        public List<LodEntry> Lods { get; set; }
        public List<SubModel> SubModels { get; set; }

        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }
        
        public void LoadBinary(Stream stream)
        {
            var baseOffset = stream.Position;

            var data = new GEO2ModelData(stream);

            Type = data.Type;

            Handle = data.Handle;
            UID = data.UID;

            Transform1 = data.Transform1;
            Transform2 = data.Transform2;

            Unknown1 = data.Unknown_2C;
            Unknown2 = data.Unknown_34;
            
            var lodsOffset = (int)(stream.Position - baseOffset);

            Lods = new List<LodEntry>(data.LodCount);
            SubModels = new List<SubModel>(data.SubModelCount);
            
            // process data
            for (int i = 0; i < data.LodCount; i++)
            {
                stream.Position = baseOffset + (lodsOffset + (i * 0x20));

                var _lod = new GEO2LodData(stream);
                
                var lod = new LodEntry() {
                    IsDummy = (_lod.LodInstanceCount == 0),

                    Flags = _lod.Unknown_12,
                    Reserved = _lod.Unknown_18,

                    Transform = _lod.Transform,
                };

                Lods.Add(lod);

                // nothing to see here, move along
                if (_lod.LodInstanceCount == 0)
                    continue;

                lod.SubModels = new List<SubModel>(_lod.LodInstanceCount);
                
                for (int l = 0; l < _lod.LodInstanceCount; l++)
                {
                    stream.Position = baseOffset + (_lod.LodInstanceDataOffset + (l * 0xC));

                    var _lodInstance = new GEO2LodInstanceData(stream);

                    if (_lodInstance.SubModelOffset == 0)
                        throw new InvalidOperationException("Invalid LOD instance!");

                    stream.Position = baseOffset + _lodInstance.SubModelOffset;

                    SubModel subModel = null;

                    var dataOffset = 0;
                    var dataLength = 0;

                    if ((Type & 0xF0) != 0)
                    {
                        var _subModel = new GEO2SubModelDataV2(stream);

                        if (_subModel.DataOffset == 0)
                            throw new InvalidOperationException("Invalid sub-model (V2)!");

                        subModel = new SubModel() {
                            HasVectorData = false,

                            TextureId = _subModel.TextureId,
                            TextureSource = _subModel.TextureSource,

                            Type = _subModel.Type,
                            Flags = _subModel.Flags,

                            Unknown1 = _subModel.Unknown_04,
                            Unknown2 = _subModel.Unknown_10,
                        };

                        dataOffset = _subModel.DataOffset;
                        dataLength = (_subModel.DataSizeDiv * 10);
                    }
                    else
                    {
                        var _subModel = new GEO2SubModelData(stream);

                        if (_subModel.DataOffset == 0)
                            throw new InvalidOperationException("Invalid sub-model!");

                        subModel = new SubModel() {
                            HasVectorData = true,

                            V1 = _subModel.Transform1,
                            V2 = _subModel.Transform2,

                            TextureId = _subModel.TextureId,
                            TextureSource = _subModel.TextureSource,

                            Type = _subModel.Type,
                            Flags = _subModel.Flags,

                            Unknown1 = _subModel.Unknown_1C,
                            Unknown2 = _subModel.Unknown_28,
                        };

                        dataOffset = _subModel.DataOffset;
                        dataLength = (_subModel.DataSizeDiv * 10);
                    }

                    // transform?
                    if (_lodInstance.TransformAxisOffset != 0)
                    {
                        stream.Position = (baseOffset + _lodInstance.TransformAxisOffset);

                        subModel.Transform = new TransformAxis(stream);
                        subModel.HasTransform = true;
                    }

                    stream.Position = (baseOffset + dataOffset);
                    
                    var buffer = new byte[dataLength];

                    stream.Read(buffer, 0, dataLength);

                    subModel.ModelDataBuffer = buffer;
                    
                    lod.SubModels.Add(subModel);
                    SubModels.Add(subModel);
                }
            }
        }
    }
}
