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
    public class GlobalTexturesResource : SpoolableResource<SpoolablePackage>, IMaterialPackage
    {
        public int UID { get; set; }

        public List<MaterialDataPC> Materials { get; set; }
        public List<SubstanceDataPC> Substances { get; set; }
        public List<PaletteData> Palettes { get; set; }
        public List<TextureDataPC> Textures { get; set; }

        public bool HasMaterials => Materials?.Count > 0;

        protected override void Load()
        {
            Materials = new List<MaterialDataPC>();
            Substances = new List<SubstanceDataPC>();
            Palettes = new List<PaletteData>();
            Textures = new List<TextureDataPC>();

            var upst = Spooler.GetFirstChild(ChunkType.StandaloneTextures) as SpoolableBuffer;
            var mdpc = Spooler.GetFirstChild(ChunkType.ModelPackagePC) as SpoolableBuffer;

            if (upst == null || mdpc == null)
                return;

            var pak = mdpc.AsResource<ModelPackage>(true);

            var materials = pak.Materials;

            using (var f = upst.GetMemoryStream())
            {
                // skip padding
                f.Position += 16;

                var data = f.ReadInt32();

                var uid = (data & 0xFFFF);
                var count = ((data >> 16) & 0xFFFF);

                if (count != materials.Count)
                    throw new InvalidOperationException("Failed to load global textures - material count mismatch!");

                UID = uid;
                Materials = new List<MaterialDataPC>(count);

                for (int i = 0; i < count; i++)
                {
                    var handle = (f.ReadInt32() & 0xFFFF);
                    
                    if (handle != i)
                        Debug.WriteLine($"Global material {i} remapped to {handle}.");

                    Materials.Add(materials[handle]);
                }

                Substances = pak.Substances;
                Palettes = pak.Palettes;
                Textures = pak.Textures;
            }
        }

        protected override void Save()
        {
            throw new NotImplementedException();
        }

        public int FindMaterial(MaterialHandle material, out IMaterialData result)
        {
            result = null;

            if (material.UID == UID)
            {
                if (HasMaterials && (material.Handle < Materials.Count))
                {
                    result = Materials[material.Handle];
                    return 1;
                }

                // missing
                return -1;
            }

            // not found
            return 0;
        }
    }

    public class GlobalTexturesFile : FileChunker
    {
        public GlobalTexturesResource GlobalTextures { get; set; }
        
        public override bool CanSave
        {
            get { return (HasTextures); }
        }

        public bool HasTextures
        {
            get { return (GlobalTextures != null && GlobalTextures.Materials.Count > 0); }
        }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            if (sender is SpoolablePackage && sender.Context == 0x0)
            {
                GlobalTextures = sender.AsResource<GlobalTexturesResource>();

                // register it
                PackageManager.Load(GlobalTextures);
            }

            base.OnSpoolerLoaded(sender, e);
        }

        public GlobalTexturesFile() { }
        public GlobalTexturesFile(string filename) : base(filename) { }
    }
}
