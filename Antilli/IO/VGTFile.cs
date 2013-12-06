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

using DSCript;
using DSCript.IO;
using DSCript.Methods;

using Antilli.Models;

namespace Antilli.IO
{
    public class VGTFile : ModelFile
    {
        public List<PCMPMaterial> StandaloneTextures { get; set; }

        public void LoadStandaloneTextures()
        {
            ChunkEntry standaloneTextures = ChunkFile.Chunks[1].Entries[0];

            using (BlockEditor blockEditor = new BlockEditor(ChunkFile.GetBlockData(standaloneTextures)))
            using (BinaryReader f = new BinaryReader(blockEditor.Stream))
            {
                f.Seek(0x10, SeekOrigin.Begin);

                if (f.ReadUInt16() != (ushort)PackageType.VehicleGlobals)
                    throw new Exception("Bad vehicle globals identifier, cannot read vehicle globals!");

                int nTextures = f.ReadInt16();

                if (nTextures != Models[0].MaterialData.Materials.Count)
                    throw new Exception("Bad standalone texture count, cannot read vehicle globals!");

                StandaloneTextures = new List<PCMPMaterial>(nTextures);

                for (int s = 0; s < nTextures; s++)
                {
                    int matId = f.ReadInt16();

                    StandaloneTextures.Add(Models[0].MaterialData.Materials[matId]);

                    f.Seek(0x2, SeekOrigin.Current);
                }

                DSC.Log("Successfully read standalone textures list!");
            }
        }

        public override void LoadModels()
        {
            ChunkBlock root = ChunkFile.Chunks[1];
            
            // Verify VGT format is being followed
            if (root.GetChunkType() != ChunkType.UnifiedPackage)
                return;

            ChunkEntry models = root.Entries.Last();

            if (models.GetChunkType() != ChunkType.ModelPackagePC)
                return;

            if (root.Entries[0].GetChunkType() != ChunkType.StandaloneTextures || root.Entries[1].GetChunkType() != ChunkType.ModelPackagePC)
                throw new Exception("Improperly formatted VGT file!");

            // VGT files only have one model package defined
            Models = new List<ModelPackage>(1) {
                new ModelPackagePC(ChunkFile.GetBlockData(models))
            };

            LoadStandaloneTextures();
            
            DSC.Log("VGT file loaded successfully!", Models.Count);
        }

        // Call the default constructor
        public VGTFile(string filename) : base(filename) { }
    }
}
