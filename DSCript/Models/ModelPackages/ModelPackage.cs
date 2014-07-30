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
    public abstract class ModelPackage : SpoolableResource<SpoolableBuffer>
    {
        public int UID { get; set; }

        public Driv3rModelFile ModelFile { get; set; }

        public PackageType PackageType
        {
            get { return Enum.IsDefined((typeof(PackageType)), UID) ? (PackageType)UID : PackageType.SpooledModels; }
        }

        public List<PartsGroup> Parts { get; set; }
        public List<MeshGroup> MeshGroups { get; set; }
        public List<MeshDefinition> Meshes { get; set; }

        public List<VertexData> VertexBuffers { get; set; }
        public IndexData IndexBuffer { get; set; }

        public List<PCMPMaterial> Materials { get; set; }
        public List<PCMPSubMaterial> SubMaterials { get; set; }
        public List<PCMPTexture> Textures { get; set; }

        public bool HasMaterials
        {
            get { return Materials != null && Materials.Count > 0; }
        }

        public bool HasModels
        {
            get { return (Parts != null && Parts.Count > 0); }
        }
    }
}
