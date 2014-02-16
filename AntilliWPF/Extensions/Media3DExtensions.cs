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

        public static void SetOpacity(this Model3D @this, double opacity, bool doubleSided=false)
        {
            if (@this is GeometryModel3D)
            {
                GeometryModel3D geom = (GeometryModel3D)@this;

                geom.Material.SetOpacity(opacity);

                if (doubleSided)
                    geom.BackMaterial.SetOpacity(opacity);
            }
            else if (@this is Model3DGroup)
            {
                foreach (Model3D groupModel in ((Model3DGroup)@this).Children)
                    groupModel.SetOpacity(opacity);
            }
        }

        public static void SetOpacity(this DriverModelVisual3D @this, double opacity)
        {
            @this.BaseMaterial.SetOpacity(opacity);
            @this.UpdateModel();
        }

        public static void SetOpacity(this DriverModelGroup @this, double opacity)
        {
            if (@this.Models.Count == 0)
                return;

            foreach (DriverModelVisual3D dmodel in @this.Models)
                dmodel.SetOpacity(opacity);
        }

        public static void SelectModel3D(this Visual3D @this, Model3D selectedModel, double opacity)
        {
            if (@this is ModelVisual3D)
            {
                ModelVisual3D mv3d = @this as ModelVisual3D;

                if (mv3d.Content != null)
                    mv3d.Content.SetOpacity((mv3d.Content != selectedModel) ? opacity : 1.0);

                if (mv3d.Children.Count > 0)
                    foreach (Visual3D v3d in mv3d.Children)
                        v3d.SelectModel3D(selectedModel, opacity);
            }
        }
    }
}
