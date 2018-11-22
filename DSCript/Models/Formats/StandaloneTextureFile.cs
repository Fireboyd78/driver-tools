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
    public class StandaloneTextureData : SpoolableResource<SpoolablePackage>
    {
        public short UID { get; set; }

        public ModelPackage ModelPackage { get; set; }

        public List<MaterialDataPC> StandaloneTextures { get; set; }

        protected override void Load()
        {
            var upst = Spooler.GetFirstChild(ChunkType.StandaloneTextures) as SpoolableBuffer;
            var mdpc = Spooler.GetFirstChild(ChunkType.ModelPackagePC) as SpoolableBuffer;

            if (upst == null || mdpc == null)
                return;

            ModelPackage = SpoolableResourceFactory.Create<ModelPackage>(mdpc, true);

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

        public ModelPackage GetModelPackage()
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
}
