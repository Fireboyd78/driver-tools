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
    public class Driv3rVehiclesFile : ModelFile
    {
        public StandaloneTextureFile VehicleGlobals { get; set; }
        public List<VehicleHierarchyData> Hierarchies { get; set; }

        public override bool CanSave
        {
            get { return (HasModels || HasHierarchies); }
        }

        public bool HasHierarchies
        {
            get { return (Hierarchies?.Count > 0); }
        }

        public bool HasVehicleGlobals
        {
            get { return (VehicleGlobals != null && VehicleGlobals.HasTextures); }
        }

        public bool HasIndividualModels
        {
            get { return (Models?.Count == Hierarchies?.Count); }
        }
        
        /// <summary>
        /// Returns whether or not this is a VVV file.
        /// </summary>
        public bool IsMissionVehicleFile
        {
            get { return (Hierarchies?.Count > Models?.Count); }
        }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            switch ((ChunkType)sender.Context)
            {
            case ChunkType.VehicleHierarchy:
                Hierarchies.Add(sender.AsResource<VehicleHierarchyData>(true));
                break;
            }

            base.OnSpoolerLoaded(sender, e);
        }

        protected override void OnFileLoadBegin()
        {
            Hierarchies = new List<VehicleHierarchyData>();

            base.OnFileLoadBegin();
        }

        public ModelPackage GetModelContainer(VehicleHierarchyData hierarchy)
        {
            if (HasIndividualModels)
            {
                var idx = Hierarchies.IndexOf(hierarchy);
                return Models[idx];
            }
            else
            {
                return Models[0];
            }
        }

        public List<Model> GetVehicleParts(VehicleHierarchyData hierarchy)
        {
            var parts = new List<Model>();
            var mpak = GetModelContainer(hierarchy);

            foreach (var part in hierarchy.Parts)
            {
                if (part.ModelId == 255)
                    continue;
                
                var model = mpak.Models[part.ModelId];

                parts.Add(model);
            }

            return parts;
        }
        
        public Driv3rVehiclesFile() { }
        public Driv3rVehiclesFile(string filename) : base(filename) { }
    }   
}
