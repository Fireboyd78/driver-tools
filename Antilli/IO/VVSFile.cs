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
    public class VVSFile : ModelFile
    {
        public override void LoadModels()
        {
            ChunkBlock root = ChunkFile.Chunks[0];

            Models = new List<ModelsPackage>(root.Entries.Count / 2);

            for (int i = root.Entries.Count / 2; i < root.Entries.Count; i++)
                Models.Add(new ModelsPackage(ChunkFile.GetBlockData(root.Entries[i])));

            DSC.Log("Loaded {0} VVS models!", Models.Count);
        }

        // Call the default constructor
        public VVSFile(string filename) : base(filename) { }
    }
}
