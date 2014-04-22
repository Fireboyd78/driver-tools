using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;

using HelixToolkit.Wpf;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    public class DriverModelVisual3D : BlendModelVisual3D
    {
        public static readonly DependencyProperty ModelFileProperty;
        public static readonly DependencyProperty ModelPackageProperty;
        public static readonly DependencyProperty MeshProperty;

        public static new readonly DependencyProperty MaterialProperty;

        static DriverModelVisual3D()
        {
            Type thisType = typeof(DriverModelVisual3D);

            ModelFileProperty =
                DependencyProperty.Register("ModelFile", typeof(IModelFile), thisType,
                new PropertyMetadata(null, null));
            ModelPackageProperty =
                DependencyProperty.Register("ModelPackage", typeof(ModelPackage), thisType,
                new PropertyMetadata(null, null));
            MeshProperty =
                DependencyProperty.Register("Mesh", typeof(IndexedMesh), thisType,
                new UIPropertyMetadata(null, MeshChanged));
            MaterialProperty =
                DependencyProperty.Register("Material", typeof(PCMPMaterial), thisType,
                new UIPropertyMetadata(null, MaterialChanged));
        }

        public IModelFile ModelFile
        {
            get { return (IModelFile)GetValue(ModelFileProperty); }
            protected set { SetValue(ModelFileProperty, value); }
        }

        public ModelPackage ModelPackage
        {
            get { return (ModelPackage)GetValue(ModelPackageProperty); }
            protected set { SetValue(ModelPackageProperty, value); }
        }

        public IndexedMesh Mesh
        {
            get { return (IndexedMesh)GetValue(MeshProperty); }
            set { SetValue(MeshProperty, value); }
        }

        public new PCMPMaterial Material
        {
            get { return (PCMPMaterial)GetValue(MaterialProperty); }
            set { SetValue(MaterialProperty, value); }
        }

        public Material BaseMaterial
        {
            get { return base.Material; }
        }

        public bool IsEmissive { get; private set; }
        public bool HasTransparency { get; private set; }

        protected static void MeshChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DriverModelVisual3D)d).OnMeshChanged();
        }

        protected static new void MaterialChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DriverModelVisual3D)d).OnMaterialChanged();
        }

        protected virtual void OnMeshChanged()
        {
            //if (ModelFile == null || ModelPackage == null || Mesh == null)
            //{
            //    if (TriangleIndices.Count > 0)
            //        TriangleIndices.Clear();
            //    if (Vertices.Count > 0)
            //        Vertices.Clear();
            //
            //    return;
            //}

            if (!Mesh.VertexBuffer.HasBlendWeights)
                UseBlendWeights = false;
            
            Vertices        = Mesh.GetVertices();
            TriangleIndices = Mesh.GetTriangleIndices();
            Material        = Mesh.GetMaterial();

            if (Material == null)
                OnMaterialChanged();
        }

        protected override void OnBlendWeightsChanged()
        {
            base.OnBlendWeightsChanged();

            if (Material != null)
                this.OnMaterialChanged();
        }

        protected new void OnMaterialChanged()
        {
            if (Model.Geometry == null)
                return;

            MaterialGroup matGroup = new MaterialGroup();

            if (Material != null)
            {
                PCMPSubMaterial subMaterial = Material.SubMaterials[0];

                bool damage         = subMaterial.Damage;
                bool mask           = subMaterial.AlphaMask;
                bool transparency   = subMaterial.Transparency;
                bool emissive       = subMaterial.Emissive;
                bool specular       = subMaterial.Specular;

                PCMPTexture texInfo = (UseBlendWeights && damage) ? (mask) ? subMaterial.Textures[2] : subMaterial.Textures[1] : subMaterial.Textures[0];
                
                CachedTexture cTex = TextureCache.GetCachedTexture(texInfo);

                BitmapSourceLoadFlags loadFlags = (transparency || emissive) ? BitmapSourceLoadFlags.Transparency : BitmapSourceLoadFlags.Default;

                BitmapSource bmap = cTex.GetBitmapSource(loadFlags);

                matGroup.Children.Add(new DiffuseMaterial() {
                    Brush = new ImageBrush() {
                        ImageSource = bmap,
                        TileMode = TileMode.Tile,
                        Stretch = Stretch.Fill,
                        ViewportUnits = BrushMappingMode.Absolute
                    }
                });

                if (emissive)
                {
                    matGroup.Children.Add(new EmissiveMaterial() {
                        Brush = new ImageBrush() {
                            ImageSource = bmap,
                            TileMode = TileMode.Tile,
                            Stretch = Stretch.Fill,
                            ViewportUnits = BrushMappingMode.Absolute
                        }
                    });
                }
                else if (specular)
                {
                    matGroup.Children.Add(new SpecularMaterial() {
                        Brush = new ImageBrush() {
                            ImageSource = cTex.GetBitmapSource(BitmapSourceLoadFlags.AlphaMask),
                            TileMode = TileMode.Tile,
                            Stretch = Stretch.Fill,
                            ViewportUnits = BrushMappingMode.Absolute
                        },
                        SpecularPower = 75.0
                    });
                }

                IsEmissive = emissive;
                HasTransparency = transparency;
            }
            else
            {
                matGroup.Children.Add(new DiffuseMaterial() {
                    Brush = new SolidColorBrush(Color.FromArgb(64, 255, 64, 128))
                });
            }

            base.Material = matGroup;
        }

        public DriverModelVisual3D(IModelFile modelFile, ModelPackage modelPackage, IndexedMesh mesh, bool useBlendWeights)
            : base()
        {
            DoubleSided = true;
            UseBlendWeights = useBlendWeights;

            ModelFile = modelFile;
            ModelPackage = modelPackage;

            Mesh = mesh;
        }
    }
}
