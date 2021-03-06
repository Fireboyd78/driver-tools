﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Media.Composition;

namespace Antilli
{
    public class AntilliMesh3D
    {
        public List<Vertex> Vertices { get; }
        public Int32Collection TriangleIndices { get; }

        public float TweenFactor => 1.0f;

        public MeshGeometry3D ToGeometry(bool tweenVertices = false)
        {
            int nVerts = Vertices.Count;

            var positions = new Point3DCollection(nVerts);
            var normals = new Vector3DCollection(nVerts);
            var textureCoordinates = new PointCollection(nVerts);
            
            foreach (var vertex in Vertices)
            {
                var pos = vertex.Position;
                var nor = vertex.Normal;
                var uv = vertex.UV;

                if (tweenVertices)
                {
                    pos = (pos + (vertex.PositionW * TweenFactor));
                    nor = (nor + (vertex.NormalW * TweenFactor));
                }

                positions.Add(new Point3D(pos.X, pos.Y, pos.Z));
                normals.Add(new Vector3D(nor.X, nor.Y, nor.Z));
                textureCoordinates.Add(new Point(uv.X, uv.Y));
            }

            return new MeshGeometry3D() {
                Positions = positions,
                Normals = normals,
                TextureCoordinates = textureCoordinates,
                TriangleIndices = TriangleIndices
            };
        }

        public AntilliMesh3D(List<Vertex> vertices, Int32Collection triangleIndices)
        {
            if (vertices == null || triangleIndices == null)
                throw new ArgumentNullException("Vertices/Indices cannot be null.");

            Vertices = vertices;
            TriangleIndices = triangleIndices;
        }
    }
    
    public class AntilliModelVisual3D : ModelVisual3D
    {
        public static readonly DependencyProperty DoubleSidedProperty;
        public static readonly DependencyProperty UseBlendWeightsProperty;

        public static readonly DependencyProperty MeshProperty;
        public static readonly DependencyProperty ModelProperty;

        public static readonly DependencyProperty OpacityProperty;

        static AntilliModelVisual3D()
        {
            Type thisType = typeof(AntilliModelVisual3D);

            DoubleSidedProperty = DependencyProperty.Register("DoubleSided", typeof(bool), thisType,
                new UIPropertyMetadata(true, DoubleSidedChanged));
            UseBlendWeightsProperty = DependencyProperty.Register("UseBlendWeights", typeof(bool), thisType,
                new UIPropertyMetadata(false, UseBlendWeightsChanged));
            
            MeshProperty = DependencyProperty.Register("Mesh", typeof(AntilliMesh3D), thisType,
                new PropertyMetadata(null, MeshChanged));
            ModelProperty = DependencyProperty.Register("Model", typeof(SubModel), thisType,
                new PropertyMetadata(null, ModelChanged));

            OpacityProperty = DependencyProperty.Register("Opacity", typeof(double), thisType,
                new UIPropertyMetadata(1.0, OpacityChanged));
        }

        public static readonly DiffuseMaterial NullMaterial = new DiffuseMaterial() {
            Brush = new SolidColorBrush(Color.FromArgb(255, 255, 64, 128)),
        };

