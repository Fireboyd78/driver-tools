using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using DSCript.Models;

namespace GMC2Snooper
{
    public struct ModelPackagePS2Header
    {
        public static readonly int Magic = 0x4B41504D; // 'MPAK'

        public int UID;

        public int Reserved1;
        public int Reserved2;
        public int Reserved3;

        public int ModelCount;

        public int MaterialDataOffset;
        public int DataSize;

        // does not include model offset list
        public int HeaderSize
        {
            get { return 0x20; }
        }

        public void ReadHeader(Stream stream)
        {
            if (stream.ReadInt32() != Magic)
                throw new Exception("Bad magic, cannot load ModelPackagePS2!");

            UID = stream.ReadInt32();

            Reserved1 = stream.ReadInt32();
            Reserved2 = stream.ReadInt32();
            Reserved3 = stream.ReadInt32();

            ModelCount = stream.ReadInt32();

            MaterialDataOffset = stream.ReadInt32();
            DataSize = stream.ReadInt32();
        }

        public void WriteHeader(Stream stream)
        {
            stream.Write(Magic);

            stream.Write(UID);

            stream.Write(Reserved1);
            stream.Write(Reserved2);
            stream.Write(Reserved3);

            stream.Write(ModelCount);

            stream.Write(MaterialDataOffset);
            stream.Write(DataSize);
        }
    }

    public class ModelPackagePS2
    {
        public int UID { get; set; }
        
        public List<ModelDefinition> Models { get; set; }
        
        public List<MaterialDataPS2> Materials { get; set; }
        public List<SubstanceDataPS2> Substances { get; set; }
        public List<TextureDataPS2> Textures { get; set; }

        public byte[] TextureDataBuffer { get; set; }

        public void LoadBinary(Stream stream)
        {
            var baseOffset = stream.Position;

            // read header
            var data = new ModelPackagePS2Header();
            data.ReadHeader(stream);

            // read models
            var modelOffsets = new int[data.ModelCount];

            for (int g = 0; g < data.ModelCount; g++)
                modelOffsets[g] = stream.ReadInt32();

            Models = new List<ModelDefinition>(data.ModelCount);

            for (int g = 0; g < data.ModelCount; g++)
            {
                var modelOffset = modelOffsets[g];

                // null model?
                if (modelOffset == 0)
                    continue;

                stream.Position = (baseOffset + modelOffsets[g]);

                var model = new ModelDefinition();

                model.LoadBinary(stream);

                Models.Add(model);
            }

            // read material data
            var tsc2Offset = (baseOffset + data.MaterialDataOffset);

            // make sure it actually has textures
            if (tsc2Offset == stream.Length)
                return;
            
            stream.Position = tsc2Offset;

            var tsc2Header = new MaterialPackageHeader(MaterialPackageType.PS2, stream);

            Materials = new List<MaterialDataPS2>(tsc2Header.MaterialsCount);

            for (int m = 0; m < Materials.Capacity; m++)
            {
                stream.Position = tsc2Offset + (tsc2Header.MaterialsOffset + (m * tsc2Header.MaterialSize));
                
                var srCount = stream.ReadByte();
                var frameCount = stream.ReadByte();

                stream.Position += 2;
                
                var mAnimSpeed = stream.ReadFloat();
                var mAnimToggle = (stream.ReadInt32() == 1);

                var srOffset = stream.ReadInt32() + tsc2Offset;

                var material = new MaterialDataPS2() {
                    Animated = mAnimToggle,
                    AnimationSpeed = mAnimSpeed
                };

                Materials.Add(material);

                stream.Position = srOffset;
                var sOffset = stream.ReadInt32() + tsc2Offset;

                // get substance(s)
                for (int s = 0; s < srCount; s++)
                {                    
                    stream.Position = sOffset + (s * 0xC);

                    var s1 = stream.ReadByte();
                    var s2 = stream.ReadByte();
                    var tCount = stream.ReadByte();
                    var s4 = stream.ReadByte();

                    var substance = new SubstanceDataPS2() {
                        Mode = s1,
                        Flags = s2,
                        Type = s4,
                    };

                    material.Substances.Add(substance);
                    Substances.Add(substance);
                    
                    // reserved
                    stream.Position += 0x4;

                    var tOffset = stream.ReadInt32() + tsc2Offset;

                    for (int t = 0; t < tCount; t++)
                    {
                        stream.Position = tOffset + (t * tsc2Header.LookupSize);

                        var texOffset = stream.ReadInt32() + tsc2Offset;

                        stream.Position = texOffset;

                        var texInfo = new TextureDataPS2() {
                            Reserved = stream.ReadInt64(),

                            Modes = stream.ReadByte(),
                            Type = stream.ReadByte(),

                            MipMaps = stream.ReadByte(),
                            Flags = stream.ReadByte(),

                            Width = stream.ReadInt16(),
                            Height = stream.ReadInt16(),

                            Unknown1 = stream.ReadInt32(),
                            
                            DataOffset = stream.ReadInt32(),

                            Unknown2 = stream.ReadInt32(),
                        };
                        
                        for (int c = 0; c < texInfo.Modes; c++)
                        {
                            var clutOffset = stream.ReadInt32();

                            texInfo.CLUTs.Add(clutOffset);
                        }

                        substance.Textures.Add(texInfo);
                        Textures.Add(texInfo);
                    }
                }
            }

            Debug.WriteLine($"TSC2 header size: 0x{stream.Position - tsc2Offset:X8}");

            // resolve the texture buffer and all offsets
            var texBufOffset = 0;
            var texBufLength = 0;
            
            texBufOffset = (int)Memory.Align(stream.Position, 16);
            texBufLength = (int)(stream.Length - texBufOffset);

            // now resolve each texture's offset relative to the buffer, instead of the header
            foreach (var texInfo in Textures)
            {
                texInfo.DataOffset = (int)((tsc2Offset + texInfo.DataOffset) - texBufOffset);

                // resolve CLUT offsets as well
                for (int c = 0; c < texInfo.CLUTs.Count; c++)
                {
                    var clut = texInfo.CLUTs[c];
                    texInfo.CLUTs[c] = (int)((tsc2Offset + clut) - texBufOffset);
                }
            }
            
            Debug.WriteLine($"Reading texture buffer @ {texBufOffset:X8} (size:{texBufLength:X8})");

            // initialize the buffer
            TextureDataBuffer = new byte[texBufLength];

            stream.Position = texBufOffset;

            // finally, fill in the buffer!
            stream.Read(TextureDataBuffer, 0, texBufLength);
        }

        public ModelPackagePS2()
        {
            Models = new List<ModelDefinition>();

            Materials = new List<MaterialDataPS2>();
            Substances = new List<SubstanceDataPS2>();
            Textures = new List<TextureDataPS2>();
        }
    }
}
