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

        public List<MaterialDataPC> StandaloneTextures { get; set; }

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

                StandaloneTextures = new List<MaterialDataPC>(count);

                for (int i = 0; i < count; i++)
                {
                    var matId = f.ReadInt16();

                    StandaloneTextures.Add(materials[matId]);

                    f.Position += 0x2;
                }
            }
        }

        protected override void Save()
        {
            throw new NotImplementedException();
        }
    }

    public class StandaloneTextureFile : FileChunker
    {
        public StandaloneTextureData StandaloneTextureData { get; set; }

        public MaterialDataPC GetStandaloneTexture(int id)
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
            get { return HasModels && base.CanSave; }
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

        protected override void OnFileSaveBegin()
        {
            if (HasModels)
            {
                foreach (var model in Models)
                    model.CommitChanges();
            }

            base.OnFileSaveBegin();
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

        public ModelPackagePC GetModelContainer(VehicleHierarchyData hierarchy)
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

        public List<PartsGroup> GetVehicleParts(VehicleHierarchyData hierarchy)
        {
            var parts = new List<PartsGroup>();
            var mpak = GetModelContainer(hierarchy);

            foreach (var part in hierarchy.Parts)
            {
                if (part.ModelId == 255)
                    continue;
                
                var model = mpak.Parts[part.ModelId];

                parts.Add(model);
            }

            return parts;
        }
        
        public Driv3rVehiclesFile() { }
        public Driv3rVehiclesFile(string filename) : base(filename) { }
    }   
}
