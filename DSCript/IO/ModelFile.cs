using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using DSCript.Spooling;

namespace DSCript.Models
{
    public class StandaloneTextureData : SpoolableResource<SpoolablePackage>
    {
        public short UID { get; set; }

        public List<PCMPMaterial> StandaloneTextures { get; set; }

        protected override void Load()
        {
            var upst = Spooler.GetFirstChild(ChunkType.StandaloneTextures) as SpoolableBuffer;
            var mdpc = Spooler.GetFirstChild(ChunkType.ModelPackagePC) as SpoolableBuffer;

            if (upst == null || mdpc == null)
                return;

            var modelData = SpoolableResourceFactory.Create<ModelPackagePC>(mdpc, true);

            var materials = modelData.Materials;

            using (var f = upst.GetMemoryStream())
            {
                f.Position = 0x10;

                UID = f.ReadInt16();

                var count = f.ReadInt16();

                if (count != materials.Count)
                    throw new Exception("Failed to load StandaloneTextureData - texture count mismatch!");

                StandaloneTextures = new List<PCMPMaterial>(count);

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

        public PCMPMaterial GetStandaloneTexture(int id)
        {
            return (HasTextures) ? StandaloneTextureData.StandaloneTextures[id] : null;
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
            if (sender is SpoolablePackage && sender.Magic == 0x0)
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
            if ((ChunkType)sender.Magic == ChunkType.ModelPackagePC)
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
        public List<HierarchyData> Hierarchies { get; set; }

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

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            switch ((ChunkType)sender.Magic)
            {
            case ChunkType.VehicleHierarchy:
                Hierarchies.Add(sender.AsResource<HierarchyData>(true));
                break;
            }

            base.OnSpoolerLoaded(sender, e);
        }

        protected override void OnFileLoadBegin()
        {
            Hierarchies = new List<HierarchyData>();

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

    public class HierarchyData : SpoolableResource<SpoolableBuffer>
    {
        public class PartEntry
        {
            public short Type { get; set; }
            public short Unknown1 { get; set; }

            public short Unknown2 { get; set; }
            public short Unknown3 { get; set; }
        }

        public ModelPackage ModelPackage { get; set; }

        public int UID { get; set; }
        public int Reserved { get; set; }

        protected override void Load()
        {
            var awhf = this.Spooler;

            using (var f = awhf.GetMemoryStream())
            {
                f.Position = 0xC;

                if (f.ReadInt16() != 6)
                    throw new Exception("Cannot load hierarchy data - unsupported type");

                var nParts = f.ReadInt16();

                UID = f.ReadInt32();

                var colDataOffset = f.ReadInt32();

                Reserved = f.ReadInt32();

                var t1Count = f.ReadInt16();
                var t2Count = f.ReadInt16();
                var t3Count = f.ReadInt16();
                var t4Count = f.ReadInt16();

                f.Align(16);

                return;
            }
        }

        protected override void Save()
        {
            throw new NotImplementedException();
        }
    }

    
}
