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
    public interface IVehiclesFile : IModelPackagesFile
    {
        bool HasGlobals { get; }
        bool HasHierarchies { get; }
        bool HasVirtualVehicles { get; }

        IMaterialPackage GlobalTextures { get; }

        List<VehicleHierarchyData> Hierarchies { get; }

        VehicleHierarchyData FindHierarchy(int uid);
        
        //
        // TODO: Add support for vehicle handling data, sound data, etc.
        //
    }

    public class SpooledVehiclesFile : ModelFile, IVehiclesFile
    {
        public List<VehicleHierarchyData> Hierarchies { get; set; }

        public virtual IMaterialPackage GlobalTextures { get; set; }

        public virtual bool HasGlobals
        {
            get { return (GlobalTextures != null); }
        }

        public virtual bool HasHierarchies
        {
            get { return (Hierarchies?.Count > 0); }
        }
        
        public bool HasVirtualVehicles
        {
            get { return (Hierarchies?.Count > Packages?.Count); }
        }

        public override bool CanSave
        {
            get { return (HasModels || HasHierarchies); }
        }
        
        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            switch ((ChunkType)sender.Context)
            {
            case ChunkType.ModelPackagePC:
            case ChunkType.ModelPackagePC_X:
            case ChunkType.ModelPackageWii:
                var modelPackage = sender.AsResource<ModelPackage>();
                
                if (sender.Parent.Context == ChunkType.SpoolSystemInitChunker)
                {
                    // make sure it's loaded!
                    PackageManager.Load(modelPackage);

                    GlobalTextures = modelPackage;
                }
                else
                {
                    Packages.Add(modelPackage);
                }
                break;
            case ChunkType.VehicleHierarchy:
                var hierarchy = sender.AsResource<VehicleHierarchyData>();

                hierarchy.Platform = PlatformType.Any;
                hierarchy.Version = sender.Version;

                SpoolableResourceFactory.Load(hierarchy);

                Hierarchies.Add(hierarchy);
                break;
            }
        }

        protected override void OnFileLoadBegin()
        {
            Hierarchies = new List<VehicleHierarchyData>();

            base.OnFileLoadBegin();
        }
        
        public virtual VehicleHierarchyData FindHierarchy(int uid)
        {
            foreach (var hierarchy in Hierarchies)
            {
                if (hierarchy.UID == uid)
                    return hierarchy;
            }

            return null;
        }
        
        public SpooledVehiclesFile() { }
        public SpooledVehiclesFile(string filename) : base(filename) { }
    }
    
    public class Driv3rVehiclesFile : SpooledVehiclesFile
    {
        public GlobalTexturesFile VehicleGlobals { get; set; }
        
        public override bool HasGlobals
        {
            get { return (VehicleGlobals != null && VehicleGlobals.HasTextures); }
        }

        public override IMaterialPackage GlobalTextures
        {
            get { return (HasGlobals) ? VehicleGlobals.GlobalTextures : null; }
            set
            {
                throw new InvalidOperationException("Can't set global textures for this type of vehicles file!");
            }
        }
        
        public Driv3rVehiclesFile() { }
        public Driv3rVehiclesFile(string filename) : base(filename) { }
    }   
}
