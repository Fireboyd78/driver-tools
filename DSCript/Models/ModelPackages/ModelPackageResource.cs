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
    public enum PlatformType
    {
        PS2     = 0,
        Xbox    = 1,
        PC      = 2,
    }
    
    public abstract class ModelPackageResource : SpoolableResource<SpoolableBuffer>
    {
        protected const int MGX_ModelPackagePS2     = 0x32434D47; // 'GMC2'
        protected const int MGX_ModelPackageXBox    = 0x4258444D; // 'MDXB'
        protected const int MGX_ModelPackagePC      = 0x4350444D; // 'MDPC'
        protected const int MGX_ModelPackageXN      = 0x4E58444D; // 'MDXN'

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

        public ModelFile ModelFile { get; set; }

        public virtual PackageType PackageType
        {
            get { return Enum.IsDefined((typeof(PackageType)), UID) ? (PackageType)UID : PackageType.SpooledModels; }
        }

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
        
        public List<Model> Models { get; set; }
        public List<LodInstance> LodInstances { get; set; }
        public List<SubModel> SubModels { get; set; }

        public List<VertexBuffer> VertexBuffers { get; set; }
        public IndexData IndexBuffer { get; set; }

        public List<MaterialDataPC> Materials { get; set; }
        public List<SubstanceDataPC> Substances { get; set; }
        public List<TextureDataPC> Textures { get; set; }

        public virtual bool HasMaterials    => Materials?.Count > 0;
        public virtual bool HasModels       => Models?.Count > 0 && (VertexBuffers != null && IndexBuffer != null);
    }
}
