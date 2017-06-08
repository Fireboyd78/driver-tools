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
using System.Windows.Threading;

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
        private Driv3rModelFile _modelFile;
        
        public TextureViewer TextureViewer { get; private set; }
        public MaterialEditor MaterialEditor { get; private set; }
        public MaterialEditor GlobalMaterialEditor { get; private set; }

        /// <summary>
        /// Gets the command line arguments that were passed to the application from either the command prompt or the desktop.
        /// </summary>
        public string[] CommandLineArgs { get; private set; }
        
        private int m_currentTab;

        public int CurrentTab
        {
            get { return m_currentTab; }
            set
            {
                m_currentTab = value;
                // TODO: Temporarily unload stuff?
            }
        }

        private void LoadDriv3rVehicles(string filename)
        {
            var vehicleFile = new Driv3rVehiclesFile(filename);

            var city = Driv3r.GetCityFromFileName(filename);
            var vgtFile = "";

            if (city != Driv3r.City.Unknown)
                vgtFile = String.Format("{0}\\{1}\\CarGlobals{1}.vgt", Path.GetDirectoryName(filename), city.ToString());

            if (File.Exists(vgtFile))
                vehicleFile.VehicleGlobals = new StandaloneTextureFile(vgtFile);

            if (vehicleFile.HasVehicleGlobals)
            {
                viewGlobalMaterials.Visibility = Visibility.Visible;

                if (IsGlobalMaterialEditorOpen)
                    GlobalMaterialEditor.UpdateMaterials();
            }
            else
            {
                if (IsGlobalMaterialEditorOpen)
                {
                    GlobalMaterialEditor.Close();
                    viewGlobalMaterials.Visibility = Visibility.Collapsed;
                }

                viewGlobalMaterials.Visibility = Visibility.Collapsed;
            }

            ModelFile = vehicleFile;
        }

        private void OnFileOpened(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();
            var filter = FileManager.FindFilter(extension, GameType.Driv3r, (GameFileFlags.Models | GameFileFlags.Textures));

            if (filter.Flags == GameFileFlags.None)
            {
                MessageBox.Show("Unsupported file type selected, please try another file.", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            var timer = new Stopwatch();
            timer.Start();
            
            switch (extension)
            {
            case ".vvs":
            case ".vvv":
                LoadDriv3rVehicles(filename);
                break;
            default:
                ModelFile = new Driv3rModelFile(filename);
                break;
            }

            SubTitle = filename;

            if (ModelFile.HasModels)
            {
                Viewer.Viewport.InfiniteSpin = Settings.InfiniteSpin;

                viewTextures.IsEnabled = true;
                viewMaterials.IsEnabled = true;
            }

            timer.Stop();

            DSC.Update($"Loaded model file in {timer.ElapsedMilliseconds}ms.");
        }

        public void OpenFile()
        {
            var dialog = FileManager.Driv3rOpenDialog;

            if (dialog.ShowDialog() ?? false)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() => {
                    OnFileOpened(dialog.FileName);
                }));
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

        public ModelGroupListItem SelectedModelGroup
        {
            get { return Groups.SelectedItem as ModelGroupListItem; }
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
                
                return items;
            }
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

        public void OnModelPackageSelected(object sender, SelectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(() => {

            if (Viewer.SelectedModel != null)
                Viewer.SelectedModel = null;

            int index = Packages.SelectedIndex;

            if (index == -1)
                return;

            TextureCache.FlushIfNeeded();
            var modelPackage = ModelPackages[index];

            SelectedModelPackage = modelPackage;

            // see if we need to load it
            if (!SelectedModelPackage.HasModels)
                SelectedModelPackage.GetInterface().Load();
            
            OnPropertyChanged("ModelGroups");

            if (SelectedModelPackage.HasModels)
            {
                Groups.SelectedIndex = 0;
            }
            else
            {
                Groups.SelectedIndex = -1;
                Viewer.Visuals = null;
            }
            
            if (IsTextureViewerOpen)
                TextureViewer.UpdateTextures();
            if (IsMaterialEditorOpen)
                MaterialEditor.UpdateMaterials();
            }));
        }

        private bool m_hasBlendWeights;

        public bool HasBlendWeights
        {
            get { return m_hasBlendWeights; }
            set
            {
                m_hasBlendWeights = value;
                OnPropertyChanged("BlendWeightsVisible");
            }
        }

        public Visibility BlendWeightsVisible
        {
            get { return (m_hasBlendWeights) ? Visibility.Visible : Visibility.Collapsed; }
        }
        
        private RadioButton[] m_lodBtnRefs = new RadioButton[7];

        private bool m_allowLodChanges = true;
        private bool m_blockNextLodChange = false;

        private int m_curLod = 0;

        public void ResetLODButtons()
        {
            m_allowLodChanges = false;

            foreach (var lodBtn in m_lodBtnRefs)
            {
                if (lodBtn != null)
                {
                    lodBtn.IsEnabled = false;
                    lodBtn.IsChecked = false;
                }
            }

            m_allowLodChanges = true;
        }

        public void OnLevelOfDetailChanged(int oldLod = -1)
        {
            if (oldLod != -1)
            {
                if (oldLod != m_curLod)
                    m_lodBtnRefs[oldLod].IsChecked = false;
            }
            else
            {
                if (oldLod != m_curLod)
                {
                    var lodBtn = m_lodBtnRefs[m_curLod];

                    if (lodBtn.IsChecked != true)
                    {
                        m_blockNextLodChange = true;
                        lodBtn.IsChecked = true;
                    }
                }
            }

            if (m_curLod != -1)
            {
                if (oldLod != m_curLod)
                    LoadSelectedModel(false);
            }
            else
            {
                Viewer.ClearModels();
                Viewer.Viewport.SetDebugInfo("No valid levels of detail in model.");

                ResetLODButtons();
            }
        }

        public int LevelOfDetail
        {
            get { return m_curLod; }
            set
            {
                var lod = value;
                var lodBtn = m_lodBtnRefs[lod];

                if (lodBtn == null)
                {
                    var newLod = -1;

                    for (int i = 0; i < 7; i++)
                    {
                        if (m_lodBtnRefs[i] != null)
                        {
                            newLod = i;
                            break;
                        }
                    }
                    
                    lod = newLod;
                }

                var oldLod = m_curLod;
                m_curLod = lod;

                OnLevelOfDetailChanged(oldLod);
            }
        }
        
        public void UpdateRenderingOptions()
        {
            var checkBlendWeights = true;

            var lodCount = new int[7];

            foreach (var part in SelectedModelGroup.Parts)
            {
                // run this check once to prevent unnecessary slowdown
                if (checkBlendWeights)
                {
                    checkBlendWeights = false;
                    HasBlendWeights = part.VertexBuffer.HasBlendWeights;
                }
                
                // count all possible LODs
                for (int i = 0; i < 7; i++)
                {
                    var partDef = part.Parts[i];

                    if (partDef == null)
                        continue;

                    if (partDef.Groups.Count > 0)
                        lodCount[i] += 1;
                }
            }

            // enable LODs if more than one model present
            for (int i = 0; i < 7; i++)
            {
                var lodBtn = m_lodBtnRefs[i];

                if (lodBtn != null)
                    lodBtn.IsEnabled = (lodCount[i] > 0);
            }
        }
        
        public void LoadSelectedModel(bool fullUpdate)
        {
            if (SelectedModelGroup == null)
            {
                Groups.SelectedIndex = 0;
                return;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            if (Viewer.SelectedModel != null)
                Viewer.OnModelDeselected();

            var selectedItem = Viewer.ModelsList.GetSelectedContainer();

            if (selectedItem != null)
                selectedItem.IsSelected = false;

            if (fullUpdate)
            {
                ResetLODButtons();
                UpdateRenderingOptions();

                OnLevelOfDetailChanged(-1);
            }
            
            var models = new List<ModelVisual3DGroup>();

            foreach (var part in SelectedModelGroup.Parts)
            {
                var partDef = part.Parts[LevelOfDetail];

                if (partDef.Groups == null)
                    continue;

                var meshes = new ModelVisual3DGroup();

                foreach (var group in partDef.Groups)
                {
                    foreach (var mesh in group.Meshes)
                    {
                        //Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, new ThreadStart(() => {
                        meshes.Children.Add(new DriverModelVisual3D(mesh, Viewer.UseBlendWeights));
                        //}));
                    }
                }
                
                if (meshes.Children.Count > 0)
                    models.Add(meshes);
            }

            if (models.Count > 0)
            {
                // set the new model
                Viewer.SetDriv3rModel(models);
            }
            else
            {
                // just clear the models
                Viewer.ClearModels();
                Viewer.Viewport.SetDebugInfo("Level of detail contains no valid models.");
            }
        }
        
        private void ViewModelTexture(object sender, RoutedEventArgs e)
        {
            var material = ((MenuItem)e.Source).Tag as DSCript.Models.MaterialData;

            if (material == null)
            {
                MessageBox.Show("No texture assigned!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!IsTextureViewerOpen)
                OpenTextureViewer();
            
            TextureViewer.SelectTexture(material.Substances[0].Textures[0]);
        }
        
        private void ExportObjFile()
        {
            if (SelectedModelPackage == null)
                MessageBox.Show("Nothing to export!");
            else
            {
                // TODO: Implement OBJ exporter
                if (SelectedModelGroup != null)
                {
                    var prompt = new MKInputBox("OBJ Exporter", "Please enter a name for the model:", SelectedModelGroup.UID.ToString("D10")) {
                        Owner = this,
                        ShowCancelButton = false,
                        ShowOptionCheckbox = true,
                        OptionName = "Split Meshes by Material",
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
                    };

                    if (prompt.ShowDialog() ?? false)
                    {
                        var path = Path.Combine(Settings.ModelsDirectory, prompt.InputValue);

                        if (OBJFile.Export(path, prompt.InputValue, SelectedModelPackage, SelectedModelGroup.UID, prompt.IsOptionChecked) == ExportResult.Success)
                        {
                            var msg = String.Format("Successfully exported to '{0}'!", path);
                            MessageBox.Show(msg, "OBJ Exporter", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            var msg = String.Format("Failed to export the file '{0}'!", path);
                            MessageBox.Show(msg, "OBJ Exporter", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        public void ExportModelPackage()
        {
            if (SelectedModelPackage == null)
                MessageBox.Show("Nothing to export!");
            else
            {
                string path = Path.Combine(Settings.ExportDirectory, Path.GetFileName(ModelFile.FileName));
                
                DSC.Log("Compiling ModelPackage...");
                SelectedModelPackage.GetInterface().Save();
                
                DSC.Log("Exporting ModelPackage...");
                SelectedModelPackage.ModelFile.Save(path, false);
                DSC.Log("Done!");
                
                string msg = String.Format("Successfully exported to '{0}'!", path);
                MessageBox.Show(msg, "ModelPackage Exporter", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void ExportVehicleHierarchyVPK()
        {
            var modelFile = ModelFile as Driv3rVehiclesFile;

            if (SelectedModelPackage == null || ModelFile == null)
            {
                MessageBox.Show("Nothing to export!");
                return;
            }

            var idx = (!modelFile.IsMissionVehicleFile) ? modelFile.Models.IndexOf(SelectedModelPackage) : Groups.SelectedIndex;

            var hierarchy =  modelFile.Hierarchies[idx];

            var dir = Settings.ExportDirectory;
            var path = String.Format("{0}\\{1}_{2}.vpk", dir, Path.GetFileName(ModelFile.FileName).Replace('.', '_'), hierarchy.UID);

            hierarchy.SaveVPK(path);

            var msg = String.Format("Successfully exported VPK file to '{0}'!", path);

            MessageBox.Show(msg, "VehicleHierarchy VPK Exporter", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        public void ExportTexture(TextureData texture)
        {
            ExportTexture(texture, this);
        }

        public void ExportTexture(TextureData texture, Window owner)
        {
            string path = Path.Combine(Settings.ExportDirectory, String.Format("{0}.dds", texture.CRC32));

            texture.ExportFile(path);

            string msg = String.Format("Successfully exported to '{0}'!", path);
            MessageBox.Show(owner, msg, "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ReplaceTexture(TextureData texture)
        {
            OpenFileDialog replaceTexture = new OpenFileDialog() {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "DDS Texture|*.dds",
                Title = "Choose DDS Texture:",
                ValidateNames = true
            };

            if (replaceTexture.ShowDialog() ?? false)
            {
                using (FileStream ddsFile = File.Open(replaceTexture.FileName, FileMode.Open))
                {
                    byte[] buffer = new byte[ddsFile.Length];

                    ddsFile.Read(buffer, 0, buffer.Length);

                    texture.Buffer  = buffer;
                    texture.CRC32   = (int)Memory.GetCRC32(buffer);

                    CachedTexture tex = TextureCache.GetCachedTexture(texture);

                    tex.Reload();

                    BitmapSource bmap = tex.GetBitmapSource();

                    tex.Texture.Width = Convert.ToUInt16(bmap.Width);
                    tex.Texture.Height = Convert.ToUInt16(bmap.Height);

                    LoadSelectedModel(false);

                    if (IsTextureViewerOpen)
                        TextureViewer.ReloadTexture();
                }
            }
        }

        private void Initialize()
        {
            Settings.Verify();

            InitializeComponent();
            
            DSC.ProgressUpdated += (o, e) => {
                var progress_str = e.Message;

                if (e.Progress > -1)
                    progress_str += $" [{Math.Round(e.Progress):F1}%]";

                // might re-use this eventually
                //Viewer.Viewport.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new ThreadStart(() => {
                //    Viewer.Viewport.SetDebugInfo(progress_str);
                //}));

                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new ThreadStart(() => {
                    Console.WriteLine($"[INFO] {progress_str}");
                }));
            };

            KeyDown += (o,e) => {
                switch (e.Key)
                {
                case Key.I:
                    Viewer.ToggleInfiniteSpin();
                    break;
                case Key.G:
                    Viewer.ToggleDebugMode();
                    break;
                case Key.C:
                    Viewer.ToggleCameraMode();
                    break;
                }
            };

            Viewer.MainWindow = this;

            Packages.SelectionChanged += OnModelPackageSelected;
            Groups.SelectionChanged += (o, e) => LoadSelectedModel(true);

            foreach (var child in LODButtons.Children)
            {
                var lodBtn = (child as RadioButton);

                if (lodBtn != null)
                {
                    var lod = int.Parse((string)lodBtn.Tag);
                    m_lodBtnRefs[lod] = lodBtn;

                    lodBtn.Checked += (o, e) => {
                        if (m_allowLodChanges)
                        {
                            if (!m_blockNextLodChange)
                            {
                                if (lodBtn.IsChecked != true)
                                    return;

                                // update the level of detail
                                LevelOfDetail = lod;
                            }
                            else
                            {
                                m_blockNextLodChange = false;
                            }
                        }
                    };
                }
            }
            
            BlendWeights.Checked += (o, e) => Viewer.ToggleBlendWeights();
            BlendWeights.Unchecked += (o, e) => Viewer.ToggleBlendWeights();
            
            fileOpen.Click += (o, e) => OpenFile();
            
            fileExit.Click += (o, e) => Environment.Exit(0);

            viewTextures.Click += (o, e) => OpenTextureViewer();
            viewMaterials.Click += (o, e) => OpenMaterialEditor();
            viewGlobalMaterials.Click += (o, e) => OpenGlobalMaterialEditor();
            
            exportMDPC.Click += (o, e) => ExportModelPackage();
            exportVPK.Click += (o, e) => ExportVehicleHierarchyVPK();
            exportObj.Click += (o, e) => ExportObjFile();

            chunkViewer.Click += (o, e) => {
                var cViewer = new ChunkViewer();
            
                cViewer.Show();
            };

            modelTool.Click += (o, e) => {
                var mTool = new Importer();

                mTool.Show();
            };

            var d3Log = new Action<string>((s) => {
                Console.WriteLine(s);
            });

            Viewer.Loaded += (o, e) => {
                if (CommandLineArgs != null)
                {
                    string str = "";

                    for (int i = 0; i < CommandLineArgs.Length; i++)
                        str += CommandLineArgs[i].Trim('\0');

                    if (str != "")
                        Viewer.Viewport.DebugInfo = String.Format("Loaded with arguments: {0}", str);
                }

                //if (String.IsNullOrEmpty((string)DSC.Configuration["Driv3r"]))
                //{
                //    MessageBox.Show("ERROR: Driv3r not found!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                //    Environment.Exit(1);
                //}

                #region disabled code
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
            if (args != null && args.Length > 0)
                CommandLineArgs = args;

            Initialize();
        }
    }
}
