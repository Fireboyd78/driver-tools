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
        protected bool UsesSpoolSystem = false;
        protected bool IsMaterialPackage = false;

        public bool AllowPackageRegistry { get; set; } = true;

        public List<ModelPackage> Packages { get; }

        public virtual IMaterialPackage GlobalTextures { get; set; }

        public virtual bool HasModels
        {
            get { return (Packages.Count > 0); }
        }

        public virtual bool HasGlobals
        {
            get { return (GlobalTextures != null); }
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
            case ChunkType.StandaloneTextures:
                if (sender.Parent.Context == ChunkType.UnifiedPackage)
                {
                    GlobalTextures = sender.Parent.AsResource<GlobalTexturesResource>();

                    // defer loading it for now
                    IsMaterialPackage = true;
                }
                break;

            case ChunkType.ModelPackagePC_X:
                if (sender.Version == 3)
                    break; // DSF - unhandled for now
                
                goto case ChunkType.ModelPackagePC;

            case ChunkType.ModelPackagePC:
            case ChunkType.ModelPackagePS2:
            case ChunkType.ModelPackageXbox:
            case ChunkType.ModelPackageWii:
                var package = SpoolableResourceFactory.Create<ModelPackage>(sender);

                // not loaded
                package.UID = -1;

                // register global package
                if (sender.Parent.Context == ChunkType.SpoolSystemInitChunker)
                {
                    // make sure it's loaded!
                    if (AllowPackageRegistry)
                    {
                        PackageManager.Load(package);
                    }
                    else
                    {
                        SpoolableResourceFactory.Load(package);
                    }

                    GlobalTextures = package;
                    UsesSpoolSystem = true;
                }
                else
                {
                    Packages.Add(package);
                }
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

        protected override void OnFileLoadEnd()
        {
            // load the global textures now
            if (IsMaterialPackage)
            {
                if (AllowPackageRegistry)
                {
                    PackageManager.Load(GlobalTextures);
                }
                else
                {
                    SpoolableResourceFactory.Load(GlobalTextures);
                }
            }

            base.OnFileLoadEnd();

            // TODO: Move somewhere else
            //if (HasModels)
            //{
            //    var level = ModelPackage.LoadLevel;
            //    ModelPackage.LoadLevel = ModelPackageLoadLevel.FastLoad;
            //
            //    for (int i = 0; i < Packages.Count; i++)
            //    {
            //        var package = Packages[i];
            //
            //        if (package.UID == -1)
            //            SpoolableResourceFactory.Load(package);
            //
            //        package.DisplayName = $"{package.UID:X8} : {package.Handle:X4}";
            //    }
            //
            //    ModelPackage.LoadLevel = level;
            //}
        }

        public override void Dispose()
        {
            if (GlobalTextures != null)
            {
                // unregister our global textures
                if (AllowPackageRegistry)
                    PackageManager.UnRegister(GlobalTextures);

                GlobalTextures.Dispose();
            }
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
