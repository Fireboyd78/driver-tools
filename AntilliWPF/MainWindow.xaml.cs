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
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
//using System.Windows.Shapes;

using HelixToolkit.Wpf;

using Microsoft.Win32;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementations
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }
        #endregion

        #region DebugInfo methods/properties
        static Thread debugInfoThread;
        volatile bool restartThread = false;

        public void SetDebugInfo(string str, params object[] args)
        {
            SetDebugInfo(String.Format(str, args));
        }

        public void SetDebugInfo(string str)
        {
            Viewport.DebugInfo = str;

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
            Viewport.DebugInfo = String.Empty;
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

            Viewport.Dispatcher.Invoke(new ResetDebugCallback(ResetDebugInfo));
        }
        #endregion

        const string titleFmt = "{0} - {1}";

        string title = String.Empty;
        string subTitle = String.Empty;

        List<DriverModelGroup> currentModel;
        DriverModelGroup selectedModel;

        IModelFile modelFile;

        bool debugMode = false;
        bool isWalkAround = false;
        bool isSorting = true;

        delegate void ResetDebugCallback();

        public string SubTitle
        {
            get { return subTitle; }
            set
            {
                subTitle = value;

                Title = String.Format(titleFmt, title, subTitle);
            }
        }

        public bool DebugMode
        {
            get { return debugMode; }
            set
            {
                Viewport.ShowFieldOfView = value;
                Viewport.ShowFrameRate = value;
                Viewport.ShowCameraInfo = value;
                Viewport.ShowCameraTarget = value;
                Viewport.ShowTriangleCountInfo = value;

                debugMode = value;

                SetDebugInfo("Debug mode {0}.", (debugMode) ? "enabled" : "disabled");

                RaisePropertyChanged("DebugMode");
            }
        }

        public bool InfiniteSpin
        {
            get { return Viewport.InfiniteSpin; }
            set
            {
                if (Viewport.CameraMode == HelixToolkit.Wpf.CameraMode.WalkAround)
                {
                    SetDebugInfo("Infinite spin cannot be enabled in walkaround mode.");
                }
                else
                {
                    Viewport.InfiniteSpin = value;

                    if (InfiniteSpin)
                        Viewport.CameraController.StartSpin(new Vector(120.0, 0.0), new Point(0, 0), new Point3D(0, 0, 0));

                    SetDebugInfo("Infinite spin {0}.", (InfiniteSpin) ? "enabled" : "disabled");

                    RaisePropertyChanged("InfiniteSpin");
                }
            }
        }

        public bool WalkaroundMode
        {
            get { return isWalkAround; }
            set
            {
                isWalkAround = value;

                Viewport.CameraMode = (!isWalkAround) ? CameraMode.Inspect : CameraMode.WalkAround;
                //VP3D.CameraRotationMode                 = (!isWalkAround) ? CameraRotationMode.Trackball : CameraRotationMode.Turntable;
                //VP3D.CameraController.ModelUpDirection  = (!isWalkAround) ? zDown : zUp;
                //VP3D.Camera.UpDirection                 = (!isWalkAround) ? zDown : zUp;

                InfiniteSpin = false;

                //if (!isWalkAround)
                //{
                //    VCam.Position = dCamPos;
                //    VCam.LookDirection = dLookdir;
                //}

                SetDebugInfo("Walkaround mode {0}.", (isWalkAround) ? "enabled" : "disabled");
            }
        }

        public bool IsSorting
        {
            get { return isSorting; }
            set
            {
                isSorting = value;

                SetDebugInfo("Model sorting {0}.", (isSorting) ? "enabled" : "disabled");
                RaisePropertyChanged("IsSorting");
            }
        }

        static OpenFileDialog openFile = new OpenFileDialog() {
            CheckFileExists = true,
            CheckPathExists = true,
            Filter = "DRIV3R|*.vvs;*.vvv;*.vgt;*.d3c;*.pcs;*.cpr;*.dam;*.map;*.gfx;*.pmu;*.d3s;*.mec;*.bnk"
            + "|Driver: San Francisco|dngvehicles.sp;san_francisco.dngc",
            //+ "|Any file|*.*",
            ValidateNames = true,
        };

        public TextureViewer TextureViewer { get; private set; }
        public MaterialEditor MaterialEditor { get; private set; }
        public MaterialEditor GlobalMaterialEditor { get; private set; }

        public void OpenFile()
        {
            if (openFile.ShowDialog() ?? false)
            {
                string filename = openFile.FileName;
                IModelFile modelFile = null;

                switch (Path.GetExtension(filename).Trim('.').ToLower())
                {
                case "vvs":
                    modelFile = new VVSFile(filename);
                    goto vehicles;
                case "vvv":
                    modelFile = new VVVFile(filename);
                    goto vehicles;
                vehicles:
                    {
                    string carGlobalsMiami      = @"Miami\CarGlobalsMiami.vgt";
                    string carGlobalsNice       = @"Nice\CarGlobalsNice.vgt";
                    string carGlobalsIstanbul   = @"Istanbul\CarGlobalsIstanbul.vgt";

                    string root                 = String.Format(@"{0}\Vehicles", DSC.Configuration.GetDirectory("Driv3r"));
                    
                    string pathFmt              = String.Format("{0}{1}", root, @"\{0}");
                    string path                 = String.Empty;

                    if (filename.Contains("miami"))
                        path = String.Format(pathFmt, carGlobalsMiami);
                    else if (filename.Contains("nice"))
                        path = String.Format(pathFmt, carGlobalsNice);
                    else if (filename.Contains("istanbul"))
                        path = String.Format(pathFmt, carGlobalsIstanbul);
                    else if (filename.Contains("mission"))
                    {
                        string strPath = Path.GetFileNameWithoutExtension(filename).ToLower();

                        /* === mission%02d.vvv ===
                        miami: 	    01 - 10, 32-33, 38-40, 50-51, 56, 59-61, 71-72, 77-78
                        nice: 	    11 - 21, 34-35, 42-44, 52-53, 57, 62-64, 73-74, 80-81
                        istanbul: 	22 - 31, 36-37, 46-48, 54-55, 58, 65-67, 75-76, 83-84 */

                        //-- Be careful not to break the formatting!!
                        switch (int.Parse(strPath.Substring(strPath.Length - 2, 2)))
                        {
                        case 01: case 02: case 03: case 04: case 05: case 06:
                        case 07: case 08: case 09: case 10: case 32: case 33:
                        case 38: case 39: case 40: case 50: case 51: case 56:
                        case 59: case 60: case 61: case 71: case 72:
                            path = String.Format(pathFmt, carGlobalsMiami);
                            break;
                        case 11: case 12: case 13: case 14: case 15: case 16:
                        case 17: case 18: case 19: case 20: case 21: case 34:
                        case 35: case 42: case 43: case 44: case 52: case 53:
                        case 57: case 62: case 63: case 64: case 73: case 74:
                            path = String.Format(pathFmt, carGlobalsNice);
                            break;
                        case 22: case 23: case 24: case 25: case 26: case 27:
                        case 28: case 29: case 30: case 31: case 36: case 37:
                        case 46: case 47: case 48: case 54: case 55: case 58:
                        case 65: case 66: case 67: case 75: case 76:
                            path = String.Format(pathFmt, carGlobalsIstanbul);
                            break;
                        }
                    }

                    if (path != String.Empty)
                        ModelPackage.Globals = new VGTFile(path);

                    if (ModelPackage.HasGlobals)
                    {
                        viewGlobalMaterials.Visibility = System.Windows.Visibility.Visible;

                        if (IsGlobalMaterialEditorOpen)
                            GlobalMaterialEditor.UpdateMaterials();
                    }
                    else
                    {
                        if (IsGlobalMaterialEditorOpen)
                        {
                            GlobalMaterialEditor.Close();
                            viewGlobalMaterials.Visibility = System.Windows.Visibility.Collapsed;
                        }
                    }
                    }
                    break;
                default:
                    modelFile = new ModelFile(filename);
                    break;
                }

                if (modelFile.Models != null)
                {
                    SubTitle = filename;
                    ModelFile = modelFile;

                    InfiniteSpin = Settings.Configuration.GetSetting<bool>("InfiniteSpin", true);

                    viewTextures.IsEnabled = true;
                    viewMaterials.IsEnabled = true;
                }
            }
        }

        ToggleButton selectedLOD;

        public ToggleButton SelectedLOD
        {
            get { return selectedLOD; }
            set
            {
                ToggleButton lod = value;

                if (selectedLOD != null)
                {
                    ToggleButton previousLod = selectedLOD;

                    selectedLOD = lod;
                    selectedLOD.IsChecked = true;

                    if (previousLod.IsChecked == true)
                        previousLod.IsChecked = false;

                    LoadSelectedModel();
                }
                else
                {
                    selectedLOD = lod;
                    selectedLOD.IsChecked = true;
                }
            }
        }

        public int CurrentLOD
        {
            get
            {
                if (SelectedLOD == null)
                    SelectedLOD = (ToggleButton)LODButtons.Children[0];

                return int.Parse((string)SelectedLOD.Tag);
            }
        }

        public bool UseBlendWeights
        {
            get { return BlendWeights.IsChecked ?? false; }
            set { BlendWeights.IsChecked = value; }
        }

        public bool IsTextureViewerOpen
        {
            get { return (TextureViewer != null) ? TextureViewer.IsVisible : false; }
        }

        public bool IsMaterialEditorOpen
        {
            get { return (MaterialEditor != null) ? MaterialEditor.IsVisible : false; }
        }

        public bool IsGlobalMaterialEditorOpen
        {
            get { return (GlobalMaterialEditor != null) ? GlobalMaterialEditor.IsVisible : false; }
        }

        public void OpenTextureViewer()
        {
            TextureViewer = new TextureViewer(this);
            TextureViewer.Show();

            if (SelectedModelPackage != null)
                TextureViewer.UpdateTextures();
        }

        public void OpenMaterialEditor()
        {
            MaterialEditor = new MaterialEditor(this);
            MaterialEditor.Show();

            if (SelectedModelPackage != null)
                MaterialEditor.UpdateMaterials();
        }

        public void OpenGlobalMaterialEditor()
        {
            GlobalMaterialEditor = new MaterialEditor(this) {
                ShowGlobalMaterials = true
            };

            GlobalMaterialEditor.Show();
        }

        public void OnKeyDownReceived(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
            case Key.I:
                InfiniteSpin = !InfiniteSpin;
                break;
            case Key.G:
                DebugMode = !DebugMode;
                break;
            case Key.P:
                WalkaroundMode = !WalkaroundMode;
                break;
            case Key.V:
                IsSorting = !IsSorting;
                break;
            default:
                break;
            }
        }

        public IModelFile ModelFile
        {
            get { return modelFile; }
            set
            {
                modelFile = value;

                TextureCache.Flush();
                RaisePropertyChanged("ModelPackages");

                Packages.SelectedIndex = 0;
            }
        }

        public ModelPackage SelectedModelPackage { get; private set; }

        public List<ModelPackage> ModelPackages
        {
            get
            {
                if (ModelFile != null)
                    return ModelFile.Models;

                return null;
            }
        }

        public List<ModelResourcePackage> ModelResourcePackages
        {
            get
            {
                var modelFile = ModelFile as DSFModelFile;

                if (modelFile != null)
                    return modelFile.Models;

                return null;
            }
        }

        public ModelGroupListItem SelectedModelGroup
        {
            get { return Groups.SelectedItem as ModelGroupListItem; }
        }

        public List<ModelListItem> Elements
        {
            get
            {
                if (CurrentModel == null)
                    return null;

                List<ModelListItem> models = new List<ModelListItem>();

                int idx = 0;

                foreach (DriverModelGroup dg in (List<DriverModelGroup>)CurrentModel)
                    models.Add(new ModelListItem(++idx, dg));

                return models;
            }
        }

        public void UpdateModels()
        {
            RaisePropertyChanged("CurrentModel");
            RaisePropertyChanged("Elements");
        }

        public List<DriverModelGroup> CurrentModel
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

        public List<ModelGroupListItem> ModelGroups
        {
            get
            {
                if (SelectedModelPackage == null)
                    return null;

                List<ModelGroupListItem> items = new List<ModelGroupListItem>();

                for (int p = 0; p < SelectedModelPackage.Parts.Count; p++)
                {
                    PartsGroup part = SelectedModelPackage.Parts[p];

                    items.Add(new ModelGroupListItem(ModelFile, SelectedModelPackage, part));

                    int pp = p;

                    while (++pp < SelectedModelPackage.Parts.Count && SelectedModelPackage.Parts[pp].UID == part.UID)
                        ++p;
                }

                return items;
            }
        }

        public void OnModelPackageSelected(object sender, SelectionChangedEventArgs e)
        {
            int index = Packages.SelectedIndex;

            if (index == -1)
                return;

            ModelPackage modelPackage = ModelPackages[index];

            SelectedModelPackage = modelPackage;

            if (!modelPackage.HasBlendWeights && UseBlendWeights)
                UseBlendWeights = false;

            BlendWeights.Visibility = (modelPackage.HasBlendWeights) ? Visibility.Visible : System.Windows.Visibility.Hidden;

            RaisePropertyChanged("ModelGroups");

            Groups.SelectedIndex = (modelPackage.Parts != null) ? 0 : -1;

            if (modelPackage.Parts == null)
                CurrentModel = null;

            if (IsTextureViewerOpen)
                TextureViewer.UpdateTextures();
            if (IsMaterialEditorOpen)
                MaterialEditor.UpdateMaterials();
        }

        public void ResetLODButtons()
        {
            foreach (ToggleButton lod in LODButtons.Children)
                lod.IsEnabled = false;

            foreach (PartsGroup part in SelectedModelGroup.Parts)
            {
                foreach (PartDefinition partDef in part.Parts)
                {
                    if (partDef.Group != null)
                        foreach (ToggleButton lod in LODButtons.Children)
                            if (int.Parse((string)lod.Tag) == partDef.ID)
                            {
                                lod.IsEnabled = true;
                                break;
                            }
                }
            }
        }

        public void LoadSelectedModel()
        {
            if (Groups.SelectedItem == null)
                return;

            ResetLODButtons();

            List<DriverModelGroup> models = new List<DriverModelGroup>();

            VisualsLayer1.Children.Clear();
            VisualsLayer2.Children.Clear();
            VisualsLayer3.Children.Clear();
            //VisualsLayer4.Children.Clear();

            foreach (PartsGroup part in SelectedModelGroup.Parts)
            {
                MeshGroup group = part.Parts[CurrentLOD].Group;

                if (group == null)
                    continue;

                DriverModelGroup parts = new DriverModelGroup();

                foreach (MeshDefinition prim in group.Meshes)
                {
                    DriverModelVisual3D dmodel = new DriverModelVisual3D(ModelFile, SelectedModelPackage, prim);

                    if (dmodel.IsEmissive)
                        VisualsLayer3.Children.Add(dmodel);
                    else if (dmodel.HasTransparency)
                        VisualsLayer2.Children.Add(dmodel);
                    else
                        VisualsLayer1.Children.Add(dmodel);

                    parts.Models.Add(dmodel);
                }

                models.Add(parts);
            }

            CurrentModel = models;
        }

        List<VisualParentSwap> visualParents = new List<VisualParentSwap>();

        public DriverModelGroup SelectedModel
        {
            get { return selectedModel; }
            set
            {
                selectedModel = value;

                if (visualParents.Count > 0)
                {
                    foreach (VisualParentSwap k in visualParents)
                        k.RestoreParent();

                    VisualsLayer4.Children.Clear();
                    visualParents.Clear();
                }

                foreach (DriverModelGroup dModel in (List<DriverModelGroup>)CurrentModel)
                {
                    if (SelectedModel != null && SelectedModel != dModel)
                    {
                        dModel.SetOpacity(GhostOpacity);

                        foreach (DriverModelVisual3D d in dModel.Models)
                            visualParents.Add(new VisualParentSwap(d, VisualsLayer4));
                    }
                    else
                        dModel.SetOpacity(1.0);
                }
            }
        }

        public void ToggleBlendWeights()
        {
            if (CurrentModel == null)
                return;

            foreach (DriverModelGroup dgroup in CurrentModel)
            {
                foreach (DriverModelVisual3D dmodel in dgroup.Models)
                    dmodel.UseBlendWeights = UseBlendWeights;
            }
        }

        public void OnModelSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ModelsList.SelectedItem != null)
            {
                ModelListItem item = ModelsList.SelectedItem as ModelListItem;

                SelectedModel = item.Model;
            }
            else
            {
                SelectedModel = null;
            }
        }

        public void DEBUG_ExportModelPackage()
        {
            if (SelectedModelPackage == null)
                MessageBox.Show("Nothing to export!");
            else
            {
                DSC.Log("Compiling ModelPackage...");
                SelectedModelPackage.Compile();

                DSC.Log("Exporting ModelPackage...");
                ModelFile.ChunkFile.Export(@"C:\Users\Mark\Desktop\export.chunk");
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            title = Title;

            KeyDown += OnKeyDownReceived;

            Packages.SelectionChanged += OnModelPackageSelected;
            Groups.SelectionChanged += (o, e) => LoadSelectedModel();

            ModelsList.SelectionChanged += OnModelSelected;

            foreach (ToggleButton lod in LODButtons.Children)
            {
                lod.Checked += (o, e) => {
                    if (SelectedLOD != lod)
                        SelectedLOD = lod;
                };
                lod.Unchecked += (o, e) => {
                    if (SelectedLOD == lod)
                        lod.IsChecked = true;
                };
            }

            BlendWeights.Checked += (o, e) => ToggleBlendWeights();
            BlendWeights.Unchecked += (o, e) => ToggleBlendWeights();

            DeselectModel.Click += (o, e) => {
                ModelsList.SelectedIndex = -1;
            };

            fileOpen.Click += (o, e) => OpenFile();

            viewTextures.Click += (o, e) => OpenTextureViewer();
            viewMaterials.Click += (o, e) => OpenMaterialEditor();
            viewGlobalMaterials.Click += (o, e) => OpenGlobalMaterialEditor();

            exportMDPC.Click += (o, e) => DEBUG_ExportModelPackage();

            Viewport.Loaded += (o, e) => {
                // Set up FOV and Near/Far distance
                VCam.FieldOfView = Settings.Configuration.GetSetting<int>("DefaultFOV", 45);
                VCam.NearPlaneDistance = Settings.Configuration.GetSetting<double>("NearDistance", 0.125);
                VCam.FarPlaneDistance = Settings.Configuration.GetSetting<double>("FarDistance", 150000);
            };

            // MeshBuilder box = new MeshBuilder();
            // box.AddBox(new Point3D(0, 0, 0), 1.5, 1.5, 1.5);
            // 
            // CurrentModel = new GeometryModel3D() {
            //     Geometry = box.ToMesh(),
            //     Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)))
            // };
        }
    }

    public class VisualParentSwap
    {
        public DriverModelVisual3D Visual { get; private set; }

        public ModelVisual3D Parent { get; private set; }

        public int Index { get; private set; }

        public void RestoreParent()
        {
            ModelVisual3D oldParent = (ModelVisual3D)VisualTreeHelper.GetParent(Visual);

            bool insert = Index <= Parent.Children.Count;

            oldParent.Children.Remove(Visual);

            if (insert)
                Parent.Children.Insert(Index, Visual);
            else
                Parent.Children.Add(Visual);
        }

        public VisualParentSwap(DriverModelVisual3D visual, ModelVisual3D newParent)
        {
            Visual = visual;

            Parent = (ModelVisual3D)VisualTreeHelper.GetParent(Visual);

            Index = Parent.Children.IndexOf(Visual);

            Parent.Children.Remove(Visual);
            newParent.Children.Add(Visual);
        }
    }

    public class ModelGroupListItem
    {
        public string Text
        {
            get { return (!IsNull) ? UID.ToString() : "<NULL>"; }
        }

        public uint UID { get; private set; }

        public bool IsNull
        {
            get
            {
                foreach (PartsGroup part in Parts)
                    if (part.Parts[0].Group != null)
                        return false;

                return true;
            }
        }
        
        public List<PartsGroup> Parts { get; private set; }

        public IModelFile ModelFile { get; private set; }
        public ModelPackage ModelPackage { get; private set; }

        public Model3DGroup Models { get; private set; }

        public Model3DGroup ToModel3DGroup(SortingVisual3D sortingVisual3D, ModelVisual3D sortingVisual3DEmissive, int lodType, bool useBlendWeights)
        {
            Model3DGroup opaqueModels = new Model3DGroup();

            Model3DGroup models = new Model3DGroup();

            sortingVisual3D.Children.Clear();
            sortingVisual3DEmissive.Children.Clear();

            foreach (PartsGroup part in Parts)
            {
                MeshGroup group = part.Parts[lodType].Group;

                if (group == null)
                    continue;

                Model3DGroup partModels = new Model3DGroup();

                Model3DGroup __models = new Model3DGroup();

                foreach (MeshDefinition prim in group.Meshes)
                {
                    Driv3rModel3D dmodel = new Driv3rModel3D(ModelFile, ModelPackage, prim, useBlendWeights);
                    GeometryModel3D geom = dmodel;

                    ModelVisual3D mvis = new ModelVisual3D() { Content = geom };

                    if (dmodel.IsEmissive)
                        sortingVisual3DEmissive.Children.Add(mvis);
                    else if (dmodel.HasTransparency)
                        sortingVisual3D.Children.Add(mvis);
                    else
                        partModels.Children.Add(dmodel);

                    __models.Children.Add(geom);
                }

                models.Children.Add(__models);
                opaqueModels.Children.Add(partModels);
            }

            Models = models;

            return opaqueModels;
        }

        public Model3DGroup ToModel3DGroup(int lodType, bool useBlendWeights)
        {
            Model3DGroup models = new Model3DGroup();

            foreach (PartsGroup part in Parts)
            {
                MeshGroup group = part.Parts[lodType].Group;

                if (group == null)
                    continue;

                Model3DGroup partModels = new Model3DGroup();

                foreach (MeshDefinition prim in group.Meshes)
                    partModels.Children.Add(new Driv3rModel3D(ModelFile, ModelPackage, prim, useBlendWeights));

                models.Children.Add(partModels);
            }

            return models;
        }

        public ModelGroupListItem(IModelFile modelFile, ModelPackage modelPackage, PartsGroup partBasedOn)
        {
            ModelFile = modelFile;
            ModelPackage = modelPackage;
            
            UID = partBasedOn.UID;

            Parts = new List<PartsGroup>();

            int startIndex = ModelPackage.Parts.FindIndex((p) => p == partBasedOn);

            for (int p = startIndex; p < ModelPackage.Parts.Count; p++)
            {
                PartsGroup part = ModelPackage.Parts[p];

                if (part.UID != UID)
                    continue;

                do
                    Parts.Add(part);
                while (++p < ModelPackage.Parts.Count && (part = ModelPackage.Parts[p]).UID == UID);

                break;
            }
        }
    }
}
