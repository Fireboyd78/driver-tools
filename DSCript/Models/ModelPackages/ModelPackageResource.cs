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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using DSCript.Spooling;

namespace DSCript.Models
{
    public enum PlatformType : int
    {
        PC      = 0,
        
        Console = 1,
            PS2     = (Console + 1),
            Xbox    = (Console + 2),
        
        Mobile  = 4,
            PSP     = (Mobile + 1),
        
        NextGen = 8,
            PS3     = (NextGen + 1),
            Xbox360 = (NextGen + 2),
            Wii     = (NextGen + 3),
        
        Any     = -1,
    }

    public interface IMaterialPackage : ISpoolableResource
    {
        int UID { get; }

        List<MaterialDataPC> Materials { get; }
        List<SubstanceDataPC> Substances { get; }
        List<TextureDataPC> Textures { get; }
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

        public int Flags { get; set; }

        public ModelFile ModelFile { get; set; }
        
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

        TDetail IDetailProvider.Deserialize<TDetail>(Stream stream)
        {
            return Deserialize<TDetail>(stream);
        }

        void IDetailProvider.Serialize<TDetail>(Stream stream, ref TDetail detail)
        {
            Serialize(stream, ref detail);
        }
        
        protected TDetail Deserialize<TDetail>(Stream stream)
            where TDetail : IDetail, new()
        {
            var result = new TDetail();
            result.Deserialize(stream, this);

            return result;
        }

        protected void Serialize<TDetail>(Stream stream, ref TDetail detail)
            where TDetail : IDetail
        {
            detail.Serialize(stream, this);
        }
        
        public List<Model> Models { get; set; }
        public List<LodInstance> LodInstances { get; set; }
        public List<SubModel> SubModels { get; set; }

        public List<VertexBuffer> VertexBuffers { get; set; }
        public IndexBuffer IndexBuffer { get; set; }

        public List<MaterialDataPC> Materials { get; set; }
        public List<SubstanceDataPC> Substances { get; set; }
        public List<TextureDataPC> Textures { get; set; }

        public virtual bool HasMaterials    => Materials?.Count > 0;
        public virtual bool HasModels       => Models?.Count > 0 && (VertexBuffers != null && IndexBuffer != null);
        
        public virtual int FindMaterial(MaterialHandle material, out IMaterialData result)
        {
            result = null;

            if ((material.UID == 0xFFFD) || (material.UID == UID))
            {
                if (HasMaterials && (material.Handle < Materials.Count))
                {
                    result = Materials[material.Handle];
                    return -4;
                }

                // missing
                return -1;
            }
            else
            {
                if (material.UID != UID)
                    return ModelFile.FindMaterial(material, out result);
            }

            // not found
            return 0;
        }
        
        public bool TryFindMaterial(MaterialHandle material, out IMaterialData result)
        {
            return (FindMaterial(material, out result) != 0);
        }

        public int FindMaterial<TMaterialData>(MaterialHandle material, out TMaterialData result)
            where TMaterialData : class, IMaterialData
        {
            IMaterialData _result = null;

            var type = FindMaterial(material, out _result);

            if ((type != 0) && (_result != null))
            {
                result = (_result as TMaterialData);

                if (result == null)
                    return 0;

                return type;
            }

            result = null;
            return type;
        }

        public bool TryFindMaterial<TMaterialData>(MaterialHandle material, out TMaterialData result)
            where TMaterialData : class, IMaterialData
        {
            return (FindMaterial(material, out result) != 0);
        }
    }
}
