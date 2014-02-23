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

namespace DSCript.Models
{
    public class VVVFile : ModelFile
    {
        public override void LoadModels()
        {
            ChunkBlock root = ChunkFile.Chunks[1];

            // Verify VVV format is being followed
            if (root.GetChunkType() != ChunkType.UnifiedPackage)
                return;

            ChunkEntry models = root.Entries.Last();

            if (models.GetChunkType() != ChunkType.ModelPackagePC)
                return;

            if (root.Entries.Count >= 2)
            {
                for (int i = 0; i < root.Entries.Count - 1; i++)
                {
                    if (root.Entries[i].GetChunkType() != ChunkType.VehicleHierarchy)
                        throw new Exception("Improperly formatted VVV file!");
                }
            }
            else
                throw new Exception("VVV file isn't well-formed!");

            // VVV files only have one model package defined
            Models = new List<ModelPackage>(1) {
                new ModelPackagePC(ChunkFile.GetBlockData(models)){
                            ModelFile = this
                        }
            };

            DSC.Log("VVV file loaded successfully!", Models.Count);
        }

        // Call the default constructor
        public VVVFile(string filename) : base(filename) { }
    }
}
