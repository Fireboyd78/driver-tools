using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using DSCript.Spooling;

namespace DSCript.Models
{
    public interface IMaterialPackage : ISpoolableResource
    {
        int UID { get; }

        bool HasMaterials { get; }

        List<MaterialDataPC> Materials { get; }
        List<SubstanceDataPC> Substances { get; }
        List<PaletteData> Palettes { get; }
        List<TextureDataPC> Textures { get; }

        int FindMaterial(MaterialHandle material, out IMaterialData result);
    }

    public abstract class ModelPackageResource : SpoolableResource<SpoolableBuffer>, IDetailProvider, IMaterialPackage
    {
        protected const int MGX_ModelPackagePS2     = 0x32434D47; // 'GMC2'
        protected const int MGX_ModelPackageXBox    = 0x4258444D; // 'MDXB'
        protected const int MGX_ModelPackagePC      = 0x4350444D; // 'MDPC'
        protected const int MGX_ModelPackageXN      = 0x4E58444D; // 'MDXN'
        protected const int MGX_ModelPackageWii     = 0x4957444D; // 'MDWI'

        public static int GetChunkId(PlatformType platform, int version)
        {
            switch (platform)
            {
            case PlatformType.PS2:
                return MGX_ModelPackagePS2;
            case PlatformType.Xbox:
                return MGX_ModelPackageXBox;
            case PlatformType.PC:
                switch (version)
                {
                case 1: return MGX_ModelPackageXN;
                case 6: return MGX_ModelPackagePC;
                }
                break;
            }
            
            return 0x21505453;
        }
        
        public PlatformType Platform { get; set; }
        
        public int Version { get; set; }
        public int UID { get; set; }

        public int Handle { get; set; }

        public int Flags { get; set; }

        public virtual MaterialPackageType MaterialPackageType
        {
            get
            {
                switch (Platform)
                {
                case PlatformType.PC:   return MaterialPackageType.PC;
                case PlatformType.Xbox: return MaterialPackageType.Xbox;
                case PlatformType.PS2:  return MaterialPackageType.PS2;
                }

                return MaterialPackageType.Unknown;
            }
        }

        int IDetailProvider.Version
        {
            get { return Version; }
        }

        int IDetailProvider.Flags
        {
            get
            {
                if ((Flags == 0) && (Version == 9))
                    return 0xBADC0DE;

                return Flags;
            }
            set { Flags = value; }
        }
        
        public List<Model> Models { get; set; }
        public List<LodInstance> LodInstances { get; set; }
        public List<SubModel> SubModels { get; set; }

        public List<VertexBuffer> VertexBuffers { get; set; }
        public IndexBuffer IndexBuffer { get; set; }

        public List<MaterialDataPC> Materials { get; set; }
        public List<SubstanceDataPC> Substances { get; set; }
        public List<PaletteData> Palettes { get; set; }
        public List<TextureDataPC> Textures { get; set; }

        public virtual bool HasMaterials    => Materials?.Count > 0;
        public virtual bool HasModels       => Models?.Count > 0 && (VertexBuffers != null && IndexBuffer != null);

        public virtual void FreeModels()
        {
            if (!AreChangesPending)
            {
                foreach (var model in Models)
                {
                    model.VertexBuffer = null;

                    foreach (var lod in model.Lods)
                    {
                        lod.Parent = null;

                        foreach (var lodInst in lod.Instances)
                        {
                            foreach (var subModel in lodInst.SubModels)
                                subModel.ModelPackage = null;

                            lodInst.SubModels.Clear();
                            lodInst.SubModels = null;
                        }

                        lod.Instances.Clear();
                        lod.Instances = null;
                    }
                    
                    model.Lods.Clear();
                    model.Lods = null;
                }

                Models.Clear();
                LodInstances.Clear();
                SubModels.Clear();

                foreach (var vBuffer in VertexBuffers)
                {
                    if (vBuffer.Vertices != null)
                    {
                        vBuffer.Vertices.Clear();
                        vBuffer.Vertices = null;
                    }
                }

                IndexBuffer.Indices = null;
            }
        }

        public virtual void FreeMaterials()
        {
            if (!AreChangesPending)
            {
                foreach (var material in Materials)
                {
                    material.Substances.Clear();
                    material.Substances = null;
                }

                foreach (var substance in Substances)
                {
                    substance.Palettes.Clear();
                    substance.Palettes = null;

                    substance.Textures.Clear();
                    substance.Textures = null;
                }

                foreach (var palette in Palettes)
                    palette.Data = null;

                foreach (var texture in Textures)
                    texture.Buffer = null;

                Materials.Clear();
                Substances.Clear();
                Palettes.Clear();
                Textures.Clear();
            }
        }
        
        public virtual int FindMaterial(MaterialHandle material, out IMaterialData result)
        {
            result = null;
            
            if ((material.UID == 0xFFFD) || (material.UID == UID))
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
        
        public int FindMaterial<TMaterialData>(MaterialHandle material, out TMaterialData result)
            where TMaterialData : class, IMaterialData
        {
            IMaterialData mtl = null;

            var type = FindMaterial(material, out mtl);

            result = (mtl as TMaterialData);
            return type;
        }
    }
}
