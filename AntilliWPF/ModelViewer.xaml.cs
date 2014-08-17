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

        public MainWindow MainWindow { get; set; }

        public double GhostOpacity
        {
            get { return Settings.Configuration.GetSetting<double>("GhostOpacity", 0.15); }
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
                            visual.SetOpacity(GhostOpacity);

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
                            model.SetOpacity(GhostOpacity);
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
            var material = ((MenuItem)e.Source).Tag as PCMPMaterial;

            if (material == null)
            {
                MessageBox.Show("No texture assigned!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MainWindow != null)
            {
                if (!MainWindow.IsTextureViewerOpen)
                    MainWindow.OpenTextureViewer();

                MainWindow.TextureViewer.SelectTexture(material.SubMaterials[0].Textures[0]);
            }
        }

        private void ViewModelMaterial(object sender, RoutedEventArgs e)
        {
            var material = ((MenuItem)e.Source).Tag as PCMPMaterial;

            if (material == null)
            {
                MessageBox.Show("No material assigned!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MainWindow != null)
            {
                if (!MainWindow.IsMaterialEditorOpen)
                    MainWindow.OpenMaterialEditor();

                var matEditor = MainWindow.MaterialEditor;

                var itemsHost = matEditor.MaterialsList.GetItemsHost();

                if (itemsHost != null)
                {
                    foreach (TreeViewItem item in itemsHost.Children)
                    {
                        var matItem = item.Header as MaterialTreeItem;

                        if (matItem != null && matItem.Material == material)
                        {
                            item.IsSelected = true;
                            break;
                        }
                    }
                }
            }
        }

        public void SetDriv3rModel(List<ModelVisual3DGroup> models)
        {
            ClearModels();

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

        public ModelViewer()
        {
            InitializeComponent();

            DeselectModel.Click += (o, e) => {
                SelectedModel = null;
            };

            Viewport.Loaded += (o, e) => {
                // Set up FOV and Near/Far distance
                VCam.FieldOfView = Settings.Configuration.GetSetting<int>("DefaultFOV", 45);
                VCam.NearPlaneDistance = Settings.Configuration.GetSetting<double>("NearDistance", 0.125);
                VCam.FarPlaneDistance = Settings.Configuration.GetSetting<double>("FarDistance", 150000);
            };
        }
    }
}
