using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using DSCript.Spooling;

namespace DSCript.Models
{
    public interface IModelFile : IFileChunker
    {
        List<ModelPackage> Packages { get; }

        bool HasModels { get; }
    }

    public class ModelFile : FileChunker, IModelFile
    {
        public List<ModelPackage> Packages { get; }
        
        public virtual bool HasModels
        {
            get { return (Packages.Count > 0); }
        }

        public override bool CanSave
        {
            get { return HasModels && base.CanSave; }
        }
        
        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            var type = (ChunkType)sender.Context;

            switch (type)
            {
            case ChunkType.ModelPackagePC_X:
                if (sender.Version == 3)
                    break; // DSF - unhandled for now
                
                goto case ChunkType.ModelPackagePC;

            case ChunkType.ModelPackagePC:
            case ChunkType.ModelPackagePS2:
            case ChunkType.ModelPackageXbox:
            case ChunkType.ModelPackageWii:
                var package = SpoolableResourceFactory.Create<ModelPackage>(sender);

                // register global package
                if (sender.Parent.Context == ChunkType.SpoolSystemInitChunker)
                    PackageManager.Load(package);
                
                Packages.Add(package);
                break;
            }

            base.OnSpoolerLoaded(sender, e);
        }
        
        protected override void OnFileSaveBegin()
        {
            foreach (var model in Packages)
                model.CommitChanges();

            base.OnFileSaveBegin();
        }
        
        public ModelFile()
        {
            Packages = new List<ModelPackage>();
        }

        public ModelFile(string filename) : this()
        {
            Load(filename);
        }
    }
}
