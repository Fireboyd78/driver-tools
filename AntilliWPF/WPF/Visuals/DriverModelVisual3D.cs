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
                DependencyProperty.Register("Mesh", typeof(MeshDefinition), thisType,
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

        public MeshDefinition Mesh
        {
            get { return (MeshDefinition)GetValue(MeshProperty); }
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

            VertexData vertexData = ModelPackage.Vertices;
            IndexData indexData = ModelPackage.Indices;

            PCMPData materialData = ModelPackage.MaterialData;

            if (vertexData.VertexType == FVFType.Vertex12)
                UseBlendWeights = false;

            Vertex[] vertices = vertexData.Buffer;
            ushort[] indices = indexData.Buffer;

            int nVerts = vertices.Length;

            var verts = new List<Vertex>(nVerts);
            var tris = new Int32Collection();

            for (uint v = 0; v <= Mesh.NumVertices; v++)
            {
                uint vIdx = (uint)Mesh.BaseVertexIndex + Mesh.MinIndex + v;

                if (vIdx == vertices.Length)
                    break;

                verts.Add(vertices[vIdx]);
            }

            for (int i = 0; i < Mesh.PrimitiveCount; i++)
            {
                uint idx = Mesh.StartIndex;
                int vIdx = Mesh.BaseVertexIndex;

                uint i0, i1, i2;

                if (Mesh.PrimitiveType == D3DPRIMITIVETYPE.D3DPT_TRIANGLESTRIP)
                {
                    if (i % 2 == 1.0)
                    {
                        i0 = indices[idx + i];
                        i1 = indices[idx + (i + 1)];
                        i2 = indices[idx + (i + 2)];
                    }
                    else
                    {
                        i0 = indices[idx + (i + 2)];
                        i1 = indices[idx + (i + 1)];
                        i2 = indices[idx + i];
                    }

                    // When reading in the vertices, the YZ-axis was flipped
                    // Therefore i0 and i2 need to be flipped for proper face orientation
                    // This was AFTER learning the hard way...
                    if ((i0 != i1) && (i0 != i2) && (i1 != i2))
                    {
                        tris.Add((int)(i2 - Mesh.MinIndex));
                        tris.Add((int)(i1 - Mesh.MinIndex));
                        tris.Add((int)(i0 - Mesh.MinIndex));
                    }
                }
                else if (Mesh.PrimitiveType == D3DPRIMITIVETYPE.D3DPT_TRIANGLELIST)
                {
                    DSCript.DSC.Log("Loading a triangle list primitive!");

                    i0 = indices[idx + i];
                    i1 = indices[idx + (i + 1)];
                    i2 = indices[idx + (i + 2)];

                    tris.Add((int)(i2 - Mesh.MinIndex));
                    tris.Add((int)(i1 - Mesh.MinIndex));
                    tris.Add((int)(i0 - Mesh.MinIndex));
                }
                else
                {
                    throw new Exception("Unknown primitive type!");
                }
            }

            Vertices = verts;
            TriangleIndices = tris;

            // Add material
            if (Mesh.SourceUID != 0)
            {
                if (Mesh.SourceUID != (uint)PackageType.VehicleGlobals)
                {
                    if (Mesh.SourceUID == ModelPackage.UID || Mesh.SourceUID == 0xFFFD)
                    {
                        Material = materialData.Materials[Mesh.MaterialId];
                    }
                    else
                    {
                        ModelPackage mPak = ModelFile.Models.Find((m) => m.UID == Mesh.SourceUID);

                        if (mPak != null)
                            Material = mPak.MaterialData.Materials[Mesh.MaterialId];
                    }
                }
                else if (ModelPackage.HasGlobals && Mesh.MaterialId < ModelPackage.Globals.StandaloneTextures.Count)
                {
                    Material = ModelPackage.Globals.StandaloneTextures[Mesh.MaterialId];
                }
            }
            else
            {
                OnMaterialChanged();
            }
        }

        protected new void OnMaterialChanged()
        {
            if (Model.Geometry == null)
                return;

            MaterialGroup matGroup = new MaterialGroup();

            if (Material != null)
            {
                PCMPSubMaterial subMaterial = Material.SubMaterials[0];

                bool transparency = false;

                bool damage = false;
                bool mask = false;

                bool specular = false;
                bool emissive = false;

                uint type = subMaterial.Unk1;
                uint spec = subMaterial.Unk2;
                uint flags = subMaterial.Unk3;

                if (flags == 0x400 || flags == 0x1000)
                    mask = true;
                if (flags == 0x800 || flags == 0x1000)
                    damage = true;
                if (spec == 0x201 || spec == 0x102)
                    specular = true;
                if (((type & 0x18000) == 0x18000) || ((type & 0x1E) == 0x1E))
                    emissive = true;
                if (((type & 0x1) == 0x1 && !specular) || type == 0x4 && !specular)
                    transparency = true;

                PCMPTexture texInfo = (UseBlendWeights && damage) ? (mask) ? subMaterial.Textures[2] : subMaterial.Textures[1] : subMaterial.Textures[0];
                
                CachedTexture cTex = TextureCache.GetCachedTexture(texInfo);

                bool alphaBlend = transparency || emissive;

                BitmapSource bmap = cTex.GetBitmapSource(alphaBlend);

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
                            ImageSource = cTex.GetBitmapSourceAlphaChannel(),
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

        public DriverModelVisual3D(IModelFile modelFile, ModelPackage modelPackage, MeshDefinition mesh)
            : base()
        {
            DoubleSided = true;

            ModelFile = modelFile;
            ModelPackage = modelPackage;

            Mesh = mesh;
        }
    }
}
