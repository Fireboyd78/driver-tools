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
                modelPackage.ModelFile = this;

                if ((ChunkType)sender.Parent.Context == ChunkType.SpoolSystemInitChunker)
                {
                    // make sure it's loaded!
                    modelPackage.GetInterface().Load();

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

                hierarchy.GetInterface().Load();

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

        public override int FindMaterial(MaterialHandle material, out IMaterialData result)
        {
            var globals = GlobalTextures;

            if (material.UID == globals.UID)
            {
                if (material.Handle < globals.Materials.Count)
                {
                    result = globals.Materials[material.Handle];
                    return 1;
                }

                // global material missing!
                result = null;
                return -1;
            }

            return base.FindMaterial(material, out result);
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

        public override ModelPackage FindPackage(int uid)
        {
            if (HasVirtualVehicles)
                return Packages[0];

            for (int i = 0; i < Hierarchies.Count; i++)
            {
                var hierarchy = Hierarchies[i];

                if (hierarchy.UID == uid)
                    return Packages[i];
            }

            // TODO: Implement global package manager
            return null;
        }
        
        public Driv3rVehiclesFile() { }
        public Driv3rVehiclesFile(string filename) : base(filename) { }
    }   
}
