using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
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
using DSCript.Spooling;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AntilliWindow
    {
        private List<ModelVisual3DGroup> _currentModel;
        private ModelVisual3DGroup _selectedModel;

        private Driv3rModelFile _modelFile;

        static OpenFileDialog openDialog = new OpenFileDialog() {
            CheckFileExists = true,
            CheckPathExists = true,
            Filter = "DRIV3R|*.vvs;*.vvv;*.vgt;*.d3c;*.pcs;*.cpr;*.dam;*.map;*.gfx;*.pmu;*.d3s;*.mec;*.bnk",
            //+ "|Driver: San Francisco|dngvehicles.sp;san_francisco.dngc",
            //+ "|Any file|*.*",
            InitialDirectory = Driv3r.RootDirectory,
            ValidateNames = true,
        };

        //public TextureViewer TextureViewer { get; private set; }
        //public MaterialEditor MaterialEditor { get; private set; }
        //public MaterialEditor GlobalMaterialEditor { get; private set; }

        /// <summary>
        /// Gets the command line arguments that were passed to the application from either the command prompt or the desktop.
        /// </summary>
        public string[] CommandLineArgs { get; private set; }

        public void OpenFile()
        {
            if (openDialog.ShowDialog() ?? false)
            {
                var timer = new Stopwatch();

                timer.Start();

                string filename = openDialog.FileName;
                
                switch (Path.GetExtension(filename).ToLower())
                {
                case ".vvs":
                case ".vvv":
                    {
                        var vehicleFile = new Driv3rVehiclesFile(filename);
                        

                        var city = Driv3r.GetCityFromFileName(filename);
                        var path = Driv3r.GetVehicleGlobals(city);

                        if (path != null)
                            vehicleFile.VehicleGlobals = new StandaloneTextureFile(path);
                        
                        if (vehicleFile.HasVehicleGlobals)
                        {
                            viewGlobalMaterials.Visibility = Visibility.Visible;
        
                            //if (IsGlobalMaterialEditorOpen)
                            //    GlobalMaterialEditor.UpdateMaterials();
                        }
                        else
                        {
                            //if (IsGlobalMaterialEditorOpen)
                            //{
                            //    GlobalMaterialEditor.Close();
                            //    viewGlobalMaterials.Visibility = Visibility.Collapsed;
                            //}

                            viewGlobalMaterials.Visibility = Visibility.Collapsed;
                        }

                        ModelFile = vehicleFile;
                    } break;
                default:
                    ModelFile = new Driv3rModelFile(filename);
                    break;
                }

                if (ModelFile.HasModels)
                {
                    SubTitle = filename;
        
                    Viewport.InfiniteSpin = Settings.Configuration.GetSetting("InfiniteSpin", true);
        
                    viewTextures.IsEnabled = true;
                    viewMaterials.IsEnabled = true;
                }

                OnPropertyChanged("HasGlobals");
                
                timer.Stop();
                
                DSC.Log("Loaded model file in {0}ms.", timer.ElapsedMilliseconds);
            }
        }

        private int currentLod = 0;

        private Dictionary<int, RadioButton> lodButtons;

        public int CurrentLod
        {
            get { return currentLod; }
            set
            {
                if (SetValue(ref currentLod, value, "CurrentLod"))
                {
                    if (SelectedModel != null)
                        OnModelDeselected();

                    var selectedItem = ModelsList.GetSelectedContainer();

                    if (selectedItem != null)
                        selectedItem.IsSelected = false;

                    LoadSelectedModel();
                }
            }
        }

        public List<ModelVisual3DGroup> CurrentModel
        {
            get { return _currentModel; }
            set
            {
                _currentModel = value;

                UpdateModels();
            }
        }

        public Driv3rModelFile ModelFile
        {
            get { return _modelFile; }
            set
            {
                _modelFile = value;
        
                TextureCache.Flush();
                OnPropertyChanged("ModelPackages");
        
                Packages.SelectedIndex = 0;
            }
        }

        public ModelPackagePC SelectedModelPackage { get; private set; }

        public List<ModelPackagePC> ModelPackages
        {
            get
            {
                if (ModelFile != null)
                    return ModelFile.Models;

                return null;
            }
        }

        public ModelVisual3DGroup SelectedModel
        {
            get { return _selectedModel; }
            set
            {
                _selectedModel = value;

                foreach (ModelVisual3DGroup dModel in (List<ModelVisual3DGroup>)CurrentModel)
                {
                    if (SelectedModel != null && SelectedModel != dModel)
                    {
                        dModel.SetOpacity(GhostOpacity);

                        foreach (ModelVisual3D d in dModel.Models)
                            VisualParentHelper.SetParent(d, VisualsLayer4);
                    }
                    else
                        dModel.SetOpacity(1.0);
                }

                if (SelectedModel == null)
                    OnModelDeselected();
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
                {
                    if (dg.Models.Count > 1)
                        models.Add(new ModelVisual3DGroupListItem(models, dg));
                    else
                        models.Add(new ModelListItem(models, dg));
                }

                return models;
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
                if (SelectedModelPackage == null || SelectedModelPackage.Parts == null)
                    return null;

                List<ModelGroupListItem> items = new List<ModelGroupListItem>();

                PartsGroup curPart = null;

                foreach (var part in SelectedModelPackage.Parts)
                {
                    if (curPart == null || curPart.UID != part.UID)
                    {
                        items.Add(new ModelGroupListItem(SelectedModelPackage, part));
                        curPart = part;
                    }
                }

                //for (int p = 0; p < SelectedModelPackage.Parts.Count; p++)
                //{
                //    PartsGroup part = SelectedModelPackage.Parts[p];
                //
                //    items.Add(new ModelGroupListItem(SelectedModelPackage, part));
                //
                //    int pp = p;
                //
                //    while (++pp < SelectedModelPackage.Parts.Count && SelectedModelPackage.Parts[pp].UID == part.UID)
                //        ++p;
                //}

                return items;
            }
        }

        public bool UseBlendWeights
        {
            get { return BlendWeights.IsChecked ?? false; }
            set { BlendWeights.IsChecked = value; }
        }
        
        //public bool IsTextureViewerOpen
        //{
        //    get { return (TextureViewer != null) ? TextureViewer.IsVisible : false; }
        //}
        
        //public bool IsMaterialEditorOpen
        //{
        //    get { return (MaterialEditor != null) ? MaterialEditor.IsVisible : false; }
        //}
        
        //public bool IsGlobalMaterialEditorOpen
        //{
        //    get { return (GlobalMaterialEditor != null) ? GlobalMaterialEditor.IsVisible : false; }
        //}

        //public void OpenTextureViewer()
        //{
        //    TextureViewer = new TextureViewer(this);
        //    TextureViewer.Show();
        //
        //    if (SelectedModelPackage != null)
        //        TextureViewer.UpdateTextures();
        //}
        
        //public void OpenMaterialEditor()
        //{
        //    MaterialEditor = new MaterialEditor(this);
        //    MaterialEditor.Show();
        //
        //    if (SelectedModelPackage != null)
        //        MaterialEditor.UpdateMaterials();
        //}
        
        //public void OpenGlobalMaterialEditor()
        //{
        //    GlobalMaterialEditor = new MaterialEditor(this) {
        //        ShowGlobalMaterials = true
        //    };
        //
        //    GlobalMaterialEditor.Show();
        //}

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

        public void UpdateModels()
        {
            OnPropertyChanged("CurrentModel");
            OnPropertyChanged("Elements");
        }


        public void OnModelDeselected()
        {
            var item = ModelsList.GetSelectedContainer();

            if (item != null)
                item.IsSelected = false;
        }
        

        public void OnModelPackageSelected(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedModel != null)
                SelectedModel = null;

            int index = Packages.SelectedIndex;

            if (index == -1)
                return;

            TextureCache.FlushIfNeeded();
            var modelPackage = ModelPackages[index];

            SelectedModelPackage = modelPackage;

            // see if we need to load it
            if (!SelectedModelPackage.HasModels)
                SelectedModelPackage.GetInterface().Load();

            /* TODO: FIX ME
            if (!modelPackage.HasBlendWeights && UseBlendWeights)
                UseBlendWeights = false;

            BlendWeights.Visibility = (modelPackage.HasBlendWeights) ? Visibility.Visible : System.Windows.Visibility.Hidden;
            */

            OnPropertyChanged("ModelGroups");

            if (modelPackage.HasModels)
            {
                Groups.SelectedIndex = 0;
            }
            else
            {
                Groups.SelectedIndex = -1;
                CurrentModel = null;
            }

            //if (IsTextureViewerOpen)
            //    TextureViewer.UpdateTextures();
            //if (IsMaterialEditorOpen)
            //    MaterialEditor.UpdateMaterials();
        }

        public void ResetLODButtons()
        {
            foreach (RadioButton lod in LODButtons.Children)
                lod.IsEnabled = false;

            if (SelectedModelGroup != null)
            {
                foreach (PartsGroup part in SelectedModelGroup.Parts)
                foreach (PartDefinition partDef in part.Parts)
                    if (partDef.Group != null)
                        foreach (ToggleButton lod in LODButtons.Children)
                            if (int.Parse((string)lod.Tag) == partDef.ID)
                            {
                                lod.IsEnabled = true;
                                break;
                            }
            }
        }

        public void LoadSelectedModel()
        {
            if (SelectedModel != null)
            {
                RestoreVisualParents();
                SelectedModel = null;
            }

            ResetLODButtons();

            VisualsLayer1.Children.Clear();
            VisualsLayer2.Children.Clear();
            VisualsLayer3.Children.Clear();
            VisualsLayer4.Children.Clear();

            var lodButton = lodButtons[CurrentLod];

            if (lodButton.IsEnabled && lodButton.IsChecked == false)
                lodButton.IsChecked = true;
            else if (!lodButton.IsEnabled)
            {
                CurrentLod = 0;
                BlendWeights.Visibility = System.Windows.Visibility.Hidden;
                return;
            }

            BlendWeights.Visibility = ((SelectedModelGroup.Parts[0].VertexBuffer.HasBlendWeights)) ? Visibility.Visible : System.Windows.Visibility.Hidden;

            List<ModelVisual3DGroup> models = new List<ModelVisual3DGroup>();

            foreach (PartsGroup part in SelectedModelGroup.Parts)
            {
                MeshGroup group = part.Parts[CurrentLod].Group;

                if (group == null)
                    continue;

                ModelVisual3DGroup parts = new ModelVisual3DGroup();

                foreach (MeshDefinition prim in group.Meshes)
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

        public void RestoreVisualParents()
        {
            if (VisualParentHelper.ResetAllParents())
                VisualsLayer4.Children.Clear();
        }

        public void ToggleBlendWeights()
        {
            if (CurrentModel == null)
                return;

            var selectedModel = (SelectedModel != null) ? SelectedModel : null;
            var item = ModelsList.GetSelectedContainer();

            SelectedModel = null;

            foreach (ModelVisual3DGroup dgroup in CurrentModel)
            {
                foreach (DriverModelVisual3D dmodel in dgroup.Models)
                    dmodel.UseBlendWeights = UseBlendWeights;
            }

            if (selectedModel != null)
                SelectedModel = selectedModel;
            if (item != null)
                item.IsSelected = true;
        }

        public void OnModelSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
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

                foreach (ModelVisual3DGroup dModel in (List<ModelVisual3DGroup>)CurrentModel)
                {
                    foreach (ModelVisual3D model in dModel.Models)
                    {
                        if (model != (ModelVisual3D)item)
                        {
                            model.SetOpacity(GhostOpacity);
                            VisualParentHelper.SetParent(model, VisualsLayer4);
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

            //if (!IsTextureViewerOpen)
            //    OpenTextureViewer();
            //
            //TextureViewer.SelectTexture(material.SubMaterials[0].Textures[0]);
        }

        //--nope
        //public void ExportGlobals()
        //{
        //    if (HasGlobals)
        //    {
        //        ModelPackage modelPackage = ModelFile.SpooledFile.ModelData;
        //
        //        string path = Path.Combine(Settings.Configuration.GetDirectory("Export"), Path.GetFileName(ModelFile.SpooledFile.ChunkFile.Filename));
        //
        //        modelPackage.Compile();
        //        ModelFile.SpooledFile.ChunkFile.Export(path);
        //
        //        string msg = String.Format("Successfully exported to '{0}'!", path);
        //        MessageBox.Show(msg, "ModelPackage Exporter", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //}

        public void ExportModelPackage()
        {
            if (SelectedModelPackage == null)
                MessageBox.Show("Nothing to export!");
            else
            {
                //string path = Path.Combine(Settings.Configuration.GetDirectory("Export"), Path.GetFileName(ModelFile.ChunkFile.Filename));
                //
                //DSC.Log("Compiling ModelPackage...");
                //SelectedModelPackage.Compile();
                //
                //DSC.Log("Exporting ModelPackage...");
                //ModelFile.ChunkFile.Export(path);
                //DSC.Log("Done!");
                //
                //string msg = String.Format("Successfully exported to '{0}'!", path);
                //MessageBox.Show(msg, "ModelPackage Exporter", MessageBoxButton.OK, MessageBoxImage.Information);

                MessageBox.Show("Exporting is broken :(");
            }
        }

        //--nope
        //public void ExportModelPackageXML()
        //{
        //    if (SelectedModelPackage == null)
        //    {
        //        MessageBox.Show("Nothing to export!");
        //        return;
        //    }
        //
        //    XmlDocument xml = new XmlDocument();
        //
        //    XmlElement mdpcNode = xml.AddElement("ModelPackage")
        //                                .AddAttribute("Type", Path.GetExtension(ModelFile.ChunkFile.Filename).Split('.')[1].ToUpper())
        //                                .AddAttribute("Version", 1);
        //
        //    XmlElement groupsNode = mdpcNode.AddElement("Groups");
        //
        //    var parts = SelectedModelPackage.PartsGroups;
        //    int nParts = SelectedModelPackage.PartsGroups.Count;
        //
        //    for (int g = 0; g < nParts; g++)
        //    {
        //        var group = parts[g];
        //
        //        bool merged = (group.UID != 0 && g + 1 < nParts && parts[g + 1].UID == group.UID);
        //
        //        // Custom static extension allows us to conditionally add XML elements
        //        var partsGroupNode = groupsNode.AddElementIf("MergedPartsGroup", merged)
        //                                            .AddAttributeIf("UID", group.UID, merged)
        //                                            .AddAttributeIf("File", String.Format("{0}.obj", group.UID), merged);
        //
        //        int m = 0;
        //
        //        bool loop = false;
        //
        //        do
        //        {
        //            if (loop)
        //                group = parts[++g];
        //
        //            var groupNode = partsGroupNode.AddElement("PartsGroup")
        //                                            .AddAttribute("Name", String.Format("Model{0}", ++m))
        //                                            .AddAttributeIf("UID", group.UID, !merged)
        //                                            .AddAttributeIf("File", String.Format("{0}.obj", group.UID), !merged);
        //
        //            foreach (PartDefinition part in group.Parts)
        //            {
        //                if (part.Group == null)
        //                    continue;
        //
        //                groupNode.AddElement("Part")
        //                            .AddAttribute("Slot", part.ID + 1)
        //                            .AddAttribute("Type", part.Reserved)
        //                            .AddAttribute("Source", String.Format("Model{0}_{1}", m, part.ID + 1));
        //            }
        //
        //            loop = (merged && g + 1 < nParts && parts[g + 1].UID == group.UID);
        //
        //        } while (loop);
        //    }
        //
        //    string dir = Settings.Configuration.GetDirectory("Export");
        //
        //    if (!Directory.Exists(dir))
        //        Directory.CreateDirectory(dir);
        //
        //    string path = String.Format("{0}\\{1}.mdpc.xml",
        //        dir, Path.GetFileName(ModelFile.ChunkFile.Filename));
        //
        //    xml.Save(path);
        //
        //    string msg = String.Format("Successfully exported XML file to '{0}'!", path);
        //
        //    MessageBox.Show(msg, "ModelPackage XML Exporter", MessageBoxButton.OK, MessageBoxImage.Information);
        //}

        public void ExportTexture(PCMPTexture texture)
        {
            ExportTexture(texture, this);
        }

        public void ExportTexture(PCMPTexture texture, Window owner)
        {
            string path = Path.Combine(Settings.Configuration.GetDirectory("Export"), String.Format("{0}.dds", texture.CRC32));

            texture.ExportFile(path);

            string msg = String.Format("Successfully exported to '{0}'!", path);
            MessageBox.Show(owner, msg, "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ReplaceTexture(PCMPTexture texture)
        {
            string initialDirectory = DSC.Configuration.GetDirectory("Driv3r");

            OpenFileDialog replaceTexture = new OpenFileDialog() {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "DDS Texture|*.dds",
                Title = "Choose DDS Texture:",
                //InitialDirectory = initialDirectory,
                ValidateNames = true
            };

            if (replaceTexture.ShowDialog() ?? false)
            {
                using (FileStream ddsFile = File.Open(replaceTexture.FileName, FileMode.Open))
                {
                    byte[] buffer = new byte[ddsFile.Length];

                    ddsFile.Read(buffer, 0, buffer.Length);

                    texture.Buffer  = buffer;
                    texture.CRC32   = Memory.CRC32(buffer);

                    CachedTexture tex = TextureCache.GetCachedTexture(texture);

                    tex.Reload();

                    BitmapSource bmap = tex.GetBitmapSource();

                    tex.Texture.Width = Convert.ToUInt16(bmap.Width);
                    tex.Texture.Height = Convert.ToUInt16(bmap.Height);

                    LoadSelectedModel();

                    //if (IsTextureViewerOpen)
                    //        TextureViewer.ReloadTexture();
                }
            }
        }

        private void Initialize()
        {
            Settings.Verify();

            InitializeComponent();

            KeyDown += OnKeyDownReceived;

            Packages.SelectionChanged += OnModelPackageSelected;
            Groups.SelectionChanged += (o, e) => LoadSelectedModel();

            ModelsList.SelectedItemChanged += OnModelSelected;

            lodButtons = new Dictionary<int, RadioButton>(LODButtons.Children.Count);

            foreach (RadioButton lod in LODButtons.Children)
            {
                int id = int.Parse((string)lod.Tag);
                lodButtons.Add(id, lod);

                lod.Checked += (o, e) => {
                    if (id != CurrentLod)
                        CurrentLod = id;
                };
            }

            BlendWeights.Checked += (o, e) => ToggleBlendWeights();
            BlendWeights.Unchecked += (o, e) => ToggleBlendWeights();

            DeselectModel.Click += (o, e) => {
                SelectedModel = null;
            };

            fileOpen.Click += (o, e) => OpenFile();
            //fileOpen.Click += (o, e) => OpenFileNew();
            fileExit.Click += (o, e) => Environment.Exit(0);

            //viewTextures.Click += (o, e) => OpenTextureViewer();
            //viewMaterials.Click += (o, e) => OpenMaterialEditor();
            //viewGlobalMaterials.Click += (o, e) => OpenGlobalMaterialEditor();

            //--nope sorry
            //exportGlobals.Click += (o, e) => ExportGlobals();
            //exportMDPC.Click += (o, e) => ExportModelPackage();
            //exportXML.Click += (o, e) => ExportModelPackageXML();

            //chunkTest.Click += (o, e) => {
            //    var cViewer = new ChunkViewer();
            //
            //    cViewer.Show();
            //};

            var d3Log = new Action<string>((s) => {
                Console.WriteLine(s);
            });

            Viewport.Loaded += (o, e) => {
                // Set up FOV and Near/Far distance
                VCam.FieldOfView = Settings.Configuration.GetSetting<int>("DefaultFOV", 45);
                VCam.NearPlaneDistance = Settings.Configuration.GetSetting<double>("NearDistance", 0.125);
                VCam.FarPlaneDistance = Settings.Configuration.GetSetting<double>("FarDistance", 150000);

                if (CommandLineArgs != null)
                {
                    string str = "";

                    for (int i = 0; i < CommandLineArgs.Length; i++)
                        str += CommandLineArgs[i].Trim('\0');

                    if (str != "")
                        Viewport.DebugInfo = String.Format("Loaded with arguments: {0}", str);
                }

                #region disabled code
#if MECREADER
                foreach (var fi in Directory.GetFiles(@"C:\Dev\Research\Driv3r\Territory\Europe\GUI\backup", "*.MEC"))
                {
                    var mecFile = new SpoolableChunk(fi);
                    var locFile = new LocaleReader(
                        String.Format(@"C:\Dev\Research\Driv3r\Territory\Europe\Locale\English\GUI\{0}.txt", Path.GetFileNameWithoutExtension(fi)));

                    var RMDL = ((SpoolableChunk)mecFile.Spoolers[0]).Spoolers[0] as SpoolableData;

                    var log = new StringBuilder();
                    var logFile = Path.ChangeExtension(fi, "log");

                    var colSize = 20;

                    if (RMDL != null && RMDL.Magic == (int)ChunkType.ReflectionsMenuDataChunk)
                    {
                        log.AppendLine(
@"MEC Reader Log File
File: {0}
----", fi);

                        using (var ms = new MemoryStream(RMDL.Buffer))
                        {
                            var type = ms.ReadInt32();
                            var count = ms.ReadInt32();

                            Debug.Assert((type == 0x191), "Type mismatch!");

                            var unk_08 = ms.ReadInt32(); // 0x0C
                            var unk_0C = ms.ReadInt32(); // 0x54
                            var unk_10 = ms.ReadInt32(); // 0x20
                            var unk_14 = ms.ReadInt32(); // 0x54
                            var unk_18 = ms.ReadInt32(); // 0x14
                            var unk_1C = ms.ReadInt32(); // 0x144
                            var unk_20 = ms.ReadInt32(); // 0x20

                            var readFloats1 = new Action(() => {
                                //for (int ki = 0; ki < (unk_10 / 4); ki++)
                                //    log.AppendLine(ms.ReadSingle().ToString());

                                log.AppendColumn("Position", colSize).AppendLine("{0}, {1}",
                                    ms.ReadSingle(),
                                    ms.ReadSingle());

                                log.AppendColumn("Size", colSize).AppendLine("{0}, {1}",
                                    ms.ReadSingle(),
                                    ms.ReadSingle());

                                log.AppendColumn("Color", colSize).AppendLine("{0:N1}, {1:N1}, {2:N1}, {3:N1}",
                                    ms.ReadSingle(),
                                    ms.ReadSingle(),
                                    ms.ReadSingle(),
                                    ms.ReadSingle()).AppendLine();

                            });

                            var readFloats2 = new Action(() => {
                                log.AppendColumn("Offset", colSize).AppendLine("0x{0:X}", ms.Position).AppendLine();

                                var locId = ms.ReadInt32();

                                log.AppendColumn("LocaleId", colSize).AppendLine("{0}\t; \"{1}\"", locId, locFile[locId]);
                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadInt32());
                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadInt32());

                                log.AppendColumn("Scale X", colSize).AppendLine(ms.ReadSingle());
                                log.AppendColumn("Scale Y", colSize).AppendLine(ms.ReadSingle());

                                var nxt = ms.Position + 0x20;

                                var pUnk1 = ms.ReadString();

                                log.AppendColumn("Unknown", colSize).AppendLine((!String.IsNullOrEmpty(pUnk1)) ? pUnk1 : "<NULL>");

                                ms.Position = nxt;

                                nxt += 0x20;

                                var pUnk2 = ms.ReadString();

                                log.AppendColumn("Unknown", colSize).AppendLine((!String.IsNullOrEmpty(pUnk2)) ? pUnk2 : "<NULL>");

                                ms.Position = nxt;
                            });

                            var readFloats3 = new Action(() => {
                                log.AppendColumn("Offset", colSize).AppendLine("0x{0:X}", ms.Position);

                                //log.AppendLine("Unk1: {0}", ms.ReadSingle());
                                //log.AppendLine("Unk2: {0}", ms.ReadSingle());
                                //log.AppendLine("Unk3: {0}", ms.ReadSingle());
                                //log.AppendLine("Unk4: {0}", ms.ReadSingle());

                                ms.Position += 0x10;

                                log.AppendColumn("TexId", colSize).AppendLine(ms.ReadInt32());
                            });

                            var printHex = new Action<byte[], int, int, int>((bytes, offset, length, stride) => {

                                for (int i = 0; i < length; i += stride)
                                {
                                    // hex column
                                    for (int h = 0; h < stride; h++)
                                    {
                                        var idx = (offset + h + i);

                                        if (idx < length)
                                            log.AppendFormat("{0:X2} ", bytes[idx]);
                                        else
                                            log.Append("   ");
                                    }

                                    log.Append(" ");

                                    // ascii column
                                    for (int d = 0; d < stride; d++)
                                    {
                                        var idx = (offset + d + i);

                                        if (!(idx < bytes.Length))
                                            break;

                                        var val = bytes[idx];

                                        log.Append((val >= 0x20) ? (char)val : '.');
                                    }

                                    log.AppendLine();
                                }

                                log.AppendLine();

                            });

                            var printActionsCallbacks = new Action(() => {

                                var infoTable = new string[8] {
                                    "OnArrowKeyUp",
                                    "OnArrowKeyDown",
                                    "OnArrowKeyLeft",
                                    "OnArrowKeyRight",
                                    "OnPressed",
                                    "Unknown",
                                    "Unknown",
                                    "Unknown"
                                };

                                log.AppendLine("----- Actions -----");

                                //printHex(ms.ReadBytes(0x44), 0, 0x44, 16);

                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadByte()).AppendLine();

                                ms.Position += 3;

                                for (int j = 0; j < 8; j++)
                                {
                                    var unk1 = ms.ReadByte();

                                    ms.Position += 3;

                                    var mVal = ms.ReadInt32();

                                    log.AppendColumn(infoTable[j], 20).AppendFormat("{0}", (unk1 != 0xFF) ? String.Format("{0}, {1}", unk1, mVal) : "<NULL>");

                                    if (unk1 != 0xFF)
                                        log.AppendFormat("\t; {0} {1}", (unk1 >= 1) ? "open menu" : "highlight object", mVal);

                                    log.AppendLine();
                                }

                                log.AppendLine();
                                log.AppendLine("----- Callbacks -----");

                                var baseOffset = ms.Position;

                                for (int i = 0; i < 8; i++)
                                {
                                    ms.Position = baseOffset + (i * 0x20);

                                    var val = ms.ReadString();

                                    if (String.IsNullOrEmpty(val))
                                        val = "<NoAction>";

                                    log.AppendColumn(infoTable[i], 20).AppendLine(val);
                                }

                                ms.Position = baseOffset + 0x100;
                            });

                            for (int i = 0; i < count; i++)
                            {
                                log.AppendLine("==================== Menu {0} ====================", i + 1);

                                var baseOffset = ms.Position;

                                log.AppendColumn("Name", colSize).AppendLine(ms.ReadString(8).Trim('\0'));
                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadInt32());

                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadByte());

                                var m_offset1 = (ms.Position += 4);

                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadString());

                                ms.Position = m_offset1 + 0x20;

                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadString());

                                ms.Position = m_offset1 + 0x40;

                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadByte()).AppendLine();

                                ms.Position = baseOffset + unk_0C;

                                var m_count = ms.ReadInt32();

                                log.AppendColumn("Entries", colSize).AppendLine(m_count).AppendLine();

                                for (int m = 0; m < m_count; m++)
                                {
                                    log.AppendLine("--------------- Entry {0} ---------------", m + 1);

                                    var mm_count = ms.ReadInt32();

                                    log.AppendColumn("Objects", colSize).AppendLine(mm_count).AppendLine();

                                    for (int k = 0; k < mm_count; k++)
                                    {
                                        log.AppendLine("---------- Object {0} ----------", k + 1);

                                        var k_type = ms.ReadByte();

                                        log.AppendColumn("Type", colSize).AppendLine(k_type);

                                        switch (k_type)
                                        {
                                        case 4:
                                            {
                                                readFloats1();
                                            } break;
                                        case 3:
                                            {
                                                readFloats1();

                                                var k_count = ms.ReadInt32();

                                                if (k_count != 0)
                                                {
                                                    log.AppendColumn("States", colSize).AppendLine(k_count).AppendLine();

                                                    for (int kk = 0; kk < k_count; kk++)
                                                    {
                                                        log.AppendLine("----- State {0} -----", kk + 1);

                                                        var kk_count = ms.ReadInt32();

                                                        log.AppendColumn("Elements", colSize).AppendLine(kk_count).AppendLine();

                                                        if (kk_count == 0)
                                                            continue;

                                                        for (int kki = 0; kki < kk_count; kki++)
                                                        {
                                                            log.AppendLine("--- Element {0} ---", kki + 1);

                                                            var j = ms.ReadByte();

                                                            log.AppendColumn("Type", colSize).AppendLine(j);

                                                            switch (j)
                                                            {
                                                            case 2:
                                                                {
                                                                    readFloats1();
                                                                    readFloats2();

                                                                    //log.AppendLine("Skipping 0x{0:X} of data @ 0x{1:X}...", unk_14, ms.Position);
                                                                    //ms.Position += unk_14;
                                                                } break;
                                                            case 1:
                                                                {
                                                                    readFloats1();
                                                                    readFloats3();

                                                                    //log.AppendLine("Skipping 0x{0:X} of data @ 0x{1:X}...", unk_18, ms.Position);
                                                                    //ms.Position += unk_18;
                                                                } break;
                                                            default:
                                                                log.AppendLine("Unrecognized type.");
                                                                break;
                                                            }

                                                            log.AppendLine();
                                                        }
                                                    }
                                                }

                                                printActionsCallbacks();

                                                //log.AppendLine("Skipping 0x{0:X} of data @ 0x{1:X}...", unk_1C, ms.Position);
                                                //ms.Position += unk_1C;
                                            } break;
                                        case 2:
                                            {
                                                readFloats1();
                                                readFloats2();

                                                //var locId = ms.ReadInt32();

                                                //log.AppendLine("LocaleId: {0}", locId);
                                                //
                                                //log.AppendLine("Skipping 0x{0:X} of data @ 0x{1:X}...", (unk_0C - 4), ms.Position);
                                                //ms.Position += (unk_0C - 4);
                                            } break;
                                        case 1:
                                            {
                                                readFloats1();
                                                readFloats3();
                                                //log.AppendLine("Skipping 0x{0:X} of data @ 0x{1:X}...", unk_18, ms.Position);
                                                //ms.Position += unk_18;
                                            } break;
                                        default:
                                            Debug.Fail(String.Format("Unknown type @ offset 0x{0:X}!!!", ms.Position - 1));
                                            Environment.Exit(1);
                                            break;
                                        }

                                        log.AppendLine();
                                    }
                                }
                            }
                        }

                        File.WriteAllText(logFile, log.ToString());
                    }

                    mecFile.Dispose();
                }
#endif
                /*
                var filename = @"C:\Dev\Research\Driv3r\__Research\PS2\city3.chunk";
                //var vvsFile = new DSCript.Spoolers.SpoolableChunk(filename);

                var d3cFile = new DSCript.Spoolers.SpoolableChunk(filename);
                
                DSC.Log("Working...");

                int nModelsRemoved = 0;

                foreach (var spooler in d3cFile.Spoolers)
                {
                    if (spooler is DSCript.Spoolers.SpoolableChunk)
                    {
                        var chnk = (DSCript.Spoolers.SpoolableChunk)spooler;

                        while (true)
                        {
                            var gmc2 = chnk.Spoolers.FirstOrDefault((s) => s.Magic == 0x32434D47);

                            if (gmc2 != null)
                            {
                                chnk.Spoolers.Remove(gmc2);
                                ++nModelsRemoved;
                            }
                            
                            if (chnk.Spoolers.Count > 0)
                            {
                                var last = chnk.Spoolers.Last();

                                if (last.Magic == 0x44504547)
                                {
                                    chnk = ((DSCript.Spoolers.SpoolableChunk)last);
                                    continue;
                                }
                            }

                            break;
                        }
                    }
                }

                d3cFile.Save(Path.Combine(Settings.Configuration.GetDirectory("Export"), Path.GetFileName(filename)));

                DSC.Log("Work done. Removed {0} GMC2 models.", nModelsRemoved);*/
                
            #if false
                // Merge cars
                int nCars = vvsFile.Spoolers.Count / 2;

                for (int i = 0, m = nCars; i < nCars; i++, m++)
                {
                    var hierarchy = vvsFile.Spoolers[i];
                    var model = vvsFile.Spoolers[m];

                    if (hierarchy is DSCript.Spoolers.SpoolableChunk)
                    {
                        ((DSCript.Spoolers.SpoolableChunk)hierarchy).Spoolers.Add(model);
                    }
                }

                vvsFile.Spoolers.RemoveRange(nCars, nCars);
                vvsFile.Save(Path.Combine(Settings.Configuration.GetDirectory("Export"), Path.GetFileName(filename)));
            #endif
            #if false   
                // Split cars
                int nCars = vvsFile.Spoolers.Count;

                for (int i = 0; i < nCars; i++)
                {
                    var pak = (DSCript.Spoolers.SpoolableChunk)vvsFile.Spoolers[i];

                    var model = pak.Spoolers[1];

                    vvsFile.Spoolers.Add(model);
                    pak.Spoolers.Remove(model);
                }
            #endif
            #if false
                // one that has a cop car
                var miamiVVV = @"C:\Dev\Research\Driv3r\Vehicles\_backup\mission60.vvv";

                var vvvFile = new DSCript.Spoolers.SpoolableChunk(miamiVVV);
                var vvvRoot = (DSCript.Spoolers.SpoolableChunk)vvvFile.Spoolers[0];

                // get hierarchy and model from VVV file
                var UPVH = vvvRoot.Spoolers[0];
                var MDPC = vvvRoot.Spoolers[1];

                var newCop = new DSCript.Spoolers.SpoolableChunk(0x9, 0) {
                    Description = UPVH.Description,
                    Spoolers = new List<DSCript.Spoolers.Spooler>(2) { UPVH, MDPC }
                };

                vvsFile.Spoolers.Add(newCop);
            #endif

                //vvsFile.Save(Path.Combine(Settings.Configuration.GetDirectory("Export"), Path.GetFileName(filename)));

                //DSC.Log("Work done.");

                //var msg = String.Format("Press OK to delete sparse file \"{0}\" (length: {1}MB).", sFile, (sparseFile.Length / 1024) / 1024);
                //
                //if (MessageBox.Show(msg, "Testing", MessageBoxButton.OK, MessageBoxImage.Information) == MessageBoxResult.OK)              
                //    sparseFile.Dispose();

                //--Obj importer
                //var objFile = new ObjFile(@"C:\Users\Mark\Desktop\chally.obj");
                //var groups = new List<ModelVisual3DGroup>();
                //
                //foreach (ObjFile.ObjGroup group in objFile.Groups)
                //{
                //    ModelVisual3DGroup parts = new ModelVisual3DGroup();
                //
                //    var models  = group.GetModel().Children;
                //
                //    foreach (GeometryModel3D geom in models)
                //    {
                //        var model = new ModelVisual3D() { Content = geom };
                //        VisualsLayer1.Children.Add(model);
                //
                //        parts.Models.Add(model);
                //    }
                //
                //    groups.Add(parts);
                //}
                //
                //CurrentModel = groups;
                //
                //DSC.Log("Done");
                #endregion
            };
        }

        public MainWindow(string[] args = null)
        {
            if (!ArrayHelper.IsNullOrEmpty(args))
                CommandLineArgs = args;

            Initialize();
        }
    }
}
