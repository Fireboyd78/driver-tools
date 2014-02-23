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
    public class BlendModelVisual3D : GeometryModelVisual3D
    {
        static double tweenFactor = 1.0;

        public static readonly DependencyProperty UseBlendWeightsProperty;

        static BlendModelVisual3D()
        {
            Type thisType = typeof(BlendModelVisual3D);

            UseBlendWeightsProperty =
            DependencyProperty.Register("UseBlendWeights", typeof(bool), thisType,
            new UIPropertyMetadata(false, BlendWeightsChanged));
        }

        public bool UseBlendWeights
        {
            get { return (bool)GetValue(UseBlendWeightsProperty); }
            set { SetValue(UseBlendWeightsProperty, value); }
        }

        protected static void BlendWeightsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BlendModelVisual3D)d).OnBlendWeightsChanged();
        }

        protected virtual void OnBlendWeightsChanged()
        {
            if (Model != null)
                OnGeometryChanged();
        }

        protected override MeshGeometry3D ToMesh()
        {
            if (Vertices == null || TriangleIndices == null)
                return null;

            int nVerts = Vertices.Count;

            Point3DCollection positions        = new Point3DCollection(nVerts);
            Vector3DCollection normals         = new Vector3DCollection(nVerts);
            PointCollection textureCoordinates = new PointCollection(nVerts);

            foreach (Vertex vertex in Vertices)
            {
                Point3D pos = (UseBlendWeights) ? Vertex.Tween(vertex.Position, vertex.BlendWeights, tweenFactor) : vertex.Position;

                positions.Add(pos);
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

        public BlendModelVisual3D()
            : base()
        {
        }

        public BlendModelVisual3D(int numVertices, int numTris)
            : base(numVertices, numTris)
        {
        }

        public BlendModelVisual3D(List<Vertex> vertices, Int32Collection triangleIndices)
            : base(vertices, triangleIndices)
        {
        }
    }
}