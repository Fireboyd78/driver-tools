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

namespace DSCript.Models
{
    public abstract class ModelPackage
    {
        public IModelFile ModelFile { get; set; }

        public uint UID { get; protected set; }

        public PackageType PackageType
        {
            get { return Enum.IsDefined((typeof(PackageType)), UID) ? (PackageType)UID : PackageType.SpooledModels; }
        }

        public virtual uint Magic
        {
            get { return 0xFF; }
        }

        public BlockData BlockData { get; set; }

        public List<PartsGroup> Parts { get; set; }
        public List<MeshGroup> MeshGroups { get; set; }
        public List<IndexedMesh> Meshes { get; set; }

        public VertexData Vertices { get; set; }
        public IndexData Indices { get; set; }

        public PCMPData MaterialData { get; set; }

        public bool HasMaterials
        {
            get { return MaterialData != null; }
        }

        public bool HasBlendWeights
        {
            get { return (Vertices != null) ? Vertices.VertexType != FVFType.Vertex12 : false; }
        }

        public virtual void Load()
        {
            throw new NotImplementedException();
        }

        public virtual void Compile()
        {
            throw new NotImplementedException();
        }

        public ModelPackage(BlockData blockData)
        {
            BlockData = blockData;
            Load();
        }
    }
}
