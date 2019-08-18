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

        ModelPackage FindPackage(int uid);
        int FindMaterial(MaterialHandle material, out IMaterialData result);

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
                var modelPackage = SpoolableResourceFactory.Create<ModelPackage>(sender);
                modelPackage.ModelFile = this;

                Packages.Add(modelPackage);
                break;
            }

            base.OnSpoolerLoaded(sender, e);
        }
        
        protected override void OnFileSaveBegin()
        {
            if (HasModels)
            {
                foreach (var model in Packages)
                    model.CommitChanges();
            }

            base.OnFileSaveBegin();
        }

        public virtual ModelPackage FindPackage(int uid)
        {
            foreach (var pak in Packages)
            {
                if (pak.UID != uid)
                    continue;

                return pak;
            }

            // not found
            return null;
        }

        //
        // Return codes:
        //  1: OK
        //  0: not found
        //
        // -1: missing material
        // -2: default material
        // -3: null material
        // -4: local material
        //
        // -128: undefined result
        //
        public virtual int FindMaterial(MaterialHandle material, out IMaterialData result)
        {
            result = null;

            //
            // Null Material
            //
            if ((material == 0) || (material == 0xCCCCCCCC))
                return -3;

            switch (material.UID)
            {
            //
            // Null Material
            //
            case 0xCCCC:
                return -3;
            //
            // Local Material
            //
            case 0xFFFD:
                // material is local to its model package;
                // it's impossible for us to know where it came from
                return -4;
            //
            // Global Material
            //
            case 0xF00D:
            case 0xFFFC:
            case 0xFFFE:
                //
                // TODO:
                //  - Implement material package manager
                //  - Material packages are model packages with no models (e.g. menu files, overlays, etc)
                //  - Enumerate over the loaded material packages for the material
                //
                return -128;
            //
            // Default Material
            //
            case 0xFFFF:
                //
                // TODO:
                //  - Return a default material from material package manager
                //  - Handle = 0, UID = 0xFFFF
                //
                return -2;
            //
            // External Material
            //
            default:
                //
                // TODO:
                //  - Implement model package manager
                //  - Enumerate over the loaded model packages
                //  - Configure an upper limit? (Max 64 in Driv3r)
                //
                var pak = FindPackage(material.UID);

                if (pak != null)
                {
                    // model package found; try to get the desired material
                    if (pak.TryFindMaterial(material, out result))
                        return 1;   
                }

                foreach (var m in Packages)
                {
                    if (m.UID != material.UID)
                        continue;
                    
                    // material is missing from model package
                    return -1;
                }

                // failed to find the material
                return 0;
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
