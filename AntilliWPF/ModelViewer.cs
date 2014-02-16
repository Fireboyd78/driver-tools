using System;
using System.Collections.Generic;
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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Threading;
//using System.Windows.Shapes;

using HelixToolkit.Wpf;

using Microsoft.Win32;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    public class ModelViewer : Observable
    {
        const string titleFmt = "{0} - {1}";

        string subTitle = String.Empty;

        Model3D currentModel;
        Model3D selectedModel;

        IModelFile modelFile;

        bool debugMode = false;
        
        readonly Dispatcher Dispatcher;
        readonly HelixViewport3D Viewer;
        readonly PerspectiveCamera Camera;

        delegate void ResetDebugCallback();

        public MainWindow Parent { get; private set; }

        public TextureViewer TextureViewer
        {
            get
            {
                if (Parent != null)
                    return Parent.TextureViewer;

                throw new NullReferenceException("Parent must be defined!");
            }
        }

#region DebugInfo methods/properties
        static Thread debugInfoThread;
        volatile bool restartThread = false;

        public void SetDebugInfo(string str, params object[] args)
        {
            SetDebugInfo(String.Format(str, args));
        }

        public void SetDebugInfo(string str)
        {
            Viewer.DebugInfo = str;

            if (debugInfoThread != null && debugInfoThread.IsAlive)
                restartThread = true;
            else
            {
                debugInfoThread = new Thread(new ThreadStart(DelayResetDebugInfo)) { IsBackground = true };
                debugInfoThread.Start();
            }
        }

        void ResetDebugInfo()
        {
            Viewer.DebugInfo = String.Empty;
        }

        void DelayResetDebugInfo()
        {
            int timeout = Settings.Configuration.GetSetting<int>("DebugInfoTimeout", 1500);

            for (int i = 0; i < timeout; i++)
            {
                Thread.Sleep(1);

                if (restartThread)
                {
                    i = 0;
                    restartThread = false;
                }
            }

            Viewer.Dispatcher.Invoke(new ResetDebugCallback(ResetDebugInfo));
        }
#endregion 

        public string SubTitle
        {
            get { return subTitle; }
            set
            {
                subTitle = value;

                Parent.Title = String.Format(titleFmt, Parent.Title, subTitle);
            }
        }

        public bool DebugMode
        {
            get { return debugMode; }
            set
            {
                Viewer.ShowFieldOfView = value;
                Viewer.ShowFrameRate = value;
                Viewer.ShowCameraInfo = value;
                Viewer.ShowCameraTarget = value;
                Viewer.ShowTriangleCountInfo = value;

                debugMode = value;

                SetDebugInfo("Debug mode {0}.", (debugMode) ? "enabled" : "disabled");

                RaisePropertyChanged("DebugMode");
            }
        }

        public bool InfiniteSpin
        {
            get { return Viewer.InfiniteSpin; }
            set
            {
                if (Viewer.CameraMode == HelixToolkit.Wpf.CameraMode.WalkAround)
                {
                    SetDebugInfo("Infinite spin cannot be enabled in walkaround mode.");
                }
                else
                {
                    Viewer.InfiniteSpin = value;

                    if (InfiniteSpin)
                        Viewer.CameraController.StartSpin(new Vector(120.0, 0.0), new Point(0, 0), new Point3D(0, 0, 0));

                    SetDebugInfo("Infinite spin {0}.", (InfiniteSpin) ? "enabled" : "disabled");

                    RaisePropertyChanged("InfiniteSpin");
                }
            }
        }

        public void DebugAddBox()
        {
            Model3DGroup group = (CurrentModel is Model3DGroup) ? (Model3DGroup)CurrentModel : new Model3DGroup();

            if (CurrentModel != null && (!(CurrentModel is Model3DGroup)))
                group.Children.Add(CurrentModel);

            MeshBuilder box = new MeshBuilder();
            box.AddBox(new Point3D(0, 0, (group.Children.Count * 1.5)), 1.5, 1.5, 1.5);

            group.Children.Add(new GeometryModel3D() {
                Geometry = box.ToMesh(),
                Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)))
            });

            CurrentModel = group;

            SetDebugInfo("Adding a box to the current model.");
        }

        public void DebugRemoveLastModel()
        {
            if (CurrentModel == null)
            {
                SetDebugInfo("No models in the scene to clear!");
            }
            else if (CurrentModel is Model3DGroup)
            {
                Model3DGroup group = (Model3DGroup)CurrentModel;

                if (group.Children.Count > 1)
                {
                    Model3D model = group.Children.Last();

                    group.Children.Remove(model);
                    model = null;

                    UpdateModels();

                    SetDebugInfo("Removed the last child model.");
                }
                else
                {
                    ClearModel();
                }
            }
            else
            {
                ClearModel();
            }
        }

        public IModelFile ModelFile
        {
            get { return modelFile; }
            set
            {
                modelFile = value;

                Models.TextureCache.Flush();
                TextureViewer.FlushCache();

                RaisePropertyChanged("ModelPackages");

                ModelPackagesList.SelectedIndex = 0;
            }
        }

        ListBox partsGroupsList;
        ListBox modelPackagesList;

        public ListBox PartsGroupsList
        {
            get { return partsGroupsList; }
            set { partsGroupsList = value; }
        }

        public ListBox ModelPackagesList
        {
            get { return modelPackagesList; }
            set { modelPackagesList = value; }
        }

        ModelPackage selectedModelPackage;

        public ModelPackage SelectedModelPackage
        {
            get { return selectedModelPackage; }
        }

        public List<ModelPackage> ModelPackages
        {
            get
            {
                if (ModelFile != null)
                    return ModelFile.Models;
                
                return null;
            }
        }

        public List<ModelListItem> Elements
        {
            get
            {
                List<ModelListItem> models = new List<ModelListItem>();

                int idx = 0;

                if (CurrentModel != null)
                {
                    if (CurrentModel is Model3DGroup)
                    {
                        foreach (Model3D m3d in ((Model3DGroup)CurrentModel).Children)
                            models.Add(new ModelListItem(++idx, m3d));
                    }
                    else
                    {
                        models.Add(new ModelListItem(++idx, CurrentModel));
                    }
                }

                return models;
            }
        }

        void UpdateModels()
        {
            RaisePropertyChanged("CurrentModel");
            RaisePropertyChanged("Elements");
        }

        public void ClearModel()
        {
            CurrentModel = null;

            SetDebugInfo("Removed current model.");
        }

        public Model3D CurrentModel
        {
            get { return currentModel; }
            set
            {
                currentModel = value;

                UpdateModels();
            }
        }

        public double GhostOpacity
        {
            get { return Settings.Configuration.GetSetting<double>("GhostOpacity", 0.15); }
        }

        public Model3D SelectedModel
        {
            get { return selectedModel; }
            set
            {
                selectedModel = value;

                if (CurrentModel != null)
                {
                    if (CurrentModel is Model3DGroup)
                    {
                        foreach (Model3D m3d in ((Model3DGroup)CurrentModel).Children)
                            m3d.SetOpacity((m3d != SelectedModel) ? GhostOpacity : 1.0);
                    }
                }
            }
        }

        public bool IsTextureViewerOpen
        {
            get { return TextureViewer != null; }
        }

        List<long> modelGroups;

        public List<long> ModelGroups
        {
            get { return modelGroups; }
        }

        public void OnModelPackageSelected(object sender, SelectionChangedEventArgs e)
        {
            int index = ((ListBox)sender).SelectedIndex;

            if (index == -1)
                return;

            ModelPackage modelPackage = ModelPackages[index];

            selectedModelPackage = modelPackage;

            if (modelPackage.Parts != null)
            {
                modelGroups = new List<long>();

                foreach (PartsGroup group in modelPackage.Parts)
                {
                    long uid = group.UID;

                    if (!modelGroups.Contains(uid))
                    {
                        if (group.Parts[0].Group != null)
                        {
                            modelGroups.Add(uid);
                        }
                    }
                }
            }
            else
            {
                modelGroups = null;
            }

            RaisePropertyChanged("ModelGroups");
            PartsGroupsList.SelectedIndex = (modelPackage.Parts != null) ? 0 : -1;

            if (modelPackage.Parts == null)
                CurrentModel = null;

            Models.TextureCache.FlushIfNeeded();

            if (IsTextureViewerOpen)
                TextureViewer.UpdateTextures();
        }

        public void OnPartsGroupSelected(object sender, SelectionChangedEventArgs e)
        {
            ListBox partsGroups = (ListBox)sender;

            if (PartsGroupsList.SelectedIndex == -1)
                return;

            Model3DGroup models = new Model3DGroup();

            long uid = (long)partsGroups.SelectedItem;

            foreach (PartsGroup part in SelectedModelPackage.Parts)
            {
                if (uid != part.UID)
                    continue;

                for (int p = 0; p < part.Parts.Count; p++)
                {
                    PartDefinition partDef = part.Parts[p];

                    // if (partDef.Group != null)
                    //     foreach (KeyValuePair<ToggleButton, int> lod in LODTypes)
                    //         if (lod.Value == p)
                    //         {
                    //             lod.Key.IsEnabled = true;
                    //             break;
                    //         }
                }

                MeshGroup group = part.Parts[0].Group;

                if (group == null)
                    continue;

                Model3DGroup partModels = new Model3DGroup();

                foreach (IndexedPrimitive prim in group.Meshes)
                {
                    DriverModel3D model = new DriverModel3D(SelectedModelPackage, prim);
                    partModels.Children.Add(model);
                }

                models.Children.Add(partModels);
            }

            CurrentModel = models;
        }

        public void OnModelSelected(object sender, SelectionChangedEventArgs e)
        {
            ListBox modelsList = (ListBox)sender;

            if (modelsList.SelectedIndex != -1)
            {
                ModelListItem item = (ModelListItem)modelsList.Items[modelsList.SelectedIndex];

                SelectedModel = item.Content;
            }
            else
            {
                if (modelsList.Items.Count > 0)
                {
                    if (CurrentModel != null)
                        CurrentModel.SetOpacity(1.0);
                }
                else
                {
                    CurrentModel = null;
                }
            }
        }

        public void OnTextureViewerOpened()
        {
            if (SelectedModelPackage != null)
                TextureViewer.UpdateTextures();
        }

        public ModelViewer(MainWindow parent, HelixViewport3D viewport, PerspectiveCamera camera)
        {
            Parent      = parent;

            Dispatcher  = Dispatcher.CurrentDispatcher;
            Viewer      = viewport;
            Camera      = camera;

            viewport.Loaded += (o, e) => {
                InfiniteSpin            = Settings.Configuration.GetSetting<bool>("InfiniteSpin", true);

                // Set up FOV and Near/Far distance
                Camera.FieldOfView        = Settings.Configuration.GetSetting<int>("DefaultFOV", 45);
                Camera.NearPlaneDistance  = Settings.Configuration.GetSetting<double>("NearDistance", 0.125);
                Camera.FarPlaneDistance   = Settings.Configuration.GetSetting<double>("FarDistance", 150000);
            };

            MeshBuilder box = new MeshBuilder();
            box.AddBox(new Point3D(0, 0, 0), 1.5, 1.5, 1.5);

            CurrentModel = new GeometryModel3D() {
                Geometry = box.ToMesh(),
                Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)))
            };
        }
    }
}
