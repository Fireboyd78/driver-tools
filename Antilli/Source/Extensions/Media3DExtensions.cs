using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HelixToolkit.Wpf;

using Antilli;

namespace System.Windows.Media.Media3D
{
    public static class Media3DExtensions
    {
        private static int RandomMaterialSeed;

        static Media3DExtensions()
        {
            GenerateSeed();
        }

        private static void GenerateSeed()
        {
            int random = DateTime.Now.Millisecond;
            RandomMaterialSeed = new Random(random).Next(0, new Random(random * (int)new Random(random).Next(0, (int)new Random(random).Next())).Next(random, int.MaxValue));
        }

        public static void Randomize(this Material @this)
        {
            if (@this == null)
                @this = new DiffuseMaterial();

            SolidColorBrush brush = new SolidColorBrush() {
                Color = Color.FromArgb(
                    255,
                    (byte)new Random(RandomMaterialSeed * 1564145 * 573 / 31).Next(0, 255),
                    (byte)new Random(RandomMaterialSeed * (35645 * 29485 / 71) / 25).Next(0, 255),
                    (byte)new Random(RandomMaterialSeed * (755 * 34157 / 33) / 10).Next(0, 255)
                )
            };

            if (@this is MaterialGroup)
            {
                foreach (Material mat in ((MaterialGroup)@this).Children)
                    Randomize(mat);
                return;
            }

            if (@this is DiffuseMaterial)
                ((DiffuseMaterial)@this).Brush = brush;
            else if (@this is SpecularMaterial)
                ((SpecularMaterial)@this).Brush = brush;
            else if (@this is EmissiveMaterial)
                ((EmissiveMaterial)@this).Brush = brush;

            GenerateSeed();
        }

        public static void RandomizeColors(this GeometryModel3D @this)
        {
            if (@this.Material == null)
                @this.Material = new DiffuseMaterial();

            @this.Material.Randomize();
        }

        public static void SetOpacity(this Material @this, double opacity)
        {
            if (@this is MaterialGroup)
            {
                foreach (Material mat in ((MaterialGroup)@this).Children)
                    SetOpacity(mat, opacity);
                return;
            }

            if (@this is DiffuseMaterial)
                ((DiffuseMaterial)@this).Brush.Opacity = opacity;
            else if (@this is SpecularMaterial)
                ((SpecularMaterial)@this).Brush.Opacity = opacity;
            else if (@this is EmissiveMaterial)
                ((EmissiveMaterial)@this).Brush.Opacity = opacity;
        }

        public static void SetOpacity(this Model3D @this, double opacity)
        {
            if (@this is GeometryModel3D)
            {
                var geom = @this as GeometryModel3D;

                geom.Material.SetOpacity(opacity);

                if (geom.BackMaterial != null)
                    geom.BackMaterial.SetOpacity(opacity);
            }
            else if (@this is Model3DGroup)
            {
                foreach (Model3D groupModel in ((Model3DGroup)@this).Children)
                    groupModel.SetOpacity(opacity);
            }
        }

        public static void SetOpacity(this ModelVisual3D @this, double opacity)
        {
            @this.Content.SetOpacity(opacity);
        }

        public static void SetOpacity(this DriverModelVisual3D @this, double opacity)
        {
            @this.BaseMaterial.SetOpacity(opacity);
        }
        public static void SetOpacity(this AntilliModelVisual3D @this, double opacity)
        {
            @this.Opacity = opacity;
        }

        public static void SetOpacity(this ModelVisual3DGroup @this, double opacity)
        {
            if (@this.Children.Count == 0)
                return;

            foreach (ModelVisual3D dmodel in @this.Children)
                dmodel.SetOpacity(opacity);
        }

        public static GeometryModel3D ToGeometry(this MeshGeometry3D @this)
        {
            return @this.ToGeometry(null, false);
        }

        public static GeometryModel3D ToGeometry(this MeshGeometry3D @this, Material material)
        {
            return @this.ToGeometry(material, false);
        }

        public static GeometryModel3D ToGeometry(this MeshGeometry3D @this, Material material, bool doubleSided)
        {
            return new GeometryModel3D() {
                Geometry = @this,
                Material = material,
                BackMaterial = (doubleSided) ? material : null
            };
        }
    }
}
