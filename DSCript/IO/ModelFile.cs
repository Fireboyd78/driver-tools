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

using System.Xml;
using System.Xml.Serialization;
using System.Windows.Media.Media3D;

using DSCript.Spooling;

namespace DSCript.Models
{
    public class StandaloneTextureData : SpoolableResource<SpoolablePackage>
    {
        public short UID { get; set; }

        public ModelPackagePC ModelPackage { get; set; }

        public List<MaterialData> StandaloneTextures { get; set; }

        protected override void Load()
        {
            var upst = Spooler.GetFirstChild(ChunkType.StandaloneTextures) as SpoolableBuffer;
            var mdpc = Spooler.GetFirstChild(ChunkType.ModelPackagePC) as SpoolableBuffer;

            if (upst == null || mdpc == null)
                return;

            ModelPackage = SpoolableResourceFactory.Create<ModelPackagePC>(mdpc, true);

            var materials = ModelPackage.Materials;

            using (var f = upst.GetMemoryStream())
            {
                f.Position = 0x10;

                UID = f.ReadInt16();

                var count = f.ReadInt16();

                if (count != materials.Count)
                    throw new Exception("Failed to load StandaloneTextureData - texture count mismatch!");

                StandaloneTextures = new List<MaterialData>(count);

                for (int i = 0; i < count; i++)
                {
                    var matId = f.ReadInt16();

                    StandaloneTextures.Add(materials[matId]);

                    f.Position += 0x2;
                }
            }

            DSC.Log("Successfully loaded StandaloneTextureData!");
        }

        protected override void Save()
        {
            throw new NotImplementedException();
        }
    }

    public class StandaloneTextureFile : FileChunker
    {
        public StandaloneTextureData StandaloneTextureData { get; set; }

        public MaterialData GetStandaloneTexture(int id)
        {
            return (HasTextures) ? StandaloneTextureData.StandaloneTextures[id] : null;
        }

        public ModelPackagePC GetModelPackage()
        {
            return (HasTextures) ? StandaloneTextureData.ModelPackage : null;
        }

        public override bool CanSave
        {
            get { return (HasTextures); }
        }

        public bool HasTextures
        {
            get { return (StandaloneTextureData != null && StandaloneTextureData.StandaloneTextures.Count > 0); }
        }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            if (sender is SpoolablePackage && sender.Context == 0x0)
                StandaloneTextureData = sender.AsResource<StandaloneTextureData>(true);

            base.OnSpoolerLoaded(sender, e);
        }

        public StandaloneTextureFile() { }
        public StandaloneTextureFile(string filename) : base(filename) { }
    }

    public class Driv3rModelFile : FileChunker
    {
        public List<ModelPackagePC> Models { get; set; }

        public ModelPackagePC GetModelPackage(int uid)
        {
            return (HasModels) ? Models.FirstOrDefault((m) => m.UID == uid) : null;
        }

        public bool HasModels
        {
            get { return (Models != null && Models.Count > 0); }
        }

        public override bool CanSave
        {
            get { return HasModels; }
        }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            if ((ChunkType)sender.Context == ChunkType.ModelPackagePC)
            {
                var mdpc = SpoolableResourceFactory.Create<ModelPackagePC>(sender);
                mdpc.ModelFile = this;
                
                Models.Add(mdpc);
            }

            base.OnSpoolerLoaded(sender, e);
        }

        protected override void OnFileLoadBegin()
        {
            Models = new List<ModelPackagePC>();

            base.OnFileLoadBegin();
        }

        protected override void OnFileLoadEnd()
        {
            if (Models.Count >= 1)
                DSC.Log("{0} model {1} loaded.", Models.Count, (Models.Count != 1) ? "packages" : "package");

            base.OnFileLoadEnd();
        }

        public Driv3rModelFile() { }
        public Driv3rModelFile(string filename) : base(filename) { }
    }

    public class Driv3rVehiclesFile : Driv3rModelFile
    {
        public StandaloneTextureFile VehicleGlobals { get; set; }
        public List<VehicleHierarchyData> Hierarchies { get; set; }

        public override bool CanSave
        {
            get { return (base.CanSave && (Hierarchies != null && Hierarchies.Count > 0)); }
        }

        public bool HasHierarchies
        {
            get { return (Hierarchies != null && Hierarchies.Count > 0); }
        }

        public bool HasVehicleGlobals
        {
            get { return (VehicleGlobals != null && VehicleGlobals.HasTextures); }
        }

        public bool HasIndividualModels
        {
            get { return (HasModels && HasHierarchies) ? (Models.Count == Hierarchies.Count) : false; }
        }

        /// <summary>
        /// Returns the vehicle container chunk for the specified vehicle id, if applicable.
        /// </summary>
        /// <param name="vehicleId">The vehicle id.</param>
        /// <returns>A vehicle container chunk corresponding to the vehicle id; if nothing is found, null.</returns>
        public SpoolablePackage GetVehicleContainerChunk(int vehicleId)
        {
            // vehicle container chunks are in the root chunk
            // if they're not there, then they don't exist
            var spooler = Content.Children.FirstOrDefault((s) => s.Context == vehicleId) as SpoolablePackage;
            return spooler;
        }

        /// <summary>
        /// Returns whether or not this is a VVV file.
        /// </summary>
        public bool IsMissionVehicleFile
        {
            get { return (Hierarchies.Count > Models.Count); }
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

        protected override void OnFileLoadEnd()
        {
            if (Models.Count == Hierarchies.Count)
                DSC.Log("Finished loading a VVS file!");

            base.OnFileLoadEnd();
        }

        public Driv3rVehiclesFile() { }
        public Driv3rVehiclesFile(string filename) : base(filename) { }
    }

    

    
}
