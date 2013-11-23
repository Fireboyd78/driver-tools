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
    public interface IModelFile
    {
        ChunkFile ChunkFile { get; set; }
        List<ModelsPackage> Models { get; set; }

        void LoadModels();
    }

    public class ModelFile : IModelFile, IDisposable
    {
        public ChunkFile ChunkFile { get; set; }

        public List<ModelsPackage> Models { get; set; }

        public virtual void Dispose()
        {
            if (ChunkFile != null)
                ChunkFile.Dispose();

            if (Models != null)
            {
                for (int i = 0; i < Models.Count; i++)
                    Models[i] = null;

                Models.Clear();
            }
        }

        public virtual void LoadModels()
        {
            Models = new List<ModelsPackage>();

            for (int i = 0; i < ChunkFile.Chunks.Count; i++)
            {
                for (int k = 0; k < ChunkFile.Chunks[i].Entries.Count; k++)
                {
                    if (Chunk.CheckType(ChunkFile.Chunks[i].Entries[k].Magic, ChunkType.ModelPackagePC))
                        Models.Add(new ModelsPackage(ChunkFile.GetBlockData(ChunkFile.Chunks[i].Entries[k])));
                }
            }

            if (Models.Count >= 1)
                DSC.Log("{0} models loaded.", Models.Count);
            else                
                Models = null;
        }

        protected virtual void LoadFile(string filename)
        {
            ChunkFile = new ChunkFile(filename);
            LoadModels();
        }

        protected ModelFile()
        {
            // This allows inheritance and disallows blank constructors.
        }

        public ModelFile(string filename)
        {
            LoadFile(filename);
        }
    }
}
