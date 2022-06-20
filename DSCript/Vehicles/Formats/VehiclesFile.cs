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
    public interface IVehiclesFile : IModelFile
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
            case ChunkType.VehicleHierarchy:
                var hierarchy = sender.AsResource<VehicleHierarchyData>();
                
                hierarchy.Platform = PlatformType.Generic;

                // DPL on XBox is an annoying little bugger!
                if (UsesSpoolSystem && (hierarchy.Version == 0))
                    hierarchy.Platform = PlatformType.Xbox;
                
                SpoolableResourceFactory.Load(hierarchy);
                
                Hierarchies.Add(hierarchy);
                break;
            case ChunkType.SpooledVehicleChunk:
                if (sender.Version != 0)
                    break;

                var parent = sender.Parent;
                var vehc = sender as SpoolablePackage;

                // assume SSIC chunk comes first since we don't load it..
                var index = parent.Children.IndexOf(vehc) - 1;

                var hier = Hierarchies[index];
                var pckg = Packages[index];

                var uid = hier.UID;

                var vehId = uid & 0xFF;
                var modLvl = (uid & 0x7000) / 0x1000;

                pckg.DisplayName = $"[{DriverPL.VehicleNames[vehId] + ((modLvl > 0) ? String.Format(" (Bodykit #{0})", modLvl) : "")}]";
                break;
            }

            base.OnSpoolerLoaded(sender, e);
        }

        protected override void OnFileLoadBegin()
        {
            Hierarchies = new List<VehicleHierarchyData>();

            base.OnFileLoadBegin();
        }

        protected override void OnFileLoadEnd()
        {
            base.OnFileLoadEnd();

            // TODO: Move somewhere else
            //if (UsesSpoolSystem)
            //{
            //    if (HasModels && HasHierarchies)
            //    {
            //        for (int i = 0; i < Packages.Count; i++)
            //        {
            //            var package = Packages[i];
            //            var hierarchy = Hierarchies[i];
            //
            //            var uid = hierarchy.UID;
            //
            //            var vehId = uid & 0xFF;
            //            var modLvl = (uid & 0x7000) / 0x1000;
            //
            //            package.DisplayName = $"[{DriverPL.VehicleNames[vehId]}]";
            //        }
            //    }
            //}
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

        protected override void OnFileLoadEnd()
        {
            base.OnFileLoadEnd();

            // TODO: Move somewhere else
            if (HasModels && HasHierarchies && !HasVirtualVehicles)
            {
                for (int i = 0; i < Packages.Count; i++)
                {
                    var package = Packages[i];
                    var hierarchy = Hierarchies[i];
            
                    var uid = hierarchy.UID;
            
                    package.DisplayName = $"[{Driv3r.GetVehicleTypeName(uid)}]";
                }
            }
        }

        public Driv3rVehiclesFile() { }
        public Driv3rVehiclesFile(string filename) : base(filename) { }
    }   
}
