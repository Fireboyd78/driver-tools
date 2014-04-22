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

        StandaloneModelFile SpooledFile { get; set; }

        bool HasSpooledFile { get; }

        void LoadModels();
    }

    public abstract class StandaloneModelFile : ModelFile
    {
        public abstract ModelPackage ModelData { get; }

        public abstract List<PCMPMaterial> StandaloneTextures { get; set; }

        public abstract PCMPData MaterialData { get; }

        public string Name { get; set; }

        public StandaloneModelFile(string filename) : base(filename)
        {
        }
    }

    public class ModelFile : IModelFile, IDisposable
    {
        public ChunkFile ChunkFile { get; set; }

        public List<ModelPackage> Models { get; set; }

        StandaloneModelFile IModelFile.SpooledFile { get; set; }

        bool IModelFile.HasSpooledFile
        {
            get { return (@this.SpooledFile != null); }
        }

        protected IModelFile @this
        {
            get { return ((IModelFile)this); }
        }

        public virtual void Dispose()
        {
            if (ChunkFile != null)
                ChunkFile.Dispose();
            if (@this.SpooledFile != null)
                @this.SpooledFile = null;

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
