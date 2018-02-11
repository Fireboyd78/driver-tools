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
                    foreach (DriverModelVisual3D dmodel in visual.Children)
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

        List<PartsGroup> m_partsGroups;

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
                var partDef = part.Parts[LevelOfDetail];

                if (partDef.Groups == null)
                    continue;

                var meshes = new ModelVisual3DGroup();

                foreach (var group in partDef.Groups)
                {
                    foreach (var mesh in group.Meshes)
                        meshes.Children.Add(new DriverModelVisual3D(mesh, UseBlendWeights));
                }

                if (meshes.Children.Count > 0)
                    models.Add(meshes);
            }

            if (models.Count > 0)
            {
                // set the new model
                foreach (var model in models)
                {
                    foreach (DriverModelVisual3D dmodel in model.Children)
                    {
                        if (dmodel.IsEmissive)
                            EmissiveLayer.Children.Add(dmodel);
                        else if (dmodel.HasTransparency)
                            TransparencyLayer.Children.Add(dmodel);
                        else
                            VisualsLayer.Children.Add(dmodel);
                    }
                }

                Visuals = models;
                return true;
            }
            else
            {
                Viewport.SetDebugInfo("Level of detail contains no valid models.");
                return false;
            }
        }

        public bool SetActiveModel(List<PartsGroup> partsGroups)
        {
            m_partsGroups = partsGroups;
            return UpdateActiveModel();
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
