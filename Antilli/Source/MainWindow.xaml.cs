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
        //public TextureViewer TextureViewer { get; private set; }
        //public MaterialEditor MaterialEditor { get; private set; }
        //public MaterialEditor GlobalMaterialEditor { get; private set; }
        
        private void LoadDriv3rVehicles(string filename)
        {
            var vehicleFile = new Driv3rVehiclesFile(filename);

            var city = Driv3r.GetCityFromFileName(filename);
            var vgtFile = "";

            if (city != Driv3r.City.Unknown)
                vgtFile = String.Format("{0}\\{1}\\CarGlobals{1}.vgt", Path.GetDirectoryName(filename), city.ToString());

            if (File.Exists(vgtFile))
                vehicleFile.VehicleGlobals = new StandaloneTextureFile(vgtFile);

            AT.CurrentState.ModelFile = vehicleFile;
            AT.CurrentState.CanUseGlobals = vehicleFile.HasVehicleGlobals;
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
                AT.CurrentState.ModelFile = new Driv3rModelFile(filename);
                AT.CurrentState.CanUseGlobals = false;
                break;
            }

            SubTitle = filename;

            if (CurrentModelFile.HasModels)
            {
                Viewer.Viewport.InfiniteSpin = Settings.InfiniteSpin;
                Packages.SelectedIndex = 0;
            }

            timer.Stop();

            DSC.Update($"Loaded model file in {timer.ElapsedMilliseconds}ms.");
        }

        public void OpenFile()
        {
            var dialog = FileManager.Driv3rOpenDialog;

            if (dialog.ShowDialog() ?? false)
                OnFileOpened(dialog.FileName);
        }

        public int CurrentTab
        {
            get { return AT.CurrentState.CurrentTab; }
            set { AT.CurrentState.CurrentTab = value; }
        }
        
        public Visibility CanShowBlendWeights
        {
            get { return AT.CurrentState.CanShowBlendWeights; }
        }

        public Visibility CanShowGlobals
        {
            get { return AT.CurrentState.CanShowGlobals; }
        }

        public Driv3rModelFile CurrentModelFile
        {
            get { return AT.CurrentState.ModelFile; }
        }

        public ModelPackagePC CurrentModelPackage
        {
            get { return AT.CurrentState.SelectedModelPackage; }
        }

        public List<ModelPackagePC> ModelPackages
        {
            get { return AT.CurrentState.ModelPackages; }
        }

        public ModelGroupListItem SelectedPartsGroup
        {
            get { return Groups.SelectedItem as ModelGroupListItem; }
        }

        public List<ModelGroupListItem> PartsGroups
        {
            get
            {
                if (CurrentModelPackage == null || CurrentModelPackage.Parts == null)
                    return null;

                List<ModelGroupListItem> items = new List<ModelGroupListItem>();

                PartsGroup curPart = null;

                foreach (var part in CurrentModelPackage.Parts)
                {
                    if (curPart == null || curPart.UID != part.UID)
                    {
                        items.Add(new ModelGroupListItem(CurrentModelPackage, part));
                        curPart = part;
                    }
                }
                
                return items;
            }
        }

        public List<MaterialTreeItem> Materials
        {
            get
            {
                var materials = new List<MaterialTreeItem>();
                int count = 0;
                
                if (CurrentModelPackage != null && CurrentModelPackage.HasMaterials)
                {
                    foreach (var material in CurrentModelPackage.Materials)
                        materials.Add(new MaterialTreeItem(++count, material));
                }
                
                return materials;
            }
        }

        public List<MaterialTreeItem> GlobalMaterials
        {
            get
            {
                var modelFile = CurrentModelFile as Driv3rVehiclesFile;

                var materials = new List<MaterialTreeItem>();
                int count = 0;

                if (modelFile != null && modelFile.HasVehicleGlobals)
                {
                    var modelPackage = modelFile.VehicleGlobals.GetModelPackage();

                    foreach (var material in modelPackage.Materials)
                        materials.Add(new MaterialTreeItem(++count, material));
                }

                return materials;
            }
        }

        public List<TextureTreeItem> Textures
        {
            get
            {
                if (CurrentModelPackage != null)
                {
                    var textures = new List<TextureTreeItem>();
                    int count = 0;

                    foreach (var texture in CurrentModelPackage.Textures)
                        textures.Add(new TextureTreeItem(count, texture));
                    
                    return textures;
                }
                return null;
            }
        }

        public List<TextureTreeItem> GlobalTextures
        {
            get
            {
                var modelFile = CurrentModelFile as Driv3rVehiclesFile;
                
                if (modelFile != null && modelFile.HasVehicleGlobals)
                {
                    var modelPackage = modelFile.VehicleGlobals.GetModelPackage();

                    var textures = new List<TextureTreeItem>();
                    int count = 0;

                    foreach (var texture in modelPackage.Textures)
                        textures.Add(new TextureTreeItem(count, texture));

                    return textures;
                }
                return null;
            }
        }

        public void MoveToTab(int index)
        {
            CurrentTab = index;
            OnPropertyChanged("CurrentTab");
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

                AT.CurrentState.SelectedModelPackage = modelPackage;
                
                if (CurrentModelPackage.HasModels)
                {
                    Groups.SelectedIndex = 0;
                }
                else
                {
                    Groups.SelectedIndex = -1;
                    Viewer.Visuals = null;
                }
            }));
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

            foreach (var part in SelectedPartsGroup.Parts)
            {
                // run this check once to prevent unnecessary slowdown
                if (checkBlendWeights)
                {
                    checkBlendWeights = false;
                    AT.CurrentState.CanUseBlendWeights = part.VertexBuffer.HasBlendWeights;
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
            if (SelectedPartsGroup == null)
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

            foreach (var part in SelectedPartsGroup.Parts)
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
            var material = ((MenuItem)e.Source).Tag as DSCript.Models.MaterialDataPC;

            if (material == null)
            {
                MessageBox.Show("No texture assigned!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!AT.CurrentState.IsTextureViewerOpen)
                AT.CurrentState.CurrentTab = 2;
            
            //TextureViewer.SelectTexture(material.Substances[0].Textures[0]);
        }
        
        private void ExportObjFile()
        {
            if (CurrentModelPackage == null)
                MessageBox.Show("Nothing to export!");
            else
            {
                // TODO: Implement OBJ exporter
                if (SelectedPartsGroup != null)
                {
                    var prompt = new MKInputBox("OBJ Exporter", "Please enter a name for the model:", SelectedPartsGroup.UID.ToString("D10")) {
                        Owner = this,
                        ShowCancelButton = false,
                        ShowOptionCheckbox = true,
                        OptionName = "Split Meshes by Material",
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
                    };

                    if (prompt.ShowDialog() ?? false)
                    {
                        var path = Path.Combine(Settings.ModelsDirectory, prompt.InputValue);

                        if (OBJFile.Export(path, prompt.InputValue, CurrentModelPackage, SelectedPartsGroup.UID, prompt.IsOptionChecked) == ExportResult.Success)
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
            if (CurrentModelPackage == null)
                MessageBox.Show("Nothing to export!");
            else
            {
                string path = Path.Combine(Settings.ExportDirectory, Path.GetFileName(CurrentModelFile.FileName));
                
                DSC.Log("Compiling ModelPackage...");
                CurrentModelPackage.GetInterface().Save();
                
                DSC.Log("Exporting ModelPackage...");
                CurrentModelPackage.ModelFile.Save(path, false);
                DSC.Log("Done!");
                
                string msg = String.Format("Successfully exported to '{0}'!", path);
                MessageBox.Show(msg, "ModelPackage Exporter", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void ExportVehicleHierarchyVPK()
        {
            var modelFile = CurrentModelFile as Driv3rVehiclesFile;

            if (CurrentModelPackage == null || CurrentModelFile == null)
            {
                MessageBox.Show("Nothing to export!");
                return;
            }

            var idx = (!modelFile.IsMissionVehicleFile) ? modelFile.Models.IndexOf(CurrentModelPackage) : Groups.SelectedIndex;

            var hierarchy =  modelFile.Hierarchies[idx];

            var dir = Settings.ExportDirectory;
            var path = String.Format("{0}\\{1}_{2}.vpk", dir, Path.GetFileName(CurrentModelFile.FileName).Replace('.', '_'), hierarchy.UID);

            hierarchy.SaveVPK(path);

            var msg = String.Format("Successfully exported VPK file to '{0}'!", path);

            MessageBox.Show(msg, "VehicleHierarchy VPK Exporter", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        public void ExportTexture(ITextureData texture)
        {
            ExportTexture(texture, this);
        }

        public void ExportTexture(ITextureData texture, Window owner)
        {
            var path = Path.Combine(Settings.ExportDirectory, String.Format("{0}.dds", texture.UID));
            
            FileManager.WriteFile(path, texture.Buffer);

            string msg = String.Format("Successfully exported to '{0}'!", path);
            MessageBox.Show(owner, msg, "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ReplaceTexture(ITextureData texture)
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
                var texRef = TextureCache.GetTexture(texture);

                using (var ddsFile = File.Open(replaceTexture.FileName, FileMode.Open))
                {
                    var buffer = new byte[ddsFile.Length];

                    ddsFile.Read(buffer, 0, buffer.Length);
                    texRef.SetBuffer(buffer);
                    
                    LoadSelectedModel(false);

                    if (IsViewWidgetVisible)
                        CurrentViewWidget.SetTexture(texture);
                }
            }
        }

        private void ReplaceTexture(object sender, RoutedEventArgs e)
        {
            var item = ((sender as FrameworkElement).DataContext) as TextureTreeItem;

            if (item != null)
            {
                var tex = item.Texture;
                ReplaceTexture(tex);

                CurrentViewWidget.Update();
            }
        }

        private void ExportTexture(object sender, RoutedEventArgs e)
        {
            var item = ((sender as FrameworkElement).DataContext) as TextureTreeItem;

            if (item != null)
            {
                var tex = item.Texture;
                ExportTexture(tex);
            }
        }
        
        public ImageWidget CurrentViewWidget
        {
            get
            {
                switch (CurrentTab)
                {
                case 1: return MaterialViewWidget;
                case 2: return TextureViewWidget;
                }

                return null;
            }
        }

        public bool IsViewWidgetVisible
        {
            get { return CurrentViewWidget != null; }
        }

        public int MatTexRowSpan
        {
            get { return (AT.CurrentState.CanUseGlobals) ? 1 : 2; }
        }

        private void SetCurrentViewElement<T>(Action<T> setterFunc, T element)
            where T : class
        {
            CurrentViewWidget.Clear();

            if (element != null)
                setterFunc(element);
        }

        private void OnCurrentViewElementUpdated(object obj)
        {
            var objId = (obj is MaterialTreeItem) ? 0
                        : (obj is SubstanceTreeItem) ? 1
                        : (obj is TextureTreeItem) ? 2
                        : -1;

            // unhandled type?!
            if (objId == -1)
                return;

            switch (objId)
            {
            case 0:
                SetCurrentViewElement(CurrentViewWidget.SetMaterial, ((MaterialTreeItem)obj).Material);
                break;
            case 1:
                SetCurrentViewElement(CurrentViewWidget.SetSubstance, ((SubstanceTreeItem)obj).Substance);
                break;
            case 2:
                SetCurrentViewElement(CurrentViewWidget.SetTexture, ((TextureTreeItem)obj).Texture);
                break;
            }
        }
        
        private void OnMaterialListSelectionChanged(object sender, RoutedEventArgs e)
        {
            var source = e.Source as TreeView;

            if (source != null)
                OnCurrentViewElementUpdated(source.SelectedItem);
        }

        private void OnTextureListSelectionChanged(object sender, RoutedEventArgs e)
        {
            var source = e.Source as ListBox;

            if (source != null)
                OnCurrentViewElementUpdated(source.SelectedItem);
        }

        private bool TrySelectMaterial(TreeView tree, IMaterialData material)
        {
            var generator = tree.ItemContainerGenerator;

            // do we need to update the layout?
            if (generator.Status == GeneratorStatus.NotStarted)
                tree.UpdateLayout();

            foreach (var item in tree.Items.OfType<MaterialTreeItem>())
            {
                if (Object.ReferenceEquals(material, item.Material))
                {
                    var childNode = generator.ContainerFromItem(item) as TreeViewItem;
                    
                    if (childNode != null)
                    {
                        childNode.Focus();
                        childNode.IsSelected = true;

                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("Couldn't select the requested material :(");
                        return false;
                    }
                }
            }
            return false;
        }

        private bool TrySelectTexture(ListBox list, ITextureData texture)
        {
            foreach (var item in list.Items.OfType<TextureTreeItem>())
            {
                if (Object.ReferenceEquals(texture, item.Texture))
                {
                    MoveToTab(2);

                    list.SelectedItem = item;
                    return true;
                }
            }
            return false;
        }
        
        private void Initialize()
        {
            Settings.Verify();

            InitializeComponent();

            AT.CurrentState.PropertyChanged += (o, e) => {
                Debug.WriteLine($"OnPropertyChanged: '{e.PropertyName}'");
                OnPropertyChanged(e.PropertyName);
            };

            /*
                These methods check the globals (if applicable),
                then checks the model packages for the material/texture
            */
            AT.CurrentState.MaterialSelectQueried += (o, e) => {
                var material = o as IMaterialData;

                MoveToTab(1);

                var selected = (AT.CurrentState.CanUseGlobals && TrySelectMaterial(GlobalMaterialsList, material))
                || TrySelectMaterial(MaterialsList, material);
            };
            AT.CurrentState.TextureSelectQueried += (o, e) => {
                var texture = o as ITextureData;
                
                var selected = (AT.CurrentState.CanUseGlobals && TrySelectTexture(GlobalTextureList, texture))
                    || TrySelectTexture(TextureList, texture);
            };
            
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
                switch (CurrentTab)
                {
                case 0:
                    Viewer.OnKeyPressed(o, e);
                    break;
                case 1:
                    MaterialViewWidget.OnKeyPressed(o, e);
                    break;
                case 2:
                    TextureViewWidget.OnKeyPressed(o, e);
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

            /*
            viewTextures.Click += (o, e) => OpenTextureViewer();
            viewMaterials.Click += (o, e) => OpenMaterialEditor();
            viewGlobalMaterials.Click += (o, e) => OpenGlobalMaterialEditor();
            */
            
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
            Initialize();
        }
    }
}
