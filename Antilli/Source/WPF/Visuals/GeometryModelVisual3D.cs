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
    public class GeometryModelVisual3D : ModelVisual3D
    {
        public static readonly DependencyProperty DoubleSidedProperty;
        public static readonly DependencyProperty MaterialProperty;
        public static readonly DependencyProperty TriangleIndicesProperty;
        public static readonly DependencyProperty VerticesProperty;

        static GeometryModelVisual3D()
        {
            Type thisType = typeof(GeometryModelVisual3D);

            VerticesProperty =
                DependencyProperty.Register("Vertices", typeof(List<Vertex>), thisType,
                new PropertyMetadata(null, GeometryChanged));
            TriangleIndicesProperty =
                DependencyProperty.Register("TriangleIndices", typeof(Int32Collection), thisType,
                new PropertyMetadata(null, GeometryChanged));
            MaterialProperty =
                DependencyProperty.Register("Material", typeof(Material), thisType,
                new UIPropertyMetadata(null, MaterialChanged));
            DoubleSidedProperty =
                DependencyProperty.Register("DoubleSided", typeof(bool), thisType,
                new UIPropertyMetadata(false, DoubleSidedChanged));
        }

        public bool DoubleSided
        {
            get { return (bool)GetValue(DoubleSidedProperty); }
            set { SetValue(DoubleSidedProperty, value); }
        }

        public Material Material
        {
            get { return (Material)GetValue(MaterialProperty); }
            set { SetValue(MaterialProperty, value); }
        }

        public GeometryModel3D Model
        {
            get { return this.Content as GeometryModel3D; }
        }

        public Int32Collection TriangleIndices
        {
            get { return (Int32Collection)GetValue(TriangleIndicesProperty); }
            set { SetValue(TriangleIndicesProperty, value); }
        }

        public List<Vertex> Vertices
        {
            get { return (List<Vertex>)GetValue(VerticesProperty); }
            set { SetValue(VerticesProperty, value); }
        }

        protected static void GeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GeometryModelVisual3D)d).OnGeometryChanged();
        }

        protected static void MaterialChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GeometryModelVisual3D)d).OnMaterialChanged();
        }

        protected static void DoubleSidedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GeometryModelVisual3D)d).OnDoubleSidedChanged();
        }

        protected virtual void OnGeometryChanged()
        {
            Model.Geometry = ToMesh();
        }

        protected virtual void OnMaterialChanged()
        {
            Model.Material = Material;
            Model.BackMaterial = (DoubleSided) ? Material : null;
        }

        protected virtual void OnDoubleSidedChanged()
        {
            if (Material != null)
                OnMaterialChanged();
        }

        public void UpdateModel()
        {
            OnGeometryChanged();
            OnMaterialChanged();
        }

        protected virtual MeshGeometry3D ToMesh()
        {
            if (Vertices == null || TriangleIndices == null)
                return null;

            int nVerts = Vertices.Count;

            Point3DCollection positions        = new Point3DCollection(nVerts);
            Vector3DCollection normals         = new Vector3DCollection(nVerts);
            PointCollection textureCoordinates = new PointCollection(nVerts);

            foreach (Vertex vertex in Vertices)
            {
                positions.Add(vertex.Position);
                normals.Add(vertex.Normal);
                textureCoordinates.Add(vertex.UV);
            }

            return new MeshGeometry3D() {
                Positions = positions,
                Normals = normals,
                TextureCoordinates = textureCoordinates,
                TriangleIndices = TriangleIndices
            };
        }

        public GeometryModelVisual3D()
        {
            this.Content = new GeometryModel3D();
        }

        public GeometryModelVisual3D(int numVerts, int numTris) : this()
        {
            Vertices = new List<Vertex>(numVerts);
            TriangleIndices = new Int32Collection(numTris);
        }

        public GeometryModelVisual3D(List<Vertex> vertices, Int32Collection triangleIndices) : this()
        {
            Vertices = vertices;
            TriangleIndices = triangleIndices;
        }
    }
}
