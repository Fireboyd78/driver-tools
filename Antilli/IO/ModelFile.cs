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
using Antilli.Models;

namespace Antilli.IO
{
    public class ModelFile
    {
        public ChunkFile File { get; set; }

        public List<ModelsPackage> Models { get; set; }

        public virtual void LoadModels()
        {
            Models = new List<ModelsPackage>();

            for (int i = 0; i < File.Chunks.Count; i++)
            {
                for (int k = 0; k < File.Chunks[i].Entries.Count; k++)
                {
                    if (Chunk.CheckType(File.Chunks[i].Entries[k].Magic, ChunkType.ModelPackagePC))
                        Models.Add(new ModelsPackage(File.GetBlockData(File.Chunks[i].Entries[k])));
                }
            }

            DSC.Log("{0} models loaded.", Models.Count);
        }

        protected ModelFile()
        {   
        }

        public ModelFile(string filename)
        {
            File = new ChunkFile(filename);
            LoadModels();
        }
    }
}
