using System;
using System.Collections.Generic;
using System.IO;

namespace DSCript.Models
{
    public class ModelPS2
    {
        public int Type { get; set; }
        public bool IsVersion2 { get; set; }

        public int UID { get; set; }
        public int Handle { get; set; }

        public Vector3 BoxOffset { get; set; }
        public Vector3 BoxScale { get; set; }

        public List<LodPS2> Lods { get; set; }
        public List<LodInstancePS2> LodInstances { get; set; }
        public List<SubModelPS2> SubModels { get; set; }

        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }
        
        public void LoadBinary(Stream stream)
        {
            var baseOffset = stream.Position;

            var data = new ModelInfoPS2(stream);

            Type = (data.Type & 0x7F);
            IsVersion2 = (data.Type & 0x80) != 0;

            Handle = data.Handle;
            UID = data.UID;

            BoxOffset = data.BoxOffset;
            BoxScale = data.BoxScale;

            Unknown1 = data.Unknown_2C;
            Unknown2 = data.Unknown_34;
            
            var lodsOffset = (int)(stream.Position - baseOffset);

            Lods = new List<LodPS2>(data.LodCount);
            LodInstances = new List<LodInstancePS2>(data.LodInstanceCount);
            SubModels = new List<SubModelPS2>(data.SubModelCount);
            
            // process data
            for (int i = 0; i < data.LodCount; i++)
            {
                stream.Position = baseOffset + (lodsOffset + (i * 0x20));

                var _lod = new LodInfoPS2(stream);
                var lod = _lod.ToClass();

                Lods.Add(lod);
                
                // nothing to see here, move along
                if (lod.IsDummy)
                    continue;

                for (int l = 0; l < _lod.LodInstanceCount; l++)
                {
                    stream.Position = baseOffset + (_lod.LodInstanceDataOffset + (l * 0xC));

                    var _lodInstance = new LodInstanceInfoPS2(stream);

                    if (_lodInstance.SubModelOffset == 0)
                        throw new InvalidOperationException("Invalid LOD instance!");

                    var lodInstance = new LodInstancePS2();

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
                    
                    IClassDetail<SubModelPS2> subModelDetail = null;

                    var dataOffset = 0;
                    var dataLength = 0;

                    if (IsVersion2)
                    {
                        var _subModel = new SubModelInfoV2PS2(stream);

                        if (_subModel.DataOffset == 0)
                            throw new InvalidOperationException("Invalid sub-model (V2)!");
                        
                        dataOffset = _subModel.DataOffset;
                        dataLength = (_subModel.DataSizeDiv * 10);

                        subModelDetail = _subModel;
                    }
                    else
                    {
                        var _subModel = new SubModelInfoPS2(stream);

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
