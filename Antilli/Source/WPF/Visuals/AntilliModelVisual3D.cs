using System;
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

    public class AntilliMaterial
    {
        public static readonly float TweenFactorToApplyDamage = 0.3f;

        public static readonly DiffuseMaterial NullMaterial = new DiffuseMaterial() {
            Brush = new SolidColorBrush(Color.FromArgb(255, 255, 64, 128)),
        };

        public static readonly DiffuseMaterial ShadowMaterial = new DiffuseMaterial() {
            Brush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)) {
                Opacity = 0.5f
            },
        };

        public struct Compiled
        {
            public bool HasAlpha { get; set; }
            public bool HasBumpMap { get; set; }

            public bool IsSpecular { get; set; }
            public bool IsEmissive { get; set; }

            public bool IsNull { get; }

            public bool IsTransparent
            {
                get { return HasAlpha && !IsSpecular; }
            }

            public SubstanceDataPC Substance { get; }
            public TextureDataPC Texture { get; }

            public IEnumerable<Material> GetMaterials(double opacity)
            {
                var texture = (!IsNull) ? Texture : null;

                var cTex = (texture.Flags != -666) ? TextureCache.GetTexture(texture) : null;
                var texMap = (cTex != null) ? cTex.Bitmap : null;

                if (texMap == null)
                {
                    // null texture?
                    HasAlpha = false;
                    HasBumpMap = false;
                    IsSpecular = false;
                    IsEmissive = false;

                    yield return NullMaterial;
                }
                else
                {
                    var loadFlags = (IsTransparent || IsEmissive) ? BitmapSourceLoadFlags.Transparency : BitmapSourceLoadFlags.Default;

                    yield return new DiffuseMaterial()
                    {
                        Brush = new ImageBrush()
                        {
                            Opacity = opacity,
                            ImageSource = texMap.GetBitmapSource(loadFlags),
                            TileMode = TileMode.Tile,
                            Stretch = Stretch.Fill,
                            ViewportUnits = BrushMappingMode.Absolute
                        }
                    };

                    if (IsEmissive)
                    {
                        yield return new EmissiveMaterial()
                        {
                            Brush = new ImageBrush()
                            {
                                Opacity = opacity,
                                ImageSource = texMap.GetBitmapSource(loadFlags),
                                TileMode = TileMode.Tile,
                                Stretch = Stretch.Fill,
                                ViewportUnits = BrushMappingMode.Absolute
                            }
                        };
                    }
                    else if (IsSpecular)
                    {
                        yield return new SpecularMaterial()
                        {
                            Brush = new ImageBrush()
                            {
                                Opacity = opacity,
                                ImageSource = texMap.GetBitmapSource(BitmapSourceLoadFlags.AlphaMask),
                                TileMode = TileMode.Tile,
                                Stretch = Stretch.Fill,
                                ViewportUnits = BrushMappingMode.Absolute
                            },
                            SpecularPower = 75.0
                        };
                    }

                    // since WPF uses BitmapSource for the actual texture,
                    // we can let the texture cache know we're done with it
                    TextureCache.Release(cTex);
                }

                yield break;
            }

            public Compiled(SubstanceDataPC substance, int texSlot)
            {
                Substance = substance;

                IsEmissive = substance.IsEmissive;
                IsSpecular = substance.IsSpecular;
                HasAlpha = substance.HasAlpha;

                var eflags = (SubstanceExtraFlags)substance.TextureFlags;
                var hasTextures = (substance.Textures != null);

                HasBumpMap = eflags.HasFlag(SubstanceExtraFlags.BumpMap);

                if (hasTextures)
                {
                    // requested damage textures?
                    if (texSlot == 1)
                    {
                        if (!eflags.HasFlag(SubstanceExtraFlags.DPL_SwapDamageAndColorMaskBits))
                        {
                            // fix slot for Driv3r vehicles
                            if (eflags.HasFlag(SubstanceExtraFlags.DamageAndColorMask_AlphaMaps))
                            {
                                texSlot = 2;
                            }
                            else if (eflags.HasFlag(SubstanceExtraFlags.Damage))
                            {
                                texSlot = 1;
                            }
                            else
                            {
                                // invalid slot requested
                                texSlot = 0;
                            }
                        }
                        else if (!eflags.HasFlag(SubstanceExtraFlags.DPL_Damage))
                        {
                            // invalid slot requested
                            texSlot = 0;
                        }
                    }

                    // use first available texture if the slot doesn't exist
                    while (texSlot >= substance.Textures.Count)
                        texSlot--;
                }
                else
                {
                    // no useable textures
                    texSlot = -1;
                }

                if (texSlot > -1)
                {
                    Texture = substance.Textures[texSlot];
                    IsNull = false;
                }
                else
                {
                    Texture = null;
                    IsNull = true;
                }
            }
        }

        public IMaterialPackage Package { get; set; }
        public MaterialHandle Material { get; set; }

        public bool HasAlpha { get; protected set; }
        public bool HasBumpMap { get; protected set; }

        public bool IsEmissive { get; protected set; }
        public bool IsSpecular { get; protected set; }
        public bool IsTransparent { get; protected set; }

        public bool IsShadow { get; set; }

        public double Opacity { get; set; }

        public int SubstanceSlot { get; set; }
        public int TextureSlot { get; set; }

        public MaterialGroup Compile()
        {
            var materials = new MaterialGroup();

            if (IsShadow || Material.UID == 0xCCCC)
            {
                materials.Children.Add(ShadowMaterial);
                IsTransparent = true;
            }
            else
            {
                MaterialDataPC mtl = null;

                if (MaterialManager.Find(Package, Material, out mtl) > 0)
                {
                    var substance = mtl.Substances[SubstanceSlot];

                    var compiled = new Compiled(substance, TextureSlot);

                    foreach (var material in compiled.GetMaterials(Opacity))
                        materials.Children.Add(material);

                    HasAlpha = compiled.HasAlpha;
                    HasBumpMap = compiled.HasBumpMap;

                    IsEmissive = compiled.IsEmissive;
                    IsSpecular = compiled.IsSpecular;
                    IsTransparent = compiled.IsTransparent;
                }
                else
                {
                    materials.Children.Add(NullMaterial);
                }
            }

            return materials;
        }

        public AntilliMaterial(IMaterialPackage package, MaterialHandle material)
        {
            Package = package;
            Material = material;
        }
    }

    public static class MeshGeometry3DExtensions
    {
        public static readonly DependencyProperty TweenFactorProperty = DependencyProperty.RegisterAttached("TweenFactor",
            typeof(float),
            typeof(MeshGeometry3D),
            new UIPropertyMetadata(float.NaN, delegate (DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var mesh = (MeshGeometry3D)d;

                var tweenFactor = (float)e.NewValue;
                var oldFactor = (float)e.OldValue;

                if (tweenFactor != oldFactor)
                {
                    var positions = (Point3DCollection)mesh.GetValue(VertexPositionsProperty);
                    var normals = (Vector3DCollection)mesh.GetValue(VertexNormalsProperty);

                    // can we tween stuff?
                    if (positions != null || normals != null)
                    {
                        var needTween = false;

                        if (!float.IsNaN(tweenFactor))
                        {
                            if (tweenFactor != 0.0f)
                                needTween = true;
                        }

                        if (needTween)
                        {
                            // tween the vertices
                            mesh.TweenVertices(positions, normals);
                        }
                        else if (!float.IsNaN(oldFactor)) // previously tweened?
                        {
                            // restore the positions/normals as needed if we're not tweening
                            if (positions != null)
                                mesh.SetValue(MeshGeometry3D.PositionsProperty, positions.CloneCurrentValue());
                            if (normals != null)
                                mesh.SetValue(MeshGeometry3D.NormalsProperty, normals.CloneCurrentValue());
                        }
                    }
                }
            }));

        public static readonly DependencyProperty VertexPositionsProperty = DependencyProperty.RegisterAttached("VertexPositions",
            typeof(Point3DCollection),
            typeof(MeshGeometry3D),
            new PropertyMetadata(null, delegate (DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var mesh = (MeshGeometry3D)d;
                var positions = (Point3DCollection)e.NewValue;

                if (positions != null)
                {
                    var count = positions.Count;
                    var numVertices = (int)mesh.GetValue(NumVerticesProperty);

                    if (count != numVertices)
                        throw new Exception("BAD VERTEX POSITIONS: NumVertices mismatch!");

                    var tweenFactor = (float)mesh.GetValue(TweenFactorProperty);

                    if (!float.IsNaN(tweenFactor) && (tweenFactor > 0.0f))
                        RetweenVertices(mesh, positions, null, null, null, numVertices, tweenFactor);
                }
                else
                {
                    // clear existing weight positions if needed
                    if ((Point3DCollection)e.OldValue != null)
                        mesh.ClearValue(WeightPositionsProperty);
                }
            }));

        public static readonly DependencyProperty VertexNormalsProperty = DependencyProperty.RegisterAttached("VertexNormals",
            typeof(Vector3DCollection),
            typeof(MeshGeometry3D),
            new PropertyMetadata(null, delegate (DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var mesh = (MeshGeometry3D)d;
                var normals = (Vector3DCollection)e.NewValue;

                if (normals != null)
                {
                    var count = normals.Count;
                    var numVertices = (int)mesh.GetValue(NumVerticesProperty);

                    if (count != numVertices)
                        throw new Exception("BAD VERTEX NORMALS: NumVertices mismatch!");

                    var tweenFactor = (float)mesh.GetValue(TweenFactorProperty);

                    if (!float.IsNaN(tweenFactor) && (tweenFactor > 0.0f))
                        RetweenVertices(mesh, null, normals, null, null, numVertices, tweenFactor);
                }
                else
                {
                    // clear existing weight normals if needed
                    if ((Vector3DCollection)e.OldValue != null)
                        mesh.ClearValue(WeightNormalsProperty);
                }
            }));

        public static readonly DependencyProperty WeightPositionsProperty = DependencyProperty.RegisterAttached("WeightPositions",
            typeof(Point3DCollection),
            typeof(MeshGeometry3D),
            new PropertyMetadata(null, delegate (DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var mesh = (MeshGeometry3D)d;
                var weightPositions = (Point3DCollection)e.NewValue;

                var positions = (Point3DCollection)mesh.GetValue(VertexPositionsProperty);

                if (positions == null)
                    throw new Exception("BAD WEIGHT POSITIONS: VertexPositions were not set first!");

                if (weightPositions != null)
                {
                    var count = weightPositions.Count;
                    var numVertices = (int)mesh.GetValue(NumVerticesProperty);

                    if (count != numVertices)
                        throw new Exception("BAD WEIGHT POSITIONS: NumVertices mismatch!");

                    var tweenFactor = (float)mesh.GetValue(TweenFactorProperty);

                    if (!float.IsNaN(tweenFactor) && (tweenFactor > 0.0f))
                        RetweenVertices(mesh, positions, null, weightPositions, null, numVertices, tweenFactor);
                }
                else
                {
                    // restore previous positions if needed
                    if ((Point3DCollection)e.OldValue != null)
                        mesh.SetValue(MeshGeometry3D.PositionsProperty, positions);
                }
            }));

        public static readonly DependencyProperty WeightNormalsProperty = DependencyProperty.RegisterAttached("WeightNormals",
            typeof(Vector3DCollection),
            typeof(MeshGeometry3D),
            new PropertyMetadata(null, delegate (DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var mesh = (MeshGeometry3D)d;
                var weightNormals = (Vector3DCollection)e.NewValue;

                var normals = (Vector3DCollection)mesh.GetValue(VertexNormalsProperty);

                if (normals == null)
                    throw new Exception("BAD WEIGHT NORMALS: VertexNormals were not set first!");

                if (weightNormals != null)
                {
                    var count = weightNormals.Count;
                    var numVertices = (int)mesh.GetValue(NumVerticesProperty);

                    if (count != numVertices)
                        throw new Exception("BAD WEIGHT NORMALS: NumVertices mismatch!");

                    var tweenFactor = (float)mesh.GetValue(TweenFactorProperty);

                    if (!float.IsNaN(tweenFactor) && (tweenFactor > 0.0f))
                        RetweenVertices(mesh, null, normals, null, weightNormals, numVertices, tweenFactor);
                }
                else
                {
                    // restore previous normals if needed
                    if ((Vector3DCollection)e.OldValue != null)
                        mesh.SetValue(MeshGeometry3D.NormalsProperty, normals);
                }
            }));

        public static readonly DependencyProperty NumVerticesProperty = DependencyProperty.RegisterAttached("NumVertices",
            typeof(int),
            typeof(MeshGeometry3D),
            new PropertyMetadata(0, delegate (DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var mesh = (MeshGeometry3D)d;
                var numVertices = (int)e.NewValue;

                if (numVertices != (int)e.OldValue)
                {
                    mesh.SetValue(MeshGeometry3D.PositionsProperty, new Point3DCollection(numVertices));
                    mesh.SetValue(MeshGeometry3D.NormalsProperty, new Vector3DCollection(numVertices));
                    mesh.SetValue(MeshGeometry3D.TextureCoordinatesProperty, new PointCollection(numVertices));

                    mesh.ClearValue(WeightPositionsProperty);
                    mesh.ClearValue(WeightNormalsProperty);
                }
            }));

        private static void RetweenVertices(MeshGeometry3D mesh,
            Point3DCollection vertexPositions,
            Vector3DCollection vertexNormals,
            Point3DCollection weightPositions,
            Vector3DCollection weightNormals,
            int numVertices,
            float tweenFactor)
        {
            if (vertexPositions == null)
                vertexPositions = (Point3DCollection)mesh.GetValue(VertexPositionsProperty);
            if (vertexNormals == null)
                vertexNormals = (Vector3DCollection)mesh.GetValue(VertexNormalsProperty);
            if (weightPositions == null)
                weightPositions = (Point3DCollection)mesh.GetValue(WeightPositionsProperty);
            if (weightNormals == null)
                weightNormals = (Vector3DCollection)mesh.GetValue(WeightNormalsProperty);
        }

        private static void TweenVertices(MeshGeometry3D mesh,
            Point3DCollection vertexPositions,
            Vector3DCollection vertexNormals,
            Point3DCollection weightPositions,
            Vector3DCollection weightNormals,
            int numVertices,
            float tweenFactor)
        {
            Point3DCollection positions = null;
            Vector3DCollection normals = null;

            var hasPositions = (vertexPositions != null && weightPositions != null);
            var hasNormals = (vertexNormals != null && weightNormals != null);

            if (hasPositions)
            {
                positions = new Point3DCollection();

                if (hasNormals)
                {
                    // positions and normals
                    normals = new Vector3DCollection();

                    for (int i = 0; i < numVertices; i++)
                    {
                        var pos = vertexPositions[i];
                        var nor = vertexNormals[i];

                        var posW = weightPositions[i];
                        var norW = weightNormals[i];

                        normals.Add(nor + (norW * tweenFactor));

                        positions.Add(new Point3D(
                            x: pos.X + (posW.X * tweenFactor),
                            y: pos.Y + (posW.Y * tweenFactor),
                            z: pos.Z + (posW.Z * tweenFactor)));
                    }
                }
                else
                {
                    // positions, no normals
                    for (int i = 0; i < numVertices; i++)
                    {
                        var pos = vertexPositions[i];
                        var posW = weightPositions[i];

                        positions.Add(new Point3D(
                            x: pos.X + (posW.X * tweenFactor),
                            y: pos.Y + (posW.Y * tweenFactor),
                            z: pos.Z + (posW.Z * tweenFactor)));
                    }
                }
            }
            else if (hasNormals)
            {
                // normals, no positions
                normals = new Vector3DCollection();

                for (int i = 0; i < numVertices; i++)
                {
                    var nor = vertexNormals[i];
                    var norW = weightNormals[i];

                    normals.Add(nor + (norW * tweenFactor));
                }
            }

            if (positions != null)
                mesh.SetValue(MeshGeometry3D.PositionsProperty, positions);
            if (normals != null)
                mesh.SetValue(MeshGeometry3D.NormalsProperty, normals);
        }

        private static void TweenVertices(this MeshGeometry3D mesh, Point3DCollection vertexPositions, Vector3DCollection vertexNormals)
        {
            var weightPositions = (Point3DCollection)mesh.GetValue(WeightPositionsProperty);
            var weightNormals = (Vector3DCollection)mesh.GetValue(WeightNormalsProperty);

            if (weightPositions != null || weightNormals != null)
            {
                var numVertices = (int)mesh.GetValue(NumVerticesProperty);
                var tweenFactor = (float)mesh.GetValue(TweenFactorProperty);

                TweenVertices(mesh, vertexPositions, vertexNormals, weightPositions, weightNormals, numVertices, tweenFactor);
            }
        }
    }

    public class AntilliModel3D
    {
        public List<Vertex> Vertices { get; }
        public Int32Collection TriangleIndices { get; }

        public float TweenFactor { get; set; }

        public MeshGeometry3D ToMesh()
        {
            int nVerts = Vertices.Count;

            var mesh = new MeshGeometry3D();

            // this sets up everything for dynamic tweening
            mesh.SetValue(MeshGeometry3DExtensions.NumVerticesProperty, nVerts);

            var positions = mesh.Positions;
            var normals = mesh.Normals;
            var textureCoordinates = mesh.TextureCoordinates;

            var weightPositions = new Point3DCollection(nVerts);
            var weightNormals = new Vector3DCollection(nVerts);

            foreach (var vertex in Vertices)
            {
                var pos = vertex.Position;
                var nor = vertex.Normal;
                var uv = vertex.UV;

                var posW = vertex.PositionW;
                var norW = vertex.NormalW;

                positions.Add(new Point3D(pos.X, pos.Y, pos.Z));
                normals.Add(new Vector3D(nor.X, nor.Y, nor.Z));
                textureCoordinates.Add(new Point(uv.X, uv.Y));

                weightPositions.Add(new Point3D(posW.X, posW.Y, posW.Z));
                weightNormals.Add(new Vector3D(norW.X, norW.Y, norW.Z));
            }

            mesh.TriangleIndices = TriangleIndices;

            // copy unmodified positions/normals to allow for dynamic tweening
            mesh.SetValue(MeshGeometry3DExtensions.VertexPositionsProperty, positions.CloneCurrentValue());
            mesh.SetValue(MeshGeometry3DExtensions.VertexNormalsProperty, normals.CloneCurrentValue());

            // tweening stuff
            mesh.SetValue(MeshGeometry3DExtensions.WeightPositionsProperty, weightPositions);
            mesh.SetValue(MeshGeometry3DExtensions.WeightNormalsProperty, weightNormals);
            mesh.SetValue(MeshGeometry3DExtensions.TweenFactorProperty, TweenFactor);

            return mesh;
        }

        public AntilliModel3D(List<Vertex> vertices, Int32Collection triangleIndices)
        {
            if (vertices == null || triangleIndices == null)
                throw new ArgumentNullException("Vertices/Indices cannot be null.");

            Vertices = vertices;
            TriangleIndices = triangleIndices;
            TweenFactor = float.NaN;
        }
    }

    public static class GeometryModel3DExtensions
    {
        public static readonly DependencyProperty DoubleSidedProperty = DependencyProperty.RegisterAttached("DoubleSided",
            typeof(bool),
            typeof(GeometryModel3D),
            new UIPropertyMetadata(false, delegate (DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var geometry = (GeometryModel3D)d;

                if (geometry.Material != null)
                    geometry.BackMaterial = ((bool)e.NewValue == true) ? geometry.Material : null;
            }));

        public static readonly DependencyProperty OpacityProperty = DependencyProperty.RegisterAttached("Opacity",
            typeof(double),
            typeof(GeometryModel3D),
            new UIPropertyMetadata(1.0, delegate (DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var geometry = (GeometryModel3D)d;

                if (geometry.Material != null)
                {
                    var opacity = (double)e.NewValue;
                    var oldval = (double)e.OldValue;

                    if (opacity != oldval)
                        geometry.Material.SetOpacity(opacity);
                }
            }));

        public static readonly DependencyProperty TweenFactorProperty = DependencyProperty.RegisterAttached("TweenFactor",
            typeof(float),
            typeof(GeometryModel3D),
            new UIPropertyMetadata(float.NaN, delegate (DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var geometry = (GeometryModel3D)d;

                var tweenFactor = (float)e.NewValue;
                var oldFactor = (float)e.OldValue;

                if (tweenFactor != oldFactor)
                {
                    var meshgeom = (AntilliMeshGeometry3D)geometry.GetValue(MeshGeometryProperty);

                    if (meshgeom != null)
                    {
                        var mesh = geometry.Geometry as MeshGeometry3D;

                        if (mesh !=null)
                            mesh.SetValue(MeshGeometry3DExtensions.TweenFactorProperty, tweenFactor);

                        var material = meshgeom.Material;
                        var texSlot = -1;

                        if (!float.IsNaN(tweenFactor) && tweenFactor >= AntilliMaterial.TweenFactorToApplyDamage)
                        {
                            texSlot = 1;
                        }
                        else
                        {
                            texSlot = 0;
                        }

                        // recompile material?
                        if (material.TextureSlot != texSlot)
                        {
                            material.TextureSlot = texSlot;
                            geometry.Material = material.Compile();

                            if (meshgeom.DoubleSided)
                                geometry.BackMaterial = geometry.Material;
                        }
                    }
                }
            }));

        public static readonly DependencyProperty MeshGeometryProperty = DependencyProperty.RegisterAttached("MeshGeometry",
            typeof(AntilliMeshGeometry3D),
            typeof(GeometryModel3D),
            new PropertyMetadata(null, delegate (DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var geometry = (GeometryModel3D)d;
                var meshgeom = (AntilliMeshGeometry3D)e.NewValue;
                var oldgeom = (AntilliMeshGeometry3D)e.OldValue;

                if (meshgeom != oldgeom)
                {
                    if (meshgeom != null)
                    {
                        var model = meshgeom.Model;
                        var material = meshgeom.Material;

                        if (!float.IsNaN(meshgeom.TweenFactor))
                        {
                            model.TweenFactor = meshgeom.TweenFactor;

                            var texSlot = -1;

                            // apply damage textures?
                            if (meshgeom.TweenFactor >= AntilliMaterial.TweenFactorToApplyDamage)
                            {
                                texSlot = 1;
                            }
                            else
                            {
                                texSlot = 0;
                            }

                            material.TextureSlot = texSlot;
                        }

                        geometry.Geometry = model?.ToMesh();
                        geometry.Material = material?.Compile();

                        geometry.Material.SetOpacity(meshgeom.Opacity);

                        if (meshgeom.DoubleSided)
                            geometry.BackMaterial = geometry.Material;
                    }
                    else
                    {
                        // clear geometry
                        geometry.Geometry = null;
                        geometry.Material = null;
                        geometry.BackMaterial = null;
                    }
                }
            }));
    }

    public class AntilliMeshGeometry3D
    {
        public AntilliModel3D Model { get; }
        public AntilliMaterial Material { get; }

        public bool DoubleSided { get; set; }

        public double Opacity { get; set; }

        public float TweenFactor { get; set; }

        public AntilliMeshGeometry3D(AntilliModel3D model, AntilliMaterial material)
        {
            Model = model;
            Material = material;
            DoubleSided = false;
            Opacity = material.Opacity;
            TweenFactor = model.TweenFactor;
        }
    }

    public class AntilliGeometryVisual3D : MeshVisual3D
    {
        public static readonly DependencyProperty DoubleSidedProperty;
        public static readonly DependencyProperty OpacityProperty;

        public static readonly DependencyProperty GeometryProperty;

        static AntilliGeometryVisual3D()
        {
            Type thisType = typeof(AntilliGeometryVisual3D);

            DoubleSidedProperty = DependencyProperty.Register("DoubleSided",
                typeof(bool),
                thisType,
                new UIPropertyMetadata(true, DoubleSidedChanged));

            OpacityProperty = DependencyProperty.Register("Opacity",
                typeof(double),
                thisType,
                new UIPropertyMetadata(1.0, OpacityChanged));

            GeometryProperty = DependencyProperty.Register("Geometry",
                typeof(GeometryModel3D),
                thisType,
                new PropertyMetadata(null, GeometryChanged));
        }

        private static void DoubleSidedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((AntilliGeometryVisual3D)d).OnDoubleSidedChanged();

        protected static void OpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((AntilliGeometryVisual3D)d).OnOpacityChanged();

        protected static void GeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((AntilliGeometryVisual3D)d).OnGeometryChanged();

        public bool DoubleSided
        {
            get { return (bool)GetValue(DoubleSidedProperty); }
            set { SetValue(DoubleSidedProperty, value); }
        }

        public double Opacity
        {
            get { return (double)GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        public GeometryModel3D Geometry
        {
            get { return (GeometryModel3D)GetValue(GeometryProperty); }
            set { SetValue(GeometryProperty, value); }
        }

        protected virtual void OnDoubleSidedChanged()
        {
            if (Geometry != null)
                Geometry.SetValue(GeometryModel3DExtensions.DoubleSidedProperty, DoubleSided);
        }

        protected virtual void OnOpacityChanged()
        {
            if (Geometry != null)
                Geometry.SetValue(GeometryModel3DExtensions.OpacityProperty, Opacity);
        }

        protected virtual void OnGeometryChanged()
        {
            if (Geometry != null)
            {
                Geometry.SetValue(GeometryModel3DExtensions.OpacityProperty, Opacity);
                Geometry.SetValue(GeometryModel3DExtensions.DoubleSidedProperty, DoubleSided);

                Content = Geometry;
            }
            else
            {
                // no model present
                Content = null;
            }
        }
    }

    public class AntilliMeshVisual3D : AntilliGeometryVisual3D
    {
        public static readonly DependencyProperty TweenFactorProperty;
        
        public static readonly DependencyProperty MeshGeometryProperty;

        static AntilliMeshVisual3D()
        {
            Type thisType = typeof(AntilliMeshVisual3D);

            TweenFactorProperty = DependencyProperty.Register("TweenFactor",
                typeof(float),
                thisType,
                new UIPropertyMetadata(0.0f, TweenFactorChanged));

            MeshGeometryProperty = DependencyProperty.Register("MeshGeometry",
                typeof(AntilliMeshGeometry3D),
                thisType,
                new PropertyMetadata(null, MeshGeometryChanged));
        }

        protected static void TweenFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((AntilliMeshVisual3D)d).OnTweenFactorChanged();

        protected static void MeshGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((AntilliMeshVisual3D)d).OnMeshGeometryChanged();

        public float TweenFactor
        {
            get { return (float)GetValue(TweenFactorProperty); }
            set
            {
                var tweenFactor = value;

                if (!float.IsNaN(tweenFactor))
                {
                    if (tweenFactor < 0.0f)
                        tweenFactor = 0.0f;
                    else if (tweenFactor > 1.0f)
                        tweenFactor = 1.0f;
                }

                SetValue(TweenFactorProperty, tweenFactor);
            }
        }

        public AntilliMeshGeometry3D MeshGeometry
        {
            get { return (AntilliMeshGeometry3D)GetValue(MeshGeometryProperty); }
            set { SetValue(MeshGeometryProperty, value); }
        }

        protected virtual void OnTweenFactorChanged()
        {
            if (MeshGeometry != null)
            {
                MeshGeometry.TweenFactor = TweenFactor;

                if (Geometry != null)
                    Geometry.SetValue(GeometryModel3DExtensions.TweenFactorProperty, TweenFactor);
            }
        } 

        protected virtual void OnMeshGeometryChanged()
        {
            if (MeshGeometry != null)
            {
                Geometry = new GeometryModel3D();
                Geometry.SetValue(GeometryModel3DExtensions.MeshGeometryProperty, MeshGeometry);
            }
            else
            {
                Geometry = null;
            }
        }
    }

    public class SubModelVisual3D : AntilliMeshVisual3D
    {
        public static readonly DependencyProperty ModelProperty;

        static SubModelVisual3D()
        {
            Type thisType = typeof(SubModelVisual3D);

            ModelProperty = DependencyProperty.Register("Model",
                typeof(SubModel),
                thisType,
                new PropertyMetadata(null, ModelChanged));
        }

        public SubModel Model
        {
            get { return (SubModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        protected static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((SubModelVisual3D)d).OnModelChanged();

        protected virtual void OnModelChanged()
        {
            if (Model != null)
            {
                var indices = new List<int>();
                var vertices = Model.GetVertices(false, ref indices);

                var triangleIndices = new Int32Collection(indices);

                var model = new AntilliModel3D(vertices, triangleIndices)
                {
                    TweenFactor = TweenFactor
                };

                var material = new AntilliMaterial(Model.ModelPackage, Model.Material)
                {
                    Opacity = Opacity
                };

                MeshGeometry = new AntilliMeshGeometry3D(model, material)
                {
                    DoubleSided = DoubleSided
                };
            }
        }
    }

    public class LodModelVisual3D : MeshVisual3D
    {
        public static readonly DependencyProperty UseBlendWeightsProperty;
        public static readonly DependencyProperty TweenFactorProperty;

        public static readonly DependencyProperty ModelProperty;
        public static readonly DependencyProperty LodProperty;

        static LodModelVisual3D()
        {
            Type thisType = typeof(LodModelVisual3D);

            UseBlendWeightsProperty = DependencyProperty.Register("UseBlendWeights",
                typeof(bool),
                thisType,
                new UIPropertyMetadata(false, UseBlendWeightsChanged));

            TweenFactorProperty = DependencyProperty.Register("TweenFactor",
                typeof(float),
                thisType,
                new UIPropertyMetadata(float.NaN, TweenFactorChanged));

            LodProperty = DependencyProperty.Register("Lod",
                typeof(int),
                thisType,
                new UIPropertyMetadata(0, LodChanged));

            ModelProperty = DependencyProperty.Register("Model",
                typeof(Model),
                thisType,
                new PropertyMetadata(null, ModelChanged));
        }

        private List<SubModelVisual3D> m_subModels = new List<SubModelVisual3D>();
        private Dictionary<SubModelVisual3D, ModelVisual3D> m_parents = new Dictionary<SubModelVisual3D, ModelVisual3D>();

        private static void UseBlendWeightsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((LodModelVisual3D)d).OnUseBlendWeightsChanged((bool)e.OldValue);

        protected static void TweenFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((LodModelVisual3D)d).OnTweenFactorChanged((float)e.OldValue);

        private static void LodChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((LodModelVisual3D)d).OnLodChanged((int)e.OldValue);

        private static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((LodModelVisual3D)d).OnModelChanged((Model)e.OldValue);

        public IEnumerable<SubModelVisual3D> SubModels
        {
            get { return m_subModels; }
        }

        public bool UseBlendWeights
        {
            get { return (bool)GetValue(UseBlendWeightsProperty); }
            set { SetValue(UseBlendWeightsProperty, value); }
        }

        public float TweenFactor
        {
            get { return (float)GetValue(TweenFactorProperty); }
            set
            {
                var tweenFactor = value;

                if (!float.IsNaN(tweenFactor))
                {
                    if (tweenFactor < 0.0f)
                        tweenFactor = 0.0f;
                    else if (tweenFactor > 1.0f)
                        tweenFactor = 1.0f;
                }

                SetValue(TweenFactorProperty, tweenFactor);
            }
        }

        public int Lod
        {
            get { return (int)GetValue(LodProperty); }
            set { SetValue(LodProperty, value); }
        }

        public Model Model
        {
            get { return (Model)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        protected virtual void OnUseBlendWeightsChanged(bool oldValue)
        {
            if (UseBlendWeights != oldValue)
            {
                // apply tweening only if it was possibly tweened before
                if (!float.IsNaN(TweenFactor))
                    TweenSubModels();
            }
        }

        protected virtual void OnTweenFactorChanged(float oldValue)
        {
            var needsTween = true;

            if (!float.IsNaN(TweenFactor) && !float.IsNaN(oldValue))
            {
                if (TweenFactor == oldValue)
                    needsTween = false;
            }
            else
            {
                // no tween needed?
                if (float.IsNaN(TweenFactor) && float.IsNaN(oldValue))
                    needsTween = false;
            }

            if (needsTween)
                TweenSubModels();
        }

        protected virtual void OnLodChanged(int oldLod)
        {
            if (Lod != oldLod)
                BuildSubModels();
        }

        protected virtual void OnModelChanged(Model oldModel)
        {
            if (Model != oldModel)
                BuildSubModels();
        }

        protected virtual void TweenSubModels()
        {
            if (UseBlendWeights)
            {
                foreach (SubModelVisual3D submodel in SubModels)
                    submodel.TweenFactor = TweenFactor;
            }
            else
            {
                foreach (SubModelVisual3D submodel in SubModels)
                    submodel.TweenFactor = float.NaN;

                SetCurrentValue(TweenFactorProperty, float.NaN);
            }
        }

        protected void AddLodInstances(Lod lod, bool allowTransforms)
        {
            var instances = lod.Instances;

            if (instances != null)
            {
                foreach (var instance in instances)
                {
                    if (instance.SubModels == null)
                        continue;

                    var transform = instance.Transform;
                    var transpose = transform.GetTranspose();

                    var m1 = transpose[0];
                    var m2 = transpose[1];
                    var m3 = transpose[2];
                    var m4 = transpose[3];

                    var mtx1 = new Matrix3D()
                    {
                        M11 = m1.X,
                        M12 = m1.Y,
                        M13 = m1.Z,
                        M14 = m1.W,

                        M21 = m2.X,
                        M22 = m2.Y,
                        M23 = m2.Z,
                        M24 = m2.W,

                        M31 = m3.X,
                        M32 = m3.Y,
                        M33 = m3.Z,
                        M34 = m3.W,

                        OffsetX = m4.X,
                        OffsetY = m4.Y,
                        OffsetZ = m4.Z,

                        M44 = m4.W,
                    };

                    var mtx = (allowTransforms && instance.UseTransform) ? mtx1 : Matrix3D.Identity;

                    mtx.Append(new Matrix3D(
                        -1, 0, 0, 0,
                         0, 0, 1, 0,
                         0, 1, 0, 0,
                         0, 0, 0, 1));

                    foreach (var submodel in instance.SubModels)
                    {
                        var vis3d = new SubModelVisual3D()
                        {
                            Model = submodel
                        };

                        if (UseBlendWeights)
                            vis3d.TweenFactor = TweenFactor;

                        vis3d.Transform = new MatrixTransform3D(mtx);

                        m_subModels.Add(vis3d);
                    }
                }
            }
        }

        protected virtual void BuildSubModels()
        {
            m_subModels.Clear();

            if (Model != null)
            {
                var isVehicle = Model.VertexType == 5;

                if (isVehicle || Lod > 0)
                {
                    var lod = Model.Lods[Lod];
                    var allowTransform = !isVehicle;

                    if (lod != null)
                        AddLodInstances(lod, allowTransform);
                }
                else
                {
                    for (int i = Lod; i < 4; i++)
                    {
                        var lod = Model.Lods[i];

                        if (lod != null)
                            AddLodInstances(lod, true);
                    }
                }
            }
        }

        public void AddSubModels(Func<SubModelVisual3D, ModelVisual3D> fnGetVisualParent)
        {
            foreach (var submodel in SubModels)
            {
                // null means it's our own child
                var parent = fnGetVisualParent(submodel) ?? this;

                m_parents.Add(submodel, parent);
                parent.Children.Add(submodel);
            }
        }

        public void RemoveSubModels()
        {
            foreach (var submodel in SubModels)
            {
                if (m_parents.ContainsKey(submodel))
                {
                    var parent = m_parents[submodel];

                    parent.Children.Remove(submodel);
                    m_parents.Remove(submodel);
                }
            }
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

            if (material.UID == 0xCCCC || Model.LodInstance.Parent.Type >= 5)
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

                    var eFlags = (SubstanceExtraFlags)substance.TextureFlags;

                    var alpha = substance.HasAlpha;
                    var emissive = substance.IsEmissive;
                    var specular = substance.IsSpecular;

                    var texIdx = 0;

                    if (UseBlendWeights)
                    {
                        if (eFlags.HasFlag(SubstanceExtraFlags.DamageAndColorMask_AlphaMaps))
                        {
                            // Driv3r damage is at 2,3
                            texIdx = 2;
                        }
                        else if ((eFlags & (SubstanceExtraFlags.DPL_Damage)) != 0)
                        {
                            // DPL damage is at 1
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
