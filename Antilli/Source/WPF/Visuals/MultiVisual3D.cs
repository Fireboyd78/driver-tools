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
using System.Windows.Markup;
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
    public abstract class CompositeModelVisual3D<TModel> : ModelVisual3D, IAddChild
        where TModel : Model3D
    {
        public new static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content",
            typeof(TModel),
            typeof(CompositeModelVisual3D<TModel>),
            new PropertyMetadata(ContentPropertyChanged));

        public new static readonly DependencyProperty TransformProperty = ModelVisual3D.TransformProperty;

        private static void ContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var visual = (CompositeModelVisual3D<TModel>)d;

            var r = new Reflector("System.Windows");
            var isSubChange = (bool)r.GetAs(typeof(DependencyPropertyChangedEventArgs), e, "IsASubPropertyChange");

            if (!isSubChange)
            {
                var newModel = (TModel)e.NewValue;
                var oldModel = (TModel)e.OldValue;

                if (oldModel != null)
                    visual.OnModelContentDecomposing(oldModel);

                visual.OnModelContentComposing(newModel);
                visual.OnModelContentComposed(newModel, oldModel);
            }
        }

        public new TModel Content
        {
            get { return (TModel)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public new Transform3D Transform
        {
            get { return (Transform3D)GetValue(TransformProperty); }
            set { SetValue(TransformProperty, value); }
        }

        void IAddChild.AddChild(object value)
        {
            var visual = value as Visual3D;
            if (visual != null)
            {
                OnVisualComposing(visual);
                Children.Add(visual);
            }
        }

        protected virtual void OnModelContentComposing(TModel model)
        {
        }

        protected virtual void OnModelContentComposed(TModel composedModel, TModel decomposedModel)
        {
            Visual3DModel = composedModel;
            SetCurrentValue(ModelVisual3D.ContentProperty, composedModel);
        }

        protected virtual void OnModelContentDecomposing(TModel model)
        {
        }

        protected virtual void OnVisualComposing(Visual3D visual)
        {
        }

        protected virtual void OnVisualDecomposing(Visual3D visual)
        {
        }

        protected virtual void OnVisualAdded(Visual3D visual)
        {

        }

        protected virtual void OnVisualRemoved(Visual3D visual)
        {
            
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            if (visualAdded != null)
            {
                var visual = (Visual3D)visualAdded;
                OnVisualAdded(visual);
            }
            else if (visualRemoved != null)
            {
                var visual = (Visual3D)visualRemoved;

                OnVisualDecomposing(visual);
                OnVisualRemoved(visual);
            }

            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }

        public CompositeModelVisual3D()
            : base()
        { }
    }

    public class CompositeTransformGeometryVisual3D : CompositeModelVisual3D<GeometryModel3D>
    {
        public new static readonly DependencyProperty ContentProperty = CompositeModelVisual3D<GeometryModel3D>.ContentProperty;
        public new static readonly DependencyProperty TransformProperty = CompositeModelVisual3D<GeometryModel3D>.TransformProperty;

        private Dictionary<GeometryModel3D, Transform3DGroup> _transforms;

        public new GeometryModel3D Content
        {
            get { return (GeometryModel3D)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public new Transform3D Transform
        {
            get { return (Transform3D)GetValue(TransformProperty); }
            set { SetValue(TransformProperty, value); }
        }

        protected override void OnModelContentComposing(GeometryModel3D model)
        {
            var transform = new Transform3DGroup();

            transform.Children.Add(Transform);
            transform.Children.Add(model.Transform);

            _transforms.Add(model, transform);

            model.Transform = transform;

            base.OnModelContentComposing(model);
        }

        protected override void OnModelContentDecomposing(GeometryModel3D model)
        {
            if (_transforms.ContainsKey(model))
            {
                var transform = _transforms[model];
                var oldTransform = transform.Children.Last();

                transform.Children.Remove(oldTransform);
                transform.Children.Remove(Transform);
                transform.Children.Clear();

                model.Transform = oldTransform;
            }

            base.OnModelContentDecomposing(model);
        }

        public CompositeTransformGeometryVisual3D()
            : base()
        {
            _transforms = new Dictionary<GeometryModel3D, Transform3DGroup>();
        }
    }
}
