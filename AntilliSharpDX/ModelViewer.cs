using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HelixToolkit.Wpf.SharpDX;
using SharpDX;

using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

//using DSCript;

namespace Antilli
{
    public static class Settings
    {
        //public static readonly IniConfiguration Configuration = new IniConfiguration("Antilli");
    }

    public class ModelViewer : ObservableObject
    {
        #region Title
        private const string titleFormat = "{0} - {1}";

        private string title;
        private string subtitle;

        public string Title
        {
            get { return (!String.IsNullOrEmpty(subtitle)) ? String.Format(titleFormat, title, subtitle) : this.title; }
            set { this.SetValue(ref title, value, "Title"); }
        }

        public string SubTitle
        {
            get { return this.subtitle; }
            set { this.SetValue(ref subtitle, value, "Title"); }
        }
        #endregion Title

        #region Viewport Properties
        private Camera camera;
        private RenderTechnique renderTechnique;

        private Color4 ambientLightColor;
        private Color4 directionalLightColor;

        private PerspectiveCamera defaultPerspectiveCamera = new PerspectiveCamera() {
            Position            = new Point3D(-4, 3.5, -4),
            LookDirection       = new Vector3D(4, -3.5, 4),
            UpDirection         = new Vector3D(0, 1, 0),
            NearPlaneDistance   = 0.125,
            FarPlaneDistance    = 150000
        };

        public Color4 AmbientLightColor
        {
            get { return this.ambientLightColor; }
            set { this.SetValue(ref ambientLightColor, value, "AmbientLightColor"); }
        }

        public Color4 DirectionalLightColor
        {
            get { return this.directionalLightColor; }
            set { this.SetValue(ref directionalLightColor, value, "DirectionalLightColor"); }
        }

        public Camera Camera
        {
            get { return this.camera; }
            set { this.SetValue(ref camera, value, "Camera"); }
        }

        public RenderTechnique RenderTechnique
        {
            get { return this.renderTechnique; }
            set { this.SetValue(ref renderTechnique, value, "RenderTechnique"); }
        }

        protected PerspectiveCamera DefaultPerspectiveCamera
        {
            get { return defaultPerspectiveCamera; }
        }
        #endregion Viewport Properties

        private ObservableElement3DCollection models;

        public ObservableElement3DCollection Models
        {
            get { return models; }
            protected set { this.SetValue(ref models, value, "Models"); }
        }

        public LineGeometry3D Grid { get; set; }

        public Color GridColor
        {
            get { return new Color(64, 64, 64, 64); }
        }

        public Color4 BackgroundColor
        {
            get { return new Color4(0, 0, 0, 0); }
        }

        public ModelViewer()
        {
            this.Title = "Antilli";

            this.Camera = DefaultPerspectiveCamera;
            this.RenderTechnique = Techniques.RenderBlinn;
            
            this.AmbientLightColor = new Color4(0x404040);
            this.DirectionalLightColor = new Color4(0x707070);

            this.Grid = LineBuilder.GenerateGrid();

            this.Models = new ObservableElement3DCollection();

            var box = new MeshBuilder();
            box.AddBox(new Vector3(0, 0, 0), 1, 1, 1);

            var mesh = box.ToMeshGeometry3D();

            var geom = new MeshGeometryModel3D() {
                Geometry = mesh,
                Material = PhongMaterials.Red,
                Transform = new Media3D.TranslateTransform3D(0, 0, 0)
            };

            Models.Add(geom);
        }
    }
}
