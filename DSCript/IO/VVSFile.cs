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
    public class VVSFile : ModelFile
    {
        public override void LoadModels()
        {
            ChunkBlock root = ChunkFile.Chunks[0];

            // Check if PS2 version
            if (ChunkFile.Chunks[1].Entries.Count > 1)
            {
                // Load PS2 VVS file

            }

            Models = new List<ModelPackage>(root.Entries.Count / 2);

            for (int i = root.Entries.Count / 2; i < root.Entries.Count; i++)
                Models.Add(new ModelPackagePC(ChunkFile.GetBlockData(root.Entries[i])) {
                    ModelFile = this
                });

            DSC.Log("Loaded {0} VVS models!", Models.Count);
        }

        // Call the default constructor
        public VVSFile(string filename) : base(filename) { }
    }
}
