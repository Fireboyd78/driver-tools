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

using System.Xml;

using HelixToolkit.Wpf;

using Microsoft.Win32;

using FreeImageAPI;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AntilliWindow
    {
        List<ModelVisual3DGroup> currentModel;
        ModelVisual3DGroup selectedModel;

        IModelFile modelFile;

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

                switch (Path.GetExtension(filename).ToLower())
                {
                case ".vvs":
                    modelFile = new VVSFile(filename);
                    goto vehicles;
                case ".vvv":
                    modelFile = new VVVFile(filename);
                    goto vehicles;
                vehicles:
                    {
                        var city = Driv3r.GetCityFromFileName(filename);

                        if (city != null)
                        {
                            var path = Driv3r.GetVehicleGlobals(city);
                            modelFile.SpooledFile = new VGTFile(path);
                        }

                        if (modelFile.HasSpooledFile)
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
                    ModelFile = modelFile;
                    SubTitle = filename;

                    Viewport.InfiniteSpin = Settings.Configuration.GetSetting<bool>("InfiniteSpin", true);

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
                Viewport.InfiniteSpin = !Viewport.InfiniteSpin;
                break;
            case Key.G:
                Viewport.DebugMode = !Viewport.DebugMode;
                break;
            case Key.C:
                Viewport.CameraMode = (Viewport.CameraMode == CameraMode.Inspect) ? CameraMode.WalkAround : (Viewport.CameraMode == CameraMode.WalkAround) ? CameraMode.FixedPosition : CameraMode.Inspect;
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

                foreach (ModelVisual3DGroup dg in (List<ModelVisual3DGroup>)CurrentModel)
                    models.Add(new ModelListItem(models, dg));

                return models;
            }
        }

        public void UpdateModels()
        {
            RaisePropertyChanged("CurrentModel");
            RaisePropertyChanged("Elements");
        }

        public List<ModelVisual3DGroup> CurrentModel
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

            TextureCache.FlushIfNeeded();
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

            List<ModelVisual3DGroup> models = new List<ModelVisual3DGroup>();

            VisualsLayer1.Children.Clear();
            VisualsLayer2.Children.Clear();
            VisualsLayer3.Children.Clear();
            //VisualsLayer4.Children.Clear();

            foreach (PartsGroup part in SelectedModelGroup.Parts)
            {
                MeshGroup group = part.Parts[CurrentLOD].Group;

                if (group == null)
                    continue;

                ModelVisual3DGroup parts = new ModelVisual3DGroup();

                foreach (IndexedMesh prim in group.Meshes)
                {
                    DriverModelVisual3D dmodel = new DriverModelVisual3D(ModelFile, SelectedModelPackage, prim, UseBlendWeights);

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

        List<VisualParentHelper> visualParents = new List<VisualParentHelper>();

        public ModelVisual3DGroup SelectedModel
        {
            get { return selectedModel; }
            set
            {
                selectedModel = value;

                if (visualParents.Count > 0)
                {
                    foreach (VisualParentHelper k in visualParents)
                        k.RestoreParent();

                    VisualsLayer4.Children.Clear();
                    visualParents.Clear();
                }

                foreach (ModelVisual3DGroup dModel in (List<ModelVisual3DGroup>)CurrentModel)
                {
                    if (SelectedModel != null && SelectedModel != dModel)
                    {
                        dModel.SetOpacity(GhostOpacity);

                        foreach (ModelVisual3D d in dModel.Models)
                            visualParents.Add(new VisualParentHelper(d, VisualsLayer4));
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

            foreach (ModelVisual3DGroup dgroup in CurrentModel)
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

        public void DEBUG_ExportModelPackageXML()
        {
            if (SelectedModelPackage == null)
            {
                MessageBox.Show("Nothing to export!");
                return;
            }

            XmlDocument xml = new XmlDocument();

            XmlElement mdpcNode = xml.AddElement("ModelPackage")
                                .AddAttribute("Type", Path.GetExtension(ModelFile.ChunkFile.Filename).Split('.')[1].ToUpper())
                                .AddAttribute("Version", 1);

            XmlElement groupsNode = mdpcNode.AddElement("Groups");

            var parts = SelectedModelPackage.Parts;
            int nParts = SelectedModelPackage.Parts.Count;

            for (int g = 0; g < nParts; g++)
            {
                var group = parts[g];

                bool merged = (group.UID != 0 && g + 1 < nParts && parts[g + 1].UID == group.UID);

                var partsGroupNode = groupsNode.AddElementIf("MergedPartsGroup", merged)
                                                    .AddAttributeIf("UID", group.UID, merged)
                                                    .AddAttributeIf("File", String.Format("{0}.obj", group.UID), merged);

                int m = 0;

                bool loop = false;

                do
                {
                    if (loop)
                        group = parts[++g];

                    var groupNode = partsGroupNode.AddElement("PartsGroup")
                                                    .AddAttribute("Name", String.Format("Model{0}", ++m))
                                                    .AddAttributeIf("UID", group.UID, !merged)
                                                    .AddAttributeIf("File", String.Format("{0}.obj", group.UID), !merged);

                    foreach (PartDefinition part in group.Parts)
                    {
                        if (part.Group == null)
                            continue;

                        groupNode.AddElement("Part")
                                    .AddAttribute("Slot", part.ID + 1)
                                    .AddAttribute("Type", part.Reserved)
                                    .AddAttribute("Source", String.Format("Model{0}_{1}", m, part.ID + 1));
                    }

                    loop = (merged && g + 1 < nParts && parts[g + 1].UID == group.UID);

                } while (loop);
            }

            xml.Save(@"C:\Users\Mark\Desktop\export.xml");

            DSC.Log("Exported model package!");
        }

        public MainWindow()
        {
            DataContext = this;
            
            InitializeComponent();

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
            exportXML.Click += (o, e) => DEBUG_ExportModelPackageXML();

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
}
