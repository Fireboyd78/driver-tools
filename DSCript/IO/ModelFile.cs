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
    public interface IModelFile
    {
        ChunkFile ChunkFile { get; set; }
        List<ModelPackage> Models { get; set; }

        IStandaloneModelFile SpooledFile { get; set; }

        bool HasSpooledFile { get; }

        void LoadModels();
    }

    public interface IStandaloneModelFile
    {
        ModelPackage ModelData { get; }

        List<PCMPMaterial> StandaloneTextures { get; set; }

        PCMPData MaterialData { get; }
    }

    public class ModelFile : IModelFile, IDisposable
    {
        public ChunkFile ChunkFile { get; set; }

        public List<ModelPackage> Models { get; set; }

        public IStandaloneModelFile SpooledFile { get; set; }

        public bool HasSpooledFile
        {
            get { return (SpooledFile != null); }
        }

        public virtual void Dispose()
        {
            if (ChunkFile != null)
                ChunkFile.Dispose();
            if (SpooledFile != null)
                SpooledFile = null;

            if (Models != null)
            {
                for (int i = 0; i < Models.Count; i++)
                    Models[i] = null;

                Models.Clear();
            }
        }

        public virtual void LoadModels()
        {
            Models = new List<ModelPackage>();

            for (int i = 0; i < ChunkFile.Chunks.Count; i++)
            {
                for (int k = 0; k < ChunkFile.Chunks[i].Entries.Count; k++)
                {
                    ChunkEntry entry = ChunkFile.Chunks[i].Entries[k];

                    switch (entry.GetChunkType())
                    {
                    case ChunkType.ModelPackagePC:
                        Models.Add(new ModelPackagePC(ChunkFile.GetBlockData(entry)) {
                            ModelFile = this
                        });
                        break;
                    default:
                        break;
                    }
                }
            }

            if (Models.Count >= 1)
                DSC.Log("{0} model {1} loaded.", Models.Count, (Models.Count != 1) ? "packages" : "package");
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

        }

        public ModelFile(string filename)
        {
            LoadFile(filename);
        }
    }
}
