using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using HelixToolkit.Wpf;

using FreeImageAPI;

using DSCript;
using DSCript.Models;
using DSCript.Spooling;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for ModelViewer.xaml
    /// </summary>
    public partial class ModelViewer : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        protected bool SetValue<T>(ref T backingField, T value, string propertyName)
        {
            if (object.Equals(backingField, value))
            {
                return false;
            }

            backingField = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        private ModelVisual3DGroup _selectedModel;
        private List<ModelVisual3DGroup> _visuals;
        private List<AntilliModelVisual3D> _models;

        private bool _applyTransforms;
        private bool _useBlendWeights;
        private int m_lod;
        
        public int LevelOfDetail
        {
            get { return m_lod; }
            set
            {
                var oldLod = m_lod;
                m_lod = value;

                OnLevelOfDetailChanged(oldLod);
            }
        }

        public bool ApplyTransforms
        {
            get { return _applyTransforms; }
            set
            {
                _applyTransforms = value;
                UpdateActiveModel();
            }
        }

        public bool UseBlendWeights
        {
            get { return _useBlendWeights; }
            set
            {
                _useBlendWeights = value;

                if (Visuals == null)
                    return;

                var selectedModel   = SelectedModel;
                var item            = ModelsList.GetSelectedContainer();

                foreach (var visual in Visuals)
                {
                    foreach (AntilliModelVisual3D dmodel in visual.Children)
                        dmodel.UseBlendWeights = _useBlendWeights;
                }

                if (selectedModel != null)
                    SelectedModel = selectedModel;
                if (item != null)
                    item.IsSelected = true;
            }
        }

        public List<ModelVisual3DGroup> Visuals
        {
            get { return _visuals; }
            set
            {
                if (SetValue(ref _visuals, value, "Visuals"))
                {
                    if (_models != null)
                    {
                        foreach (var model in _models)
                            model.ClearValue(AntilliModelVisual3D.ModelProperty);

                        _models.Clear();
                        _models = null;
                    }

                    OnPropertyChanged("Elements");
                }
            }
        }

        public List<ModelListItem> Elements
        {
            get
            {
                if (Visuals == null)
                    return null;

                var models = new List<ModelListItem>();

                foreach (var dg in Visuals)
                {
                    if (dg.Children.Count > 1)
                        models.Add(new ModelVisual3DGroupListItem(models, dg));
                    else
                        models.Add(new ModelListItem(models, dg));
                }

                return models;
            }
        }

        public ModelVisual3DGroup SelectedModel
        {
            get { return _selectedModel; }
            set
            {
                _selectedModel = value;

                if (Visuals != null)
                {
                    foreach (var visual in Visuals)
                    {
                        if (SelectedModel != null && SelectedModel != visual)
                        {
                            visual.SetOpacity(Settings.GhostOpacity);

                            foreach (var model in visual.Children)
                                VisualParentHelper.SetParent(model, TopmostLayer);
                        }
                        else
                            visual.SetOpacity(1.0);
                    }
                }

                if (SelectedModel == null)
                    OnModelDeselected();
            }
        }

        public void ClearModels()
        {
            if (SelectedModel != null)
            {
                RestoreVisualParents();
                SelectedModel = null;
            }

            VisualsLayer.Children.Clear();
            EmissiveLayer.Children.Clear();
            TransparencyLayer.Children.Clear();
            TopmostLayer.Children.Clear();

            Visuals = null;
        }

        public void UpdateModels()
        {
            OnPropertyChanged("Visuals");
            OnPropertyChanged("Elements");
        }

        public void RestoreVisualParents()
        {
            if (VisualParentHelper.ResetAllParents())
                TopmostLayer.Children.Clear();
        }

        public void OnModelDeselected()
        {
            var item = ModelsList.GetSelectedContainer();

            if (item != null)
                item.IsSelected = false;
        }

        private void OnModelSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = ModelsList.SelectedItem;

            RestoreVisualParents();

            if (item is ModelListItem)
            {
                SelectedModel = ((ModelListItem)item).Model;
            }
            else if (item is ModelVisual3D)
            {
                if (SelectedModel != null)
                    SelectedModel = null;

                foreach (var visual in Visuals)
                {
                    foreach (var model in visual.Children)
                    {
                        if (model != (ModelVisual3D)item)
                        {
                            model.SetOpacity(Settings.GhostOpacity);
                            VisualParentHelper.SetParent(model, TopmostLayer);
                        }
                        else
                            model.SetOpacity(1.0);
                    }
                }
            }
            else
            {
                SelectedModel = null;
            }

            if (SelectedModel != null)
            {
                var dmodel = SelectedModel.Children[0] as AntilliModelVisual3D;

                if (dmodel != null)
                {
                    var subModel = dmodel.Model;
                    var instance = subModel.LodInstance;
                    var lod = instance.Parent;
                    var model = lod.Parent;

                    var bbox = model.BoundingBox;

                    DSC.Log($"{model.Flags} {lod.ID} {lod.Mask}");
                    DSC.Log($"{model.Scale}");

                    DSC.Log($"{bbox.V11}\n\t{bbox.V12}\n\t{bbox.V13}\n\t{bbox.V14}");
                    DSC.Log($"{bbox.V21}\n\t{bbox.V22}\n\t{bbox.V23}\n\t{bbox.V24}");

                    DSC.Log($"{instance.Transform[0]}\n\t{instance.Transform[1]}\n\t{instance.Transform[2]}\n\t{instance.Transform[3]}");
                }
            }
        }

        private void ViewModelTexture(object sender, RoutedEventArgs e)
        {
            var material = ((MenuItem)e.Source).Tag as IMaterialData;

            if (material == null)
            {
                MessageBox.Show("No texture assigned!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var substance = material.GetSubstance(0);
            var texIdx = 0;

            // HACK: make sure we can retrieve the damage texture!
            if (substance is ISubstanceDataPC)
            {
                var substance_pc = (substance as SubstanceDataPC);
                var mayHaveDamage = (substance_pc.Textures.Count >= 4);

                texIdx = (UseBlendWeights && mayHaveDamage) ? 2 : 0;
            }

            AT.CurrentState.QueryTextureSelect(substance.GetTexture(texIdx));
        }

        private void ViewModelMaterial(object sender, RoutedEventArgs e)
        {
            var material = ((MenuItem)e.Source).Tag as IMaterialData;

            if (material == null)
            {
                MessageBox.Show("No material assigned!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AT.CurrentState.QueryMaterialSelect(material);
        }

        List<Model> m_partsGroups;

        public void RemoveActiveModel()
        {
            ClearModels();

            if (m_partsGroups != null)
                m_partsGroups = null;
        }

        public bool UpdateActiveModel()
        {
            ClearModels();

            // no active model present
            if (m_partsGroups == null)
                return true;

            var models = new List<ModelVisual3DGroup>();

            foreach (var part in m_partsGroups)
            {
                var partDef = part.Lods[LevelOfDetail];

                if (partDef.Instances == null)
                    continue;

                var meshes = new ModelVisual3DGroup();

                foreach (var group in partDef.Instances)
                {
                    var m1 = group.Transform[0];
                    var m2 = group.Transform[1];
                    var m3 = group.Transform[2];
                    var m4 = group.Transform[3];

                    var mtx = new Matrix3D() {
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

                    foreach (var mesh in group.SubModels)
                    {
                        var vis3d = new AntilliModelVisual3D(mesh, UseBlendWeights);

                        if (ApplyTransforms)
                            vis3d.Transform = new MatrixTransform3D(mtx);

                        meshes.Children.Add(vis3d);
                    }
                }

                if (meshes.Children.Count > 0)
                    models.Add(meshes);
            }

            if (models.Count > 0)
            {
                // do this first, otherwise shit will get fucked up
                Visuals = models;

                _models = new List<AntilliModelVisual3D>();

                // set the new model
                foreach (var model in models)
                {
                    foreach (AntilliModelVisual3D dmodel in model.Children)
                    {
                        if (dmodel.IsEmissive)
                            EmissiveLayer.Children.Add(dmodel);
                        else if (dmodel.HasTransparency)
                            TransparencyLayer.Children.Add(dmodel);
                        else
                            VisualsLayer.Children.Add(dmodel);

                        _models.Add(dmodel);
                    }
                }

                
                return true;
            }
            else
            {
                Viewport.SetDebugInfo("Level of detail contains no valid models.");
                return false;
            }
        }

        public bool SetActiveModel(List<Model> partsGroups)
        {
            m_partsGroups = partsGroups;
            return UpdateActiveModel();
        }

        public void ToggleTransforms()
        {
            ApplyTransforms = !ApplyTransforms;
        }
        
        public void ToggleBlendWeights()
        {
            UseBlendWeights = !UseBlendWeights;
        }

        public void ToggleInfiniteSpin()
        {
            Viewport.InfiniteSpin = !Viewport.InfiniteSpin;
        }

        public void ToggleDebugMode()
        {
            Viewport.DebugMode = !Viewport.DebugMode;
        }

        public void ToggleCameraMode()
        {
            if (Viewport.CameraMode == CameraMode.Inspect)
            {
                Viewport.CameraMode = CameraMode.WalkAround;
                Viewport.CameraInertiaFactor = 0.15;
            }
            else if (Viewport.CameraMode == CameraMode.WalkAround)
            {
                Viewport.CameraMode = CameraMode.FixedPosition;
                Viewport.CameraInertiaFactor = 0.93;
            }
            else
            {
                Viewport.CameraMode = CameraMode.Inspect;
                Viewport.CameraInertiaFactor = 0.93;
            }
        }

        public void OnKeyPressed(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
            case Key.I:
                ToggleInfiniteSpin();
                break;
            case Key.G:
                ToggleDebugMode();
                break;
            case Key.C:
                ToggleCameraMode();
                break;
            }
        }

        private void OnLevelOfDetailChanged(int oldLod)
        {
            if (oldLod != m_lod)
                UpdateActiveModel();
        }

        public ModelViewer()
        {
            InitializeComponent();
            
            DeselectModel.Click += (o, e) => {
                SelectedModel = null;
            };

            Viewport.Loaded += (o, e) => {
                VCam.FieldOfView = Settings.DefaultFOV;
            };
        }
    }
}