        public static readonly DiffuseMaterial ShadowMaterial = new DiffuseMaterial() {
            Brush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)) {
                Opacity = 0.5f
            },
        };

        public bool IsEmissive { get; private set; }
        public bool HasTransparency { get; private set; }

        public new GeometryModel3D Content
        {
            get { return base.Content as GeometryModel3D; }
            set { base.Content = value; }
        }

        public bool DoubleSided
        {
            get { return (bool)GetValue(DoubleSidedProperty); }
            set { SetValue(DoubleSidedProperty, value); }
        }

        public bool UseBlendWeights
        {
            get { return (bool)GetValue(UseBlendWeightsProperty); }
            set { SetValue(UseBlendWeightsProperty, value); }
        }
        
        public AntilliMesh3D Mesh
        {
            get { return (AntilliMesh3D)GetValue(MeshProperty); }
            set { SetValue(MeshProperty, value); }
        }

        public SubModel Model
        {
            get { return (SubModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public double Opacity
        {
            get { return (double)GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        protected static void DoubleSidedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AntilliModelVisual3D)d).OnDoubleSidedChanged();
        }

        protected static void UseBlendWeightsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AntilliModelVisual3D)d).OnUseBlendWeightsChanged();
        }

        protected static void OpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AntilliModelVisual3D)d).OnOpacityChanged();
        }

        protected static void MeshChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AntilliModelVisual3D)d).OnMeshChanged();
        }

        protected static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AntilliModelVisual3D)d).OnModelChanged();
        }
        
        protected virtual void OnDoubleSidedChanged()
        {
            UpdateMaterial();
        }

        protected virtual void OnUseBlendWeightsChanged()
        {
            if (Mesh != null)
            {
                OnMeshChanged();
                UpdateMaterial();
            }
        }

        protected virtual void OnOpacityChanged()
        {
            UpdateMaterial();
        }

        protected virtual void UpdateMaterial()
        {
            var materials = new MaterialGroup();
            var material = Model.Material;

            if (material.UID == 0xCCCC)
            {
                materials.Children.Add(ShadowMaterial);

                HasTransparency = true;
            }
            else
            {
                MaterialDataPC mtl = null;

                var package = Model.ModelPackage;

                if (MaterialManager.Find(package, material, out mtl) > 0)
                {
                    var substance = mtl.Substances[0];

                    var eFlags = substance.ExtraFlags;

                    var alpha = substance.HasAlpha;
                    var emissive = substance.IsEmissive;
                    var specular = substance.IsSpecular;

                    var texIdx = 0;

                    if (UseBlendWeights)
                    {
                        if (eFlags.HasFlag(SubstanceExtraFlags.DamageAndColorMaskAlphaMaps))
                        {
                            texIdx = 2;
                        }
                        else if ((eFlags & (SubstanceExtraFlags.DamageAndColorMask)) != 0)
                        {
                            texIdx = 1;
                        }
                    }

                    var texInfo = substance.Textures[texIdx];

                    var cTex = (texInfo.Flags != -666) ? TextureCache.GetTexture(texInfo) : null;
                    var texMap = (cTex != null) ? cTex.Bitmap : null;

                    if (texMap == null)
                    {
                        // null texture?
                        materials.Children.Add(NullMaterial);

                        IsEmissive = false;
                        HasTransparency = false;
                    }
                    else
                    {
                        var transparent = (alpha && !specular);
                        var loadFlags = (transparent || emissive) ? BitmapSourceLoadFlags.Transparency : BitmapSourceLoadFlags.Default;

                        var bmap = texMap.GetBitmapSource(loadFlags);

                        materials.Children.Add(new DiffuseMaterial() {
                            Brush = new ImageBrush() {
                                Opacity = Opacity,
                                ImageSource = bmap,
                                TileMode = TileMode.Tile,
                                Stretch = Stretch.Fill,
                                ViewportUnits = BrushMappingMode.Absolute
                            }
                        });

                        if (emissive)
                        {
                            materials.Children.Add(new EmissiveMaterial() {
                                Brush = new ImageBrush() {
                                    Opacity = Opacity,
                                    ImageSource = bmap,
                                    TileMode = TileMode.Tile,
                                    Stretch = Stretch.Fill,
                                    ViewportUnits = BrushMappingMode.Absolute
                                }
                            });
                        }
                        else if (specular)
                        {
                            materials.Children.Add(new SpecularMaterial() {
                                Brush = new ImageBrush() {
                                    Opacity = Opacity,
                                    ImageSource = texMap.GetBitmapSource(BitmapSourceLoadFlags.AlphaMask),
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

                    if (cTex != null)
                    {
                        // since WPF uses BitmapSource for the actual texture,
                        // we can let the texture cache know we're done with it
                        TextureCache.Release(cTex);
                    }
                }
                else
                {
                    materials.Children.Add(NullMaterial);
                }
            }
            
            Content.Material = materials;
            Content.BackMaterial = (DoubleSided) ? materials : null;
        }

        protected virtual void OnMeshChanged()
        {
            Content.Geometry = Mesh?.ToGeometry(UseBlendWeights);
        }
        
        protected virtual void OnModelChanged()
        {
            if (Mesh != null)
            {
                Mesh.Vertices.Clear();
                Mesh.TriangleIndices.Clear();
            }

            if (Model != null)
            {
                var indices = new List<int>();
                var vertices = Model.GetVertices(true, ref indices);

                var triangleIndices = new Int32Collection(indices);

                Mesh = new AntilliMesh3D(vertices, triangleIndices);

                UpdateMaterial();
            }
            else
            {
                Mesh = null;

                Content.Material = null;
                Content.BackMaterial = null;
            }
        }
        
        public AntilliModelVisual3D()
            : base()
        {
            Content = new GeometryModel3D();
        }

        public AntilliModelVisual3D(SubModel model, bool useBlendWeights)
            : this()
        {
            UseBlendWeights = useBlendWeights;
            Model = model;
        }
    }
}
