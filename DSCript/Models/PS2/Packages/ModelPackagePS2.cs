using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DSCript.Models
{
    public class ModelPackagePS2
    {
        public int UID { get; set; }
        
        public List<ModelPS2> Models { get; set; }
        
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

            Models = new List<ModelPS2>(data.ModelCount);

            for (int g = 0; g < data.ModelCount; g++)
            {
                var modelOffset = modelOffsets[g];

                // null model?
                if (modelOffset == 0)
                    continue;

                stream.Position = (baseOffset + modelOffsets[g]);

                var model = new ModelPS2();

                model.LoadBinary(stream);

                Models.Add(model);
            }

            // read material data
            var tsc2Offset = (int)(baseOffset + data.MaterialDataOffset);

            // make sure it actually has textures
            if (tsc2Offset == stream.Length)
                return;
            
            stream.Position = tsc2Offset;

            var tsc2Header = new MaterialPackageData(MaterialPackageType.PS2, stream);

            Materials = new List<MaterialDataPS2>(tsc2Header.MaterialsCount);

            // get materials
            for (int m = 0; m < Materials.Capacity; m++)
            {
                stream.Position = tsc2Offset + (tsc2Header.MaterialsOffset + (m * tsc2Header.MaterialSize));
                
                var _m = stream.Read<MaterialDataPS2.Detail>();
                _m.SubstanceRefsOffset += tsc2Offset;

                var material = _m.ToClass();

                Materials.Add(material);
                
                // get substances
                for (int s = 0; s < _m.NumSubstances; s++)
                {
                    stream.Position = _m.SubstanceRefsOffset + (s * tsc2Header.LookupSize);
                    stream.Position = stream.ReadInt32() + tsc2Offset;

                    var _s = stream.Read<SubstanceDataPS2.Detail>();
                    _s.TextureRefsOffset += tsc2Offset;

                    var substance = _s.ToClass();
                    
                    Substances.Add(substance);
                    material.Substances.Add(substance);
                    
                    // get textures
                    for (int t = 0; t < _s.NumTextures; t++)
                    {
                        stream.Position = _s.TextureRefsOffset + (t * tsc2Header.LookupSize);
                        stream.Position = stream.ReadInt32() + tsc2Offset;
                        
                        var _t = stream.Read<TextureDataPS2.Detail>();
                        _t.DataOffset += tsc2Offset;

                        var texture = _t.ToClass();

                        // read cluts
                        texture.CLUTs = new List<int>(_t.Pixmaps);

                        for (int c = 0; c < _t.Pixmaps; c++)
                        {
                            var offset = stream.ReadInt32() + tsc2Offset;
                            texture.CLUTs.Add(offset);
                        }

                        Textures.Add(texture);
                        substance.Textures.Add(texture);
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
                texInfo.DataOffset -= texBufOffset;

                // resolve CLUT offsets as well
                for (int c = 0; c < texInfo.CLUTs.Count; c++)
                    texInfo.CLUTs[c] -= texBufOffset;
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
            Models = new List<ModelPS2>();

            Materials = new List<MaterialDataPS2>();
            Substances = new List<SubstanceDataPS2>();
            Textures = new List<TextureDataPS2>();
        }
    }
}
