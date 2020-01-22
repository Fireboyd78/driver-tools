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
    public interface IModelPackagesFile
    {
        List<ModelPackage> Packages { get; }
        
        bool Load(string filename);

        bool Save();
        bool Save(string filename, bool updateStream = false);
    }

    public class ModelFile : FileChunker, IModelPackagesFile
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
            switch ((ChunkType)sender.Context)
            {
            case ChunkType.ModelPackagePC:
            case ChunkType.ModelPackagePC_X:
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
