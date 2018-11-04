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
        public static readonly DependencyProperty ModelPackageProperty;
        public static readonly DependencyProperty MeshProperty;

        public static new readonly DependencyProperty MaterialProperty;

        static DriverModelVisual3D()
        {
            Type thisType = typeof(DriverModelVisual3D);

            ModelPackageProperty =
                DependencyProperty.Register("ModelPackage", typeof(ModelPackagePC), thisType,
                new PropertyMetadata(null, null));
            MeshProperty =
                DependencyProperty.Register("Mesh", typeof(MeshDefinition), thisType,
                new UIPropertyMetadata(null, MeshChanged));
            MaterialProperty =
                DependencyProperty.Register("Material", typeof(DSCript.Models.MaterialDataPC), thisType,
                new UIPropertyMetadata(null, MaterialChanged));
        }

        public MeshDefinition Mesh
        {
            get { return (MeshDefinition)GetValue(MeshProperty); }
            set { SetValue(MeshProperty, value); }
        }

        public new MaterialDataPC Material
        {
            get { return (MaterialDataPC)GetValue(MaterialProperty); }
            set { SetValue(MaterialProperty, value); }
        }

        public Material BaseMaterial
        {
            get { return base.Material; }
        }

        public static readonly DiffuseMaterial NullMaterial = new DiffuseMaterial() {
            Brush = new SolidColorBrush(Color.FromArgb(255, 255, 64, 128))
        };

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
            if (!Mesh.VertexBuffer.HasDamageVertices)
                UseBlendWeights = false;
            
            Vertices        = Mesh.GetVertices(true);
            TriangleIndices = new Int32Collection(Mesh.GetTriangleIndices(true));
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
            if ((Model == null) || (Model.Geometry == null))
                return;
            
            var matGroup = new MaterialGroup();

            if (Material != null)
            {
                var substance = Material.Substances[0];

                var eFlags = substance.ExtraFlags;
                
                var alpha = substance.HasAlpha;
                var emissive = substance.IsEmissive;
                var specular = substance.IsSpecular;

                var texIdx = 0;

                if (UseBlendWeights)
                {
                    // ColorMask = 0
                    // Damage = 1
                    // DamageWithColorMask = 2
                    texIdx = (((int)substance.ExtraFlags >> 3) & 3);
                }

                var texInfo = substance.Textures[texIdx];
                
                var cTex = TextureCache.GetTexture(texInfo);
                var texMap = cTex.Bitmap;

                var transparent = (alpha && !specular);
                var loadFlags = (transparent || emissive) ? BitmapSourceLoadFlags.Transparency : BitmapSourceLoadFlags.Default;

                var bmap = texMap.ToBitmapSource(loadFlags);

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
                            ImageSource = texMap.ToBitmapSource(BitmapSourceLoadFlags.AlphaMask),
                            TileMode = TileMode.Tile,
                            Stretch = Stretch.Fill,
                            ViewportUnits = BrushMappingMode.Absolute
                        },
                        SpecularPower = 75.0
                    });
                }

                IsEmissive = emissive;
                HasTransparency = alpha;
            }
            else
            {
                matGroup.Children.Add(NullMaterial);
            }

            base.Material = matGroup;
        }

        public DriverModelVisual3D(MeshDefinition mesh, bool useBlendWeights)
            : base()
        {
            DoubleSided = true;
            UseBlendWeights = useBlendWeights;

            Mesh = mesh;
        }
    }
}
