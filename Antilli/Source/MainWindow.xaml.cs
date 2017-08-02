using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Globalization;
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
                if (CurrentModelPackage == null || !CurrentModelPackage.HasMaterials)
                    return null;

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

                if (modelFile == null || !modelFile.HasVehicleGlobals)
                    return null;

                var modelPackage = modelFile.VehicleGlobals.GetModelPackage();

                var materials = new List<MaterialTreeItem>();
                int count = 0;
                
                foreach (var material in modelPackage.Materials)
                    materials.Add(new MaterialTreeItem(++count, material));

                return materials;
            }
        }

        public List<TextureTreeItem> Textures
        {
            get
            {
                if (CurrentModelPackage == null || !CurrentModelPackage.HasMaterials)
                    return null;

                var textures = new List<TextureTreeItem>();
                int count = 0;

                foreach (var texture in CurrentModelPackage.Textures)
                    textures.Add(new TextureTreeItem(count++, texture));

                return textures;
            }
        }

        public List<TextureTreeItem> GlobalTextures
        {
            get
            {
                var modelFile = CurrentModelFile as Driv3rVehiclesFile;

                if (modelFile == null || !modelFile.HasVehicleGlobals)
                    return null;

                var modelPackage = modelFile.VehicleGlobals.GetModelPackage();

                var textures = new List<TextureTreeItem>();
                int count = 0;

                foreach (var texture in modelPackage.Textures)
                    textures.Add(new TextureTreeItem(count++, texture));

                return textures;
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

        private void MoveToTab(int index)
        {
            CurrentTab = index;
            OnPropertyChanged("CurrentTab");
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

            AT.Log($"Loaded model file in {timer.ElapsedMilliseconds}ms.");
        }

        private void OnFileOpenClick()
        {
            var dialog = FileManager.Driv3rOpenDialog;

            if (dialog.ShowDialog() ?? false)
                OnFileOpened(dialog.FileName);
        }

        private RadioButton[] m_lodBtnRefs = new RadioButton[7];

        private void ResetViewWidgets()
        {
            MaterialViewWidget.Clear();
            TextureViewWidget.Clear();
        }

        private void UpdateViewWidgets()
        {
            MaterialViewWidget.Update();
            TextureViewWidget.Update();
        }

        private void ResetLODButtons()
        {
            foreach (var lodBtn in m_lodBtnRefs)
            {
                if (lodBtn != null)
                {
                    lodBtn.IsEnabled = false;
                    lodBtn.IsChecked = false;
                }
            }
        }

        private void UpdateRenderingOptions()
        {
            var checkBlendWeights = true;

            var lodCount = new int[7];

            if (SelectedPartsGroup != null)
            {
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
                    {
                        var hasLods = (lodCount[i] > 0);

                        // do we need to uncheck it?
                        if (lodBtn.IsChecked ?? false)
                            lodBtn.IsChecked = hasLods;

                        lodBtn.IsEnabled = (lodCount[i] > 0);
                    }
                }
            }
            
            // select the correct LOD
            m_lodBtnRefs[Viewer.LevelOfDetail].IsChecked = true;
        }
        
        private void ExportObjFile()
        {
            if (CurrentModelPackage == null)
                MessageBox.Show("Nothing to export!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void ExportModelPackage()
        {
            if (CurrentModelPackage == null)
                MessageBox.Show("Nothing to export!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
            else
            {
                string path = Path.Combine(Settings.ExportDirectory, Path.GetFileName(CurrentModelFile.FileName));
                
                AT.Log("Compiling ModelPackage...");
                CurrentModelPackage.GetInterface().Save();
                
                AT.Log("Exporting ModelPackage...");
                CurrentModelPackage.ModelFile.Save(path, false);
                AT.Log("Done!");
                
                string msg = String.Format("Successfully exported to '{0}'!", path);
                MessageBox.Show(msg, "ModelPackage Exporter", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportVehicleHierarchyVPK()
        {
            var modelFile = CurrentModelFile as Driv3rVehiclesFile;

            if (CurrentModelPackage == null || CurrentModelFile == null)
            {
                MessageBox.Show("Nothing to export!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
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
        
        private void ExportTexture(ITextureData texture)
        {
            var path = Path.Combine(Settings.ExportDirectory, String.Format("{0}.dds", texture.UID));

            FileManager.WriteFile(path, texture.Buffer);

            MessageBox.Show($"Successfully exported to '{path}'!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ReplaceTexture(ITextureData texture)
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
                var ddsBuffer = File.ReadAllBytes(replaceTexture.FileName);

                var texRef = TextureCache.GetTexture(texture);
                texRef.SetBuffer(ddsBuffer);
                
                Viewer.UpdateActiveModel();

                // reload the active texture if necessary
                if (IsViewWidgetVisible)
                    CurrentViewWidget.SetTexture(texture);
            }
        }

        private void ReplaceTexture(object sender, RoutedEventArgs e)
        {
            var item = ((sender as FrameworkElement).DataContext) as TextureTreeItem;

            if (item != null)
            {
                var tex = item.Texture;
                ReplaceTexture(tex);

                UpdateViewWidgets();
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
        
        // requiring this is probably a design flaw...but it works :P
        volatile bool m_deferSelectionChange = false;

        private void OnModelPackageSelected(object sender, EventArgs e)
        {
            m_deferSelectionChange = true;
            OnPropertyChanged("PartsGroups");
            m_deferSelectionChange = false;

            if (CurrentModelPackage != null && CurrentModelFile.HasModels)
            {
                Groups.SelectedIndex = 0;
            }
            else
            {
                Groups.SelectedIndex = -1;
            }
        }

        private void OnModelPackageItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (Viewer.SelectedModel != null)
                Viewer.SelectedModel = null;

            TextureCache.FlushIfNeeded();

            int index = Packages.SelectedIndex;

            AT.CurrentState.SelectedModelPackage = (index != -1) ? ModelPackages[index] : null;
            ResetViewWidgets();
        }

        private void OnPartsGroupItemSelected(object sender, EventArgs e)
        {
            if (m_deferSelectionChange)
                return;

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() => {
                if (Groups.SelectedIndex == -1)
                {
                    Viewer.RemoveActiveModel();
                    ResetLODButtons();

                    // select the first group
                    m_deferSelectionChange = true;
                    Groups.SelectedIndex = 0;
                }

                Viewer.SetActiveModel(SelectedPartsGroup.Parts);
                UpdateRenderingOptions();
            }));

            if (m_deferSelectionChange)
                m_deferSelectionChange = false;
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
                    list.SelectedItem = item;
                    return true;
                }
            }
            return false;
        }
        
        /*
            This will check the globals (if applicable),
            then the current model package for the material/texture
        */
        private bool OnQuerySelection<TQueryInput, TQueryObject>(Func<TQueryInput, TQueryObject, bool> fnQuery,
            TQueryInput queryGlobals, TQueryInput queryOther, object obj, int tabIdx = -1)
            where TQueryInput : class
            where TQueryObject : class
        {
            var queryObj = obj as TQueryObject;

            if (queryObj != null)
            {
                if (tabIdx != -1)
                    MoveToTab(tabIdx);

                return (AT.CurrentState.CanUseGlobals && fnQuery(queryGlobals, queryObj))
                || fnQuery(queryOther, queryObj);
            }

            // no material!
            return false;
        }
        
        private void Initialize()
        {
            Settings.Verify();
            
            AppDomain.CurrentDomain.UnhandledException += (o, e) => {
                var exception = e.ExceptionObject as Exception;
                var sb = new StringBuilder();

                sb.AppendLine($"A fatal error has occurred! The program will now close.");
                sb.AppendLine();

                // this is literally useless
                if (exception is TargetInvocationException)
                    exception = exception.InnerException;

                var stk = new StackTrace(exception, true);
                var stkFrame = stk.GetFrame(0);

                sb.AppendLine($"{exception.Message}");
                sb.AppendLine();
                
                sb.AppendLine($"===== Stack trace =====");
                sb.AppendLine($"{stk.ToString()}");
                sb.AppendLine($"=======================");

                if (MessageBox.Show(sb.ToString(), "Antilli - ERROR!", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
                    Environment.Exit(1);
            };

            InitializeComponent();

            AT.CurrentState.PropertyChanged += (o, e) => {
                //Debug.WriteLine($">> State change: '{e.PropertyName}'");
                OnPropertyChanged(e.PropertyName);
            };
            
            AT.CurrentState.MaterialSelectQueried += (o, e) => {
                var selected = OnQuerySelection<TreeView, IMaterialData>(TrySelectMaterial, GlobalMaterialsList, MaterialsList, o, 1);

                if (!selected)
                    MessageBox.Show("No material found!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            AT.CurrentState.TextureSelectQueried += (o, e) => {
                var selected = OnQuerySelection<ListBox, ITextureData>(TrySelectTexture, GlobalTextureList, TextureList, o, 2);

                if (!selected)
                    MessageBox.Show("No texture found!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            AT.CurrentState.ModelPackageSelected += OnModelPackageSelected;

            DSC.ProgressUpdated += (o, e) => {
                var progress_str = e.Message;

                if (e.Progress > -1)
                    progress_str += $" [{Math.Round(e.Progress):F1}%]";
                
                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new ThreadStart(() => {
                    Debug.WriteLine($"[INFO] {progress_str}");
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
            
            Packages.SelectionChanged += OnModelPackageItemSelected;
            Groups.SelectionChanged += OnPartsGroupItemSelected;
            
            foreach (var child in LODButtons.Children)
            {
                var lodBtn = (child as RadioButton);

                if (lodBtn != null)
                {
                    var lod = int.Parse((string)lodBtn.Tag);
                    m_lodBtnRefs[lod] = lodBtn;

                    lodBtn.Click += (o, e) => {
                        if (lodBtn.IsChecked ?? false)
                            Viewer.LevelOfDetail = lod;
                    };
                }
            }
            
            BlendWeights.Checked += (o, e) => Viewer.ToggleBlendWeights();
            BlendWeights.Unchecked += (o, e) => Viewer.ToggleBlendWeights();
            
            fileOpen.Click += (o, e) => OnFileOpenClick();
            
            fileExit.Click += (o, e) => Environment.Exit(0);
            
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
            Thread.CurrentThread.CurrentCulture = AT.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = AT.CurrentCulture;

            Initialize();
        }
    }
}
