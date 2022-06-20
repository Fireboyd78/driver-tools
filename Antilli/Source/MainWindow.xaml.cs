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
using DSCript.Menus;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AntilliWindow
    {
        public int CurrentTab
        {
            get { return AT.CurrentState.CurrentTab; }
            set { AT.CurrentState.CurrentTab = value; }
        }

        public bool CanUseBlendWeights
        {
            get { return AT.CurrentState.CanUseBlendWeights; }
        }

        public IModelFile CurrentModelFile
        {
            get { return AT.CurrentState.ModelFile; }
        }

        public ModelPackage CurrentModelPackage
        {
            get { return AT.CurrentState.SelectedModelPackage; }
        }

        public List<ModelPackage> ModelPackages
        {
            get { return AT.CurrentState.ModelPackages; }
        }

        public ModelContainerListItem SelectedModelContainer
        {
            get { return Containers.SelectedItem as ModelContainerListItem; }
        }

        public List<ModelContainerListItem> ModelContainers
        {
            get
            {
                if (CurrentModelPackage == null || CurrentModelPackage.Models == null)
                    return null;

                List<ModelContainerListItem> items = new List<ModelContainerListItem>();

                Model curModel = null;

                foreach (var model in CurrentModelPackage.Models)
                {
                    if (curModel == null || curModel.UID != model.UID)
                    {
                        items.Add(new ModelContainerListItem(CurrentModelPackage, model));
                        curModel = model;
                    }
                }

                return items;
            }
        }

        public bool ShowWaitCursor
        {
            set { Mouse.OverrideCursor = (value) ? Cursors.Wait : null; }
        }

        public bool IsFileOpened
        {
            get { return AT.CurrentState.IsFileOpened; }
        }

        public bool IsFileDirty
        {
            get { return AT.CurrentState.IsFileDirty; }
            set { AT.CurrentState.IsFileDirty = value; }
        }

        public bool CanSaveFile
        {
            get { return AT.CurrentState.CanSaveFile; }
        }

        private void SetCurrentFile(string filename)
        {
            SubTitle = filename;
            AT.CurrentState.UpdateFileStatus();
        }

        private void CloseFile(bool exiting)
        {
            AT.CurrentState.FreeModels();

            IsFileDirty = false;

            SetCurrentFile(null);
        }

        private void LoadDriv3rVehicles(string filename)
        {
            var vehicleFile = new Driv3rVehiclesFile(filename);

            var city = Driv3r.GetCityFromFileName(filename);
            var vgtFile = "";

            if (city != Driv3r.City.Unknown)
                vgtFile = Driv3r.GetVehicleGlobals(city);

            if (!File.Exists(vgtFile))
            {
                // try locating it in the same directory as models file
                // vgtFile has 'Vehicles' in it so we need parent dir of model file!
                vgtFile = Path.GetFullPath( Path.Combine($"{Path.GetDirectoryName(filename)}\\..\\", vgtFile) );

                // still not found!?
                if (!File.Exists(vgtFile))
                {
                    // ask the user to choose one
                    var dlg = FileManager.GetOpenDialog(GameType.Driv3r, ".vgt");

                    dlg.Title = "Please select a global vehicle textures file.";
                    dlg.InitialDirectory = Path.GetDirectoryName(filename);
                    dlg.CheckFileExists = true;
                    dlg.CheckPathExists = true;

                    // this won't be set if the user cancels/selects an invalid file
                    vgtFile = null;

                    if (dlg.ShowDialog() ?? false)
                        vgtFile = dlg.FileName;
                }
            }
            
            // load global textures if we have them
            if (!String.IsNullOrEmpty(vgtFile))
            {
                // continue normally
                vehicleFile.VehicleGlobals = new GlobalTexturesFile(vgtFile);
            }

            AT.CurrentState.ModelFile = vehicleFile;
            AT.CurrentState.CanUseGlobals = vehicleFile.HasGlobals;
        }

        private void OnFileOpened(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();
            var filter = FileManager.FindFilter(extension, GameType.Driv3r, (GameFileFlags.Models | GameFileFlags.Textures));

            if (filter.Flags == GameFileFlags.None)
            {
                filter = FileManager.FindFilter(extension, GameType.DriverPL, (GameFileFlags.Models | GameFileFlags.Textures | GameFileFlags.Resource));

                if (filter.Flags == GameFileFlags.None)
                {
                    MessageBox.Show("Unsupported file type selected, please try another file.", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var timer = new Stopwatch();
            timer.Start();

            IModelFile modelFile = new ModelFile();
            var setupModels = true;

            PackageManager.Clear();

            switch (extension)
            {
            case ".dam":
                modelFile.SpoolerLoaded += (s, e) => {
                    if (s.Context == ChunkType.CharacterSkeletons)
                    {
                        var skel = s as SpoolableBuffer;

                        if (skel == null)
                            return;

                        var result = MessageBox.Show("Would you like to dump the skeleton data? :)", "Antilli", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            var sb = new StringBuilder();

                            var fmtVec = new Func<Vector4, string>((v) => {
                                var fmtStr = "{0,7:F4}";

                                return String.Format("[{0},{1},{2},{3}]",
                                    String.Format(fmtStr, v.X),
                                    String.Format(fmtStr, v.Y),
                                    String.Format(fmtStr, v.Z),
                                    String.Format(fmtStr, v.W)
                                    );
                            });

                            using (var ms = skel.GetMemoryStream())
                            {
                                var count = ms.ReadInt32();
                                var unk = ms.ReadInt32();

                                sb.AppendLine($"# SkeletonPackage");
                                sb.AppendLine($"# Unk: {unk:X8}");
                                sb.AppendLine();

                                for (int i = 0; i < count; i++)
                                {
                                    // make sure we're 16-bit aligned first
                                    ms.Align(16);

                                    var skelName = ms.ReadString(40);

                                    ms.Position += 4;
                                    var nBones = ms.ReadInt32();

                                    var boneLookup = new string[nBones];

                                    var bonesOffset = ms.Position;
                                    var parentsOffset = (bonesOffset + (nBones * 0xB0));

                                    sb.AppendLine($"Skeleton : {skelName} {{");

                                    for (int b = 0; b < nBones; b++)
                                    {
                                        ms.Position = (bonesOffset + (b * 0xB0));

                                        var boneName = ms.ReadString(40);
                                        ms.Position += 8;

                                        var tr1X = ms.Read<Vector4>();
                                        var tr1Y = ms.Read<Vector4>();
                                        var tr1Z = ms.Read<Vector4>();
                                        var tr1R = ms.Read<Vector4>();

                                        var tr2X = ms.Read<Vector4>();
                                        var tr2Y = ms.Read<Vector4>();
                                        var tr2Z = ms.Read<Vector4>();
                                        var tr2R = ms.Read<Vector4>();

                                        boneLookup[b] = boneName;

                                        ms.Position = (parentsOffset + (b * 4));

                                        var parentIdx = ms.ReadInt32();
                                        var parentName = "ROOT";

                                        // because idk the format
                                        if (parentIdx <= 65535)
                                            parentIdx = (short)parentIdx;

                                        sb.AppendLine($"  Bone : {boneName} {{");

                                        if (parentIdx != -1)
                                            parentName = boneLookup[parentIdx];

                                        sb.AppendLine($"    parent = {parentName} ");
                                        sb.AppendLine($"    transform[1] = [");
                                        sb.AppendLine($"      {fmtVec(tr1X)}, ");
                                        sb.AppendLine($"      {fmtVec(tr1Y)}, ");
                                        sb.AppendLine($"      {fmtVec(tr1Z)}, ");
                                        sb.AppendLine($"      {fmtVec(tr1R)}, ");
                                        sb.AppendLine($"    ]");
                                        sb.AppendLine($"    transform[2] = [");
                                        sb.AppendLine($"      {fmtVec(tr2X)}, ");
                                        sb.AppendLine($"      {fmtVec(tr2Y)}, ");
                                        sb.AppendLine($"      {fmtVec(tr2Z)}, ");
                                        sb.AppendLine($"      {fmtVec(tr2R)}, ");
                                        sb.AppendLine($"    ]");
                                        sb.AppendLine($"  }}");

                                        if ((b + 1) < nBones)
                                            sb.AppendLine();
                                    }

                                    sb.AppendLine($"}}\r\n");
                                }
                            }

                            var boneFile = Path.Combine(Settings.ExportDirectory, $"{Path.GetFileNameWithoutExtension(filename)}_skeletons.txt");

                            File.WriteAllText(boneFile, sb.ToString());

                            MessageBox.Show($"Bone data exported to '{boneFile}'!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                };
                break;
            case ".mec":
                var menuFile = new MenuPackageFile();
                menuFile.FileLoadEnd += (o, e) => {
                    var menuDef = menuFile.MenuData;

                    var xmlFile = Path.ChangeExtension(filename, ".mec.xml");

                    if (AskUserOption("Would you like to dump the XML menu tree?"))
                        menuDef.WriteTo(xmlFile);

                    if (File.Exists(xmlFile))
                    {
                        if (AskUserOption("Would you like to reload using the XML menu tree?"))
                            menuDef.LoadXml(xmlFile);
                    }

                    if (AskUserOption("Rebuild a second copy of the menu data?"))
                    {
                        // force an entire rebuild!
                        menuDef.NotifyChanges(true);
                        menuFile.Save(Path.ChangeExtension(filename, ".mec.out"), false);
                    }
                };
                modelFile = menuFile;
                break;
            case ".vvs":
            case ".vvv":
                LoadDriv3rVehicles(filename);
                setupModels = false;
                break;
            }

            if (setupModels)
            {
                if (filename.IndexOf("vehicles", StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    var vehiclesFile = new SpooledVehiclesFile(filename);
                    modelFile = vehiclesFile;

                    AT.CurrentState.ModelFile = vehiclesFile;
                    AT.CurrentState.CanUseGlobals = vehiclesFile.HasGlobals;
                }
                else
                {
                    modelFile.Load(filename);

                    AT.CurrentState.ModelFile = modelFile;
                    AT.CurrentState.CanUseGlobals = false;
                }
            }

            SetCurrentFile(filename);

            // pre-cache all textures from the package manager (like an idiot)
            foreach (var package in PackageManager.EnumerateAll())
            {
                foreach (var texture in package.Textures)
                {
                    // make a non-freeable reference so it won't get freed by accident
                    TextureCache.GetTexture(texture);
                }
            }

            if (CurrentModelFile.HasModels)
            {
                // R.I.P. (2013 - 2020)
                //Viewer.Viewport.InfiniteSpin = Settings.InfiniteSpin;
                Packages.SelectedIndex = 0;
            }
            
            timer.Stop();

            AT.Log($"Loaded model file in {timer.ElapsedMilliseconds}ms.");
        }

        private void OnFileOpenClick()
        {
            var dialog = FileManager.OpenDialog;

            if (dialog.ShowDialog() ?? false)
            {
                dialog.InitialDirectory = Path.GetDirectoryName(dialog.FileName);
                OnFileOpened(dialog.FileName);
            }
        }

        private void OnFileOpenGameClick(GameType gameType)
        {
            var dialog = FileManager.GetOpenDialog(gameType);

            if (dialog.ShowDialog() ?? false)
                OnFileOpened(dialog.FileName);
        }

        private bool AskUserPrompt(string message)
        {
            return MessageBox.Show(message, "Antilli", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        private bool AskUserOption(string message)
        {
            return MessageBox.Show(message, "Antilli", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        private void OnFileSaveClick(bool saveAs)
        {
            var filename = CurrentModelFile.FileName;

            if (saveAs)
            {
                var name = Path.GetFileName(filename);
                var ext = Path.GetExtension(filename);

                var saveDlg = new SaveFileDialog() {
                    AddExtension = true,
                    DefaultExt = ext,
                    FileName = name,
                    InitialDirectory = Settings.ExportDirectory,
                    Title = "Please enter a filename",
                    ValidateNames = true,
                    OverwritePrompt = true,
                };

                if (saveDlg.ShowDialog() ?? false)
                {
                    filename = saveDlg.FileName;

                    ShowWaitCursor = true;

                    if (CurrentModelFile.Save(filename))
                    {
                        IsFileDirty = false;

                        SetCurrentFile(filename);

                        MessageBox.Show($"Successfully saved to '{filename}'!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to save '{filename}'!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    ShowWaitCursor = false;
                }
            }
            else
            {
                if (AskUserPrompt($"All pending changes will be saved to '{CurrentModelFile.FileName}'. Do you wish to OVERWRITE the original file? (NO BACKUPS WILL BE CREATED)"))
                {
                    ShowWaitCursor = true;

                    if (CurrentModelFile.Save())
                    {
                        IsFileDirty = false;
                        MessageBox.Show($"Successfully saved changes to '{filename}'!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to save '{filename}'! No changes were made.", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    ShowWaitCursor = false;
                }
            }
        }

        private void OnFileCloseClick()
        {
            bool actuallyClose = true;

            if (IsFileDirty && !AskUserPrompt("All pending changes will be lost. Are you sure?"))
                actuallyClose = false;

            if (actuallyClose)
                CloseFile(false);
        }

        private void OnFileExitClick()
        {
            bool actuallyExit = true;

            if (IsFileDirty && !AskUserPrompt("You still have unsaved changes made to the file. Are you sure you want to exit?"))
                actuallyExit = false;

            if (actuallyExit)
            {
                CloseFile(true);
                Close();
                //Environment.Exit(0);
            }
        }

        private RadioButton[] m_lodBtnRefs = new RadioButton[7];

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

            if (SelectedModelContainer != null)
            {
                foreach (var model in SelectedModelContainer.Models)
                {
                    // run this check once to prevent unnecessary slowdown
                    if (checkBlendWeights)
                    {
                        checkBlendWeights = false;
                        AT.CurrentState.CanUseBlendWeights = (model.VertexType == 5);
                    }

                    // count all possible LODs
                    for (int i = 0; i < 7; i++)
                    {
                        var lod = model.Lods[i];

                        if (lod == null)
                            continue;

                        if (lod.Instances.Count > 0)
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

        /*
        private void ConnectToBlender()
        {
            if (AT.CurrentState.BlenderClient == null)
            {
                var client = new AntilliClient("localhost", 33759);

                ShowWaitCursor = true;

                if (client.Connect())
                {
                    AT.CurrentState.BlenderClient = client;

                    MessageBox.Show("Connection successfully established!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Could not establish a connection!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                ShowWaitCursor = false;
            }
            else
            {
                MessageBox.Show("A connection to Blender has already been established!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SendCommandToBlender()
        {
            var client = AT.CurrentState.BlenderClient;

            if (client != null)
            {
                var inputBox = new MKInputBox("Blender Sender!", "Enter command:") {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (inputBox.ShowDialog() ?? false)
                {
                    var message = inputBox.InputValue;

                    Trace.WriteLine($"Sending message to Blender: '{message}'");
                    client.Send(inputBox.InputValue);
                }
            }
        }
        */

        private void ImportAntilliScene()
        {
            string[] message = {
                "This experimental feature will REPLACE the selected model package ENTIRELY!",
                "",
                "Materials haven't been implemented yet, so you will LOSE ALL MATERIALS !!!",
                "",
                "Maybe you can still have some fun anyways? ;)",
            };

            MessageBox.Show(string.Join("\r\n", message), "Scene Importer - WARNING!", MessageBoxButton.OK, MessageBoxImage.Warning);

            var dialog = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "Antilli Scene|*.ais",
                ValidateNames = true,
            };

            if (dialog.ShowDialog() ?? false)
            {
                var scene = new AntilliScene(dialog.FileName);

                var chooser = new MKChooserBox("Scene Importer", "Select game format:", new[] {
                    "Driv3r",
                    "Driver: Parallel Lines",
                }) {
                    ShowOptionCheckbox = true,
                    OptionName = "Force retarget",
                    OptionToolTip = "Converts the model to the selected format, regardless of settings.",
                };

                if (chooser.ShowDialog() ?? false)
                {
                    //
                    // TODO: make this more robust
                    //
                    var version = (chooser.SelectedIndex == 0) ? 6 : 1;

                    var package = scene.ToModelPackage(PlatformType.PC, version);

                    // force a retarget?
                    if (chooser.IsOptionChecked)
                        package = ModelConverter.Convert(package, version);

                    var modelFile = AT.CurrentState.ModelFile;
                    var modelPackage = AT.CurrentState.SelectedModelPackage;

                    // deselect the model package
                    AT.CurrentState.SelectedModelPackage = null;

                    // free old models/materials
                    modelPackage.FreeModels();
                    modelPackage.FreeMaterials();

                    //
                    // **** REPLACE THE OLD PACKAGE WITH OUR NEW ONE ****
                    //

                    var resource = (ISpoolableResource)modelPackage;
                    var targetResource = (ISpoolableResource)package;

                    // save our new model's spooler...
                    targetResource.Save();

                    // free up its shit..
                    package.FreeModels();
                    package.FreeMaterials();

                    // ... then replace the existing spooler with our new one! ;)
                    // (we don't need our imported package anymore, we've stolen the selected one)
                    resource.Spooler = targetResource.Spooler;

                    // replace the UID and shit..
                    modelPackage.UID = package.UID;
                    modelPackage.Version = package.Version;
                    modelPackage.Flags = package.Flags;

                    // reselect the model package (it will be reloaded)
                    AT.CurrentState.SelectedModelPackage = modelPackage;

                    // notify our changes
                    AT.CurrentState.NotifyFileChange(modelFile);
                }
            }
        }

        private void ExportAntilliScene()
        {
            var prompt = AT.CurrentState.CreateDialog<ExportModelDialog>(true, false);

            if (!SelectedModelContainer.IsNull)
                prompt.FolderName = SelectedModelContainer.Name.Replace(':', '-');

            if (prompt.ShowDialog() ?? false)
            {
                var flags = prompt.Flags;
                var name = prompt.FolderName;

                var outDir = Path.Combine(Settings.ExportDirectory, name);

                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                ShowWaitCursor = true;

                var scene = new AntilliScene(CurrentModelPackage);

                using (var ms = new MemoryStream())
                {
                    scene.Serialize(ms);

                    var buffer = ms.ToArray();

                    File.WriteAllBytes(Path.Combine(outDir, $"{name}.ais"), buffer);
                }

                ShowWaitCursor = false;
            }
        }

        private void ImportModelPackage()
        {
            // TODO: add support for new packages, replacing individual model containers
            var modelFile = AT.CurrentState.ModelFile;
            var modelPackage = AT.CurrentState.SelectedModelPackage;

            if (!modelPackage.IsOwnModelPackage())
            {
                MessageBox.Show("Sorry, cannot replace individual model containers at this time.",
                                "Package Importer", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var failed = false;

            var dialog = new OpenFileDialog()
            {
                Filter = "Model Package|*.*",
                CheckFileExists = true,
                CheckPathExists = true,
                ValidateNames = true,
            };

            if (dialog.ShowDialog() ?? false)
            {
                var chunker = new FileChunker();
                var packages = new List<ModelPackage>();

                var level = ModelPackage.LoadLevel;

                //
                // main importing logic
                //
                chunker.SpoolerLoaded += (spooler, e) =>
                {
                    switch ((ChunkType)spooler.Context)
                    {
                    case ChunkType.ModelPackagePC_X:
                        if (spooler.Version == 3)
                            break; // DSF - unhandled for now

                        goto case ChunkType.ModelPackagePC;

                    case ChunkType.ModelPackagePC:
                    case ChunkType.ModelPackagePS2:
                    case ChunkType.ModelPackageXbox:
                    case ChunkType.ModelPackageWii:
                        // load headers only
                        ModelPackage.LoadLevel = ModelPackageLoadLevel.FastLoad;

                        // retrieve a COPY of the spooler so we can attach it later
                        var copy = CopyCatFactory.GetCopy(spooler, CopyClassType.DeepCopy);
                        var package = SpoolableResourceFactory.AsResource<ModelPackage>(copy, true);

                        packages.Add(package);

                        // restore load level
                        ModelPackage.LoadLevel = level;
                        break;
                    }
                };

                if (chunker.Load(dialog.FileName))
                {
                    ModelPackage package = null;

                    if (packages.Count > 1)
                    {
                        var packageNames = new List<String>();

                        for (int i = 0; i < packages.Count; i++)
                        {
                            var pkg = packages[i];
                            var spooler = pkg.AsSpooler();

                            var name = "";

                            if (!String.IsNullOrEmpty(spooler.Description))
                                name = $"[{spooler.Description}]";
                            else
                                name = $"[{pkg.UID:X8} : {pkg.Handle:X4}]";

                            packageNames.Add($"{i + 1}. {name}");
                        }

                        var pkgBox = new MKChooserBox("Package Importer", "Select a package to import:", packageNames.ToArray());

                        do
                        {
                            if (pkgBox.ShowDialog() ?? false)
                            {
                                var index = pkgBox.SelectedIndex;

                                // package found, break out
                                package = packages[index];
                                break;
                            }
                            
                            if (MessageBox.Show("No package was selected. Try again?",
                                "Package Importer", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                            {
                                failed = true;
                            }
                        } while (!failed);
                    }
                    else
                    {
                        // possibly one package or none at all
                        package = packages.FirstOrDefault();

                        if (package == null)
                            failed = true;
                    }

                    // check a few things before proceeding
                    if (!failed)
                    {
                        if (package.Platform != PlatformType.PC)
                        {
                            MessageBox.Show($"Sorry, cannot import a {package.Platform} model package.",
                                "Package Importer", MessageBoxButton.OK, MessageBoxImage.Error);

                            failed = true;
                        }

                        //
                        // TODO: Add support for these
                        //
                        if (!package.IsOwnModelPackage())
                        {
                            MessageBox.Show("Sorry, cannot import a model package that contains multiple model containers at this time.",
                                "Package Importer", MessageBoxButton.OK, MessageBoxImage.Error);

                            failed = true;
                        }
                    }

                    // if everything is good so far, prepare to import it!
                    if (!failed)
                    {
                        var fmtBox = new MKChooserBox("Package Importer", "Select game format:", new[] {
                            "Driv3r",
                            "Driver: Parallel Lines",
                        }) {
                            ShowOptionCheckbox = true,
                            OptionName = "Force retarget",
                            OptionToolTip = "Converts the model to the selected format, regardless of settings.",
                        };

                        fmtBox.SelectedIndex = (package.Version == 6) ? 0 : 1;

                        if (fmtBox.ShowDialog() ?? false)
                        {
                            //
                            // TODO: make this more robust
                            //
                            var version = (fmtBox.SelectedIndex == 0) ? 6 : 1;

                            // use UID of selected model package
                            var uid = modelPackage.UID;

                            // now fully load the model package into memory
                            ModelPackage.LoadLevel = ModelPackageLoadLevel.Default;

                            SpoolableResourceFactory.Load(package);
                            ModelPackage.LoadLevel = level;

                            // retarget?
                            if (fmtBox.IsOptionChecked || (version != package.Version))
                            {
                                package = ModelConverter.Convert(package, version, uid);
                            }
                            else
                            {
                                if (version == 1)
                                {
                                    if (modelPackage.HasModels)
                                    {
                                        var modelUID = modelPackage.Models[0].UID;

                                        // retarget model containers
                                        foreach (var model in package.Models)
                                            model.UID = modelUID;
                                    }

                                    // retarget materials to correct uid
                                    foreach (var submodel in package.SubModels)
                                    {
                                        var material = submodel.Material;

                                        if (material.UID == package.UID || material.UID == 0xFFFD)
                                            material.UID = (ushort)uid;

                                        // actually update the material...
                                        submodel.Material = material;
                                    }
                                }

                                // set new uid
                                package.UID = uid;
                            }

                            // deselect the model package
                            AT.CurrentState.SelectedModelPackage = null;

                            // free old models/materials
                            modelPackage.FreeModels();
                            modelPackage.FreeMaterials();

                            //
                            // **** REPLACE THE OLD PACKAGE WITH OUR NEW ONE ****
                            //
                            
                            var spooler = modelPackage.AsSpooler();
                            var parent = spooler.Parent;

                            var index = parent.Children.IndexOf(spooler);

                            parent.Children.Remove(spooler);
                            parent.Children.Insert(index, package.AsSpooler(true));
                            
                            index = modelFile.Packages.IndexOf(modelPackage);
                            modelFile.Packages[index] = package;

                            // reselect the model package (it will be reloaded)
                            AT.CurrentState.SelectedModelPackage = package;

                            // notify our changes
                            AT.CurrentState.NotifyFileChange(modelFile);
                        }
                    }
                }
            }

            if (failed)
                MessageBox.Show("Failed to find a suitable model package to import. No changes were made.", "Package Importer", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportModelPackage()
        {
            var prompt = AT.CurrentState.CreateDialog<ExportModelDialog>(true, false);

            if (!SelectedModelContainer.IsNull)
                prompt.FolderName = SelectedModelContainer.Name.Replace(':', '-');

            prompt.Format = (CurrentModelPackage.Version == 6) ? ExportModelFormat.Driv3r : ExportModelFormat.DriverParallelLines;
            prompt.ShowFormatSelector = true;
            
            if (prompt.ShowDialog() ?? false)
            {
                var flags = prompt.Flags;
                var name = prompt.FolderName;

                var path = Path.Combine(Settings.ModelsDirectory, name);

                var targetVersion = (prompt.Format == ExportModelFormat.Driv3r) ? 6 : 1;
                
                //ModelPackage package = null;
                Spooler spooler = null;

                var package = CurrentModelPackage;
                var packageUID = CurrentModelPackage.UID;
                var packageName = "package";

                var vvsHack = false;

                if (MessageBox.Show("Apply spooled vehicle (VVS) hack?",
                            "Package Exporter", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    // HACK: assume it's a vehicle!
                    packageUID = 0xFF;
                    vvsHack = true;
                }

                var changed = false;
                
                ShowWaitCursor = true;

                if (!prompt.ExportAll && !package.IsOwnModelPackage(SelectedModelContainer.UID))
                {
                    package = package.ExtractModelsByUID(SelectedModelContainer.UID, packageUID);
                    changed = true;
                }

                if (targetVersion != package.Version)
                {
                    // convert to a new package
                    var converted = ModelConverter.Convert(package, targetVersion, packageUID);

                    // cleanup our previous package?
                    if (changed)
                    {
                        package.FreeModels();
                        package.FreeMaterials();
                    }

                    // set the new package
                    package = converted;
                    packageName = "converted_package";

                    changed = true;
                }
                
                if (changed)
                {
                    // save our custom model package
                    spooler = package.AsSpooler(true);

                    // clean up our mess
                    package.FreeModels();
                    package.FreeMaterials();
                }
                else
                {
                    // get a copy of the original model package
                    spooler = CopyCatFactory.GetCopy(package.AsSpooler(), CopyClassType.DeepCopy);
                }

                var filepak = ChunkTemplates.UnifiedPackage;

                // get the hierarchy too!
                var modelFile = CurrentModelFile as IVehiclesFile;

                if (modelFile != null)
                {
                    var idx = (!modelFile.HasVirtualVehicles) ? modelFile.Packages.IndexOf(CurrentModelPackage) : Containers.SelectedIndex;

                    var hierarchy = modelFile.Hierarchies[idx];

                    var copyHierarchy = SpoolableResourceFactory.Clone(hierarchy, true);

                    if (modelFile.HasVirtualVehicles && idx != 0)
                    {
                        // relocate models
                        var lowestModelIndex = -1;
                        var parts = new List<VehiclePartData>();

                        // first pass: determine the first model index
                        foreach (var part in copyHierarchy.Parts)
                        {
                            if (part.ModelId != 255)
                            {
                                if (lowestModelIndex == -1 || (part.ModelId < lowestModelIndex))
                                    lowestModelIndex = part.ModelId;
                            }

                            parts.Add(part);
                        }

                        // second pass: relocate the models
                        for (int p = 0; p < copyHierarchy.Parts.Count; p++)
                        {
                            var newPart = parts[p];

                            if (newPart.ModelId == 255)
                                continue;
                            
                            newPart.ModelId -= (byte)lowestModelIndex;

                            copyHierarchy.Parts[p] = newPart;
                        }
                    }

                    copyHierarchy.Version = (targetVersion == 6) ? 0 : 1;

                    // just in case DPL on Xbox ever strikes again... ;)
                    if (copyHierarchy.Version == 0 && copyHierarchy.Platform == PlatformType.Xbox)
                        copyHierarchy.Version = 1;

                    if (vvsHack)
                    {
                        filepak.Context = copyHierarchy.UID;
                        filepak.Description = "Individual model";
                    }

                    filepak.Children.Add(copyHierarchy.AsSpooler(true));
                }

                filepak.Children.Add(spooler);

                var chunker = new FileChunker();
                chunker.Children.Add(filepak);

                var outDir = Path.Combine(Settings.ExportDirectory, name);

                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                var filename = Path.Combine(outDir, $"{packageName}.vvv");

                if (chunker.Save(filename))
                    MessageBox.Show($"Exported to '{filename}' successfully!", "Package Exporter", MessageBoxButton.OK, MessageBoxImage.Information);

                ShowWaitCursor = false;
            }
        }

        private void ExportWavefrontOBJ()
        {
            var prompt = AT.CurrentState.CreateDialog<ExportModelDialog>(true, false);

            if (!SelectedModelContainer.IsNull)
                prompt.FolderName = SelectedModelContainer.Name.Replace(':', '-');

            if (prompt.ShowDialog() ?? false)
            {
                var flags = prompt.Flags;
                var name = prompt.FolderName;

                var path = Path.Combine(Settings.ModelsDirectory, name);

                ShowWaitCursor = true;

                if (prompt.ExportAll)
                {
                    int partIdx = 0;
                    Model curPart = null;

                    foreach (var part in CurrentModelPackage.Models)
                    {
                        if (curPart == null || curPart.UID != part.UID)
                        {
                            var filename = $"{partIdx:D4}_{part.UID.High:X8}_{part.UID.Low:X8}";

                            OBJFile.Export(path, filename, CurrentModelPackage, part.UID, prompt.SplitByMaterial, prompt.BakeTransforms);

                            curPart = part;
                            partIdx++;
                        }
                    }

                    var modelCountStr = String.Format($"{partIdx} {{0}}", (partIdx > 1) ? "models" : "model");

                    MessageBox.Show($"Successfully exported {modelCountStr} to '{path}'!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var part = SelectedModelContainer;
                    var filename = $"{part.UID.High:X8}_{part.UID.Low:X8}";

                    if (OBJFile.Export(path, filename, CurrentModelPackage, part.UID, prompt.SplitByMaterial, prompt.BakeTransforms) == ExportResult.Success)
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

                ShowWaitCursor = false;
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

            var idx = (!modelFile.HasVirtualVehicles) ? modelFile.Packages.IndexOf(CurrentModelPackage) : Containers.SelectedIndex;

            var hierarchy = modelFile.Hierarchies[idx];

            var dir = Settings.ExportDirectory;
            var path = String.Format("{0}\\{1}_{2}.vpk", dir, Path.GetFileName(CurrentModelFile.FileName).Replace('.', '_'), hierarchy.UID);

            hierarchy.SaveVPK(path);

            var msg = String.Format("Successfully exported VPK file to '{0}'!", path);

            MessageBox.Show(msg, "VehicleHierarchy VPK Exporter", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExtractAllTexturesFromDirectory()
        {
            var saveDlg = new FolderSelectDialog()
            {
                Title = "Select model files directory",
            };

            if (saveDlg.ShowDialog()
                && MessageBox.Show("This will open EVERY model file in the directory and extract ALL textures from them. Continue with choosing an output folder?",
                    "Texture Extractor", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var inputBox = new MKInputBox("Texture Extractor", "Enter output folder name:", $"{Path.GetFileName(saveDlg.SelectedPath)}")
                {
                    Owner = this,
                    OptionName = "Don't create subfolders",
                    ShowOptionCheckbox = true,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (inputBox.ShowDialog() ?? false)
                {
                    var prefixes = false;

                    if (MessageBox.Show("Do you want to prefix filenames with the package UID and texture index? This can make it easier to browse them in a file explorer.",
                        "Texture Extractor", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        prefixes = true;
                    }

                    var folder = inputBox.InputValue;
                    var path = Path.Combine(Settings.TexturesDirectory, folder);

                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    var subfolders = !inputBox.IsOptionChecked;

                    // can be changed if subfolders are allowed
                    var outDir = path;

                    var chunker = new FileChunker();
                    
                    var total = 0;
                    var count = 0;

                    var level = ModelPackage.LoadLevel;

                    // load materials only
                    ModelPackage.LoadLevel = ModelPackageLoadLevel.Materials;

                    //
                    // main texture exporting logic
                    //
                    chunker.SpoolerLoaded += (sender, e) =>
                    {
                        switch ((ChunkType)sender.Context)
                        {
                        case ChunkType.ModelPackagePC_X:
                            if (sender.Version == 3)
                                break; // DSF - unhandled for now

                            goto case ChunkType.ModelPackagePC;

                        case ChunkType.ModelPackagePC:
                        case ChunkType.ModelPackagePS2:
                        case ChunkType.ModelPackageXbox:
                        case ChunkType.ModelPackageWii:
                            // load the package (materials only)
                            var package = SpoolableResourceFactory.Create<ModelPackage>(sender, true);

                            var textures = package.Textures;

                            // nab the textures
                            if (textures != null)
                            {
                                if (TextureUtils.ExportTextures(textures, outDir, (prefixes) ? $"{package.UID:X4}" : "", prefixes, true))
                                    count++;
                            }

                            total++;

                            // free package
                            package.FreeMaterials();
                            break;
                        }
                    };

                    var nFilesProcessed = 0;

                    //
                    // try loading each file as a model file
                    //
                    foreach (var file in Directory.EnumerateFiles(saveDlg.SelectedPath, "*.*"))
                    {
                        if (subfolders)
                            outDir = Path.Combine(path, Path.GetFileName(file));

                        if (chunker.Load(file))
                            nFilesProcessed++;
                    }

                    // restore loading level
                    ModelPackage.LoadLevel = level;

                    if (nFilesProcessed != 0)
                    {
                        // make sure to reset these
                        TextureUtils.BuggedAboutDuplicateFiles = false;
                        TextureUtils.OverwriteDuplicateFiles = false;

                        MessageBox.Show($"Successfully exported textures to '{outDir}':\r\n\r\n- Processed {count} / {total} packages in {nFilesProcessed} files!",
                            "Texture Extractor", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    // free the chunker
                    chunker.Dispose();
                }
            }
        }

        private bool ExportAllTextures(bool silent = false)
        {
            var modelFile = AT.CurrentState.ModelFile;

            if (modelFile != null)
            {
                var total = 0;
                var count = 0;

                var saveDlg = new FolderSelectDialog()
                {
                    InitialDirectory = Settings.TexturesDirectory,
                };

                if (saveDlg.ShowDialog())
                {
                    var prefixes = false;

                    if (MessageBox.Show("Do you want to prefix filenames with the package UID and texture index? This can make it easier to browse them in a file explorer.",
                        "Texture Extractor", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        prefixes = true;
                    }

                    var directory = saveDlg.SelectedPath;

                    // try to export as many textures as possible
                    foreach (var package in modelFile.Packages)
                    {
                        // do we have to load it first?
                        if (!package.HasMaterials)
                        {
                            var level = ModelPackage.LoadLevel;

                            // don't load the model data!
                            ModelPackage.LoadLevel = ModelPackageLoadLevel.Materials;

                            SpoolableResourceFactory.Load(package);

                            ModelPackage.LoadLevel = level;
                        }

                        var textures = package.Textures;

                        if (textures != null)
                        {
                            if (TextureUtils.ExportTextures(textures, directory, (prefixes) ? $"{package.UID:X4}" : "", prefixes, true))
                                count++;
                        }

                        total++;
                    }

                    if (count != 0)
                    {
                        // make sure to reset these
                        TextureUtils.BuggedAboutDuplicateFiles = false;
                        TextureUtils.OverwriteDuplicateFiles = false;
                    }

                    if (total != 0)
                    {
                        MessageBox.Show($"Successfully exported textures from {count} / {total} packages!");
                        return true;
                    }
                }
                else
                {
                    // fail quietly
                    return false;
                }
            }

            if (!silent)
                MessageBox.Show("Nothing to export!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);

            return false;
        }

        // requiring this is probably a design flaw...but it works :P
        volatile bool m_deferSelectionChange = false;

        private void OnModelPackageSelected(object sender, EventArgs e)
        {
            m_deferSelectionChange = true;
            OnPropertyChanged("ModelContainers");
            m_deferSelectionChange = false;

            if (CurrentModelPackage != null && CurrentModelPackage.HasModels)
            {
                Containers.SelectedIndex = 0;
            }
            else
            {
                Containers.SelectedIndex = -1;

                // make sure we completely reset...
                // the whole UI system is a joke!
                Viewer.RemoveActiveModel();
                ResetLODButtons();
            }
        }

        private void OnFileModified(object sender, EventArgs e)
        {
            // hooo boy
            IsFileDirty = true;
            CurrentModelPackage.NotifyChanges();
        }

        private void OnModelPackageItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (Viewer.SelectedModel != null)
                Viewer.SelectedModel = null;

            TextureCache.FlushIfNeeded();

            int index = Packages.SelectedIndex;

            AT.CurrentState.SelectedModelPackage = (index != -1) ? ModelPackages[index] : null;
        }

        private void OnModelContainerItemSelected(object sender, EventArgs e)
        {
            if (m_deferSelectionChange)
                return;

            if (Containers.SelectedIndex == -1)
            {
                Viewer.RemoveActiveModel();
                ResetLODButtons();

                // select the first group
                m_deferSelectionChange = true;
                Containers.SelectedIndex = 0;
            }

            if (SelectedModelContainer != null)
                Viewer.SetActiveModel(SelectedModelContainer.Models);

            UpdateRenderingOptions();

            if (m_deferSelectionChange)
                m_deferSelectionChange = false;
        }

        public RelayCommand TextureDifferCommand { get; set; }

        public void Initialize()
        {
            Settings.Verify();

            AT.CurrentState.PropertyChanged += (o, e) => {
                //Debug.WriteLine($">> State change: '{e.PropertyName}'");
                OnPropertyChanged(e.PropertyName);
            };

            AT.CurrentState.MainWindow = this;
            AT.CurrentState.ModelView = this; // TODO: unfuck this hacky shit

            AT.CurrentState.ModelPackageSelected += OnModelPackageSelected;
            AT.CurrentState.FileModified += OnFileModified;

            //--DSC.ProgressUpdated += (o, e) => {
            //--    var progress_str = e.Message;
            //--
            //--    if (e.Progress > -1)
            //--        progress_str += $" [{Math.Round(e.Progress):F1}%]";
            //--    
            //--    Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new ThreadStart(() => {
            //--        Debug.WriteLine($"[INFO] {progress_str}");
            //--    }));
            //--};

            KeyDown += (o,e) => {
                switch (CurrentTab)
                {
                case 0:
                    Viewer.OnKeyPressed(o, e);

                    switch (e.Key)
                    {
                    case Key.B:
                        var status = (FileChunker.HACK_BigEndian = !FileChunker.HACK_BigEndian) ? "enabled" : "disabled";

                        MessageBox.Show($"Big-endian mode {status}.", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case Key.X:
                        // try to convert DPL models only..
                        if (CurrentModelPackage.Version != 1)
                            break;

                        var modelPackage = ModelConverter.Convert(CurrentModelPackage, 6);

                        // assume it's a vehicle
                        modelPackage.UID = 0xFF;

                        var resource = modelPackage.GetInterface();

                        resource.Save();

                        var chunker = new FileChunker();

                        chunker.Children.Add(resource.Spooler);

                        var filename = Path.Combine(Settings.ExportDirectory,
                            $"{Path.GetFileNameWithoutExtension(CurrentModelFile.FileName)}_converted.vvv");

                        chunker.Save(filename);

                        MessageBox.Show($"Successfully converted to '{filename}'!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    }

                    break;
                default:
                    AT.CurrentState.HandleKeyDown(o, e);
                    break;
                }

                if (e.Key == Key.F5)
                {
                    var sb = new StringBuilder();
                    MemoryCache.Dump(sb, true);

                    MessageBox.Show(sb.ToString(), "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            
            Packages.SelectionChanged += OnModelPackageItemSelected;
            Containers.SelectionChanged += OnModelContainerItemSelected;
            
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

            ApplyTransform.Checked += (o, e) => Viewer.ToggleTransforms();
            ApplyTransform.Unchecked += (o, e) => Viewer.ToggleTransforms();
            
            fileOpen.Click += (o, e) => OnFileOpenClick();
            fileOpenDriv3r.Click += (o, e) => OnFileOpenGameClick(GameType.Driv3r);
            fileOpenDriverPL.Click += (o, e) => OnFileOpenGameClick(GameType.DriverPL);
            
            fileClose.Click += (o, e) => OnFileCloseClick();

            fileSave.Click += (o, e) => OnFileSaveClick(false);
            fileSaveAs.Click += (o, e) => OnFileSaveClick(true);

            fileExit.Click += (o, e) => OnFileExitClick();
            
            chunkViewer.Click += (o, e) => AT.CurrentState.CreateDialog<ChunkViewer>(false);
            modelTool.Click += (o, e) => AT.CurrentState.CreateDialog<Importer>(true);

            optionsDlg.Click += (o, e) => AT.CurrentState.CreateDialog<OptionsDialog>(true);

            impAntilliScene.Click += (o, e) => ImportAntilliScene();
            expAntilliScene.Click += (o, e) => ExportAntilliScene();

            impModelPackage.Click += (o, e) => ImportModelPackage();
            expModelPackage.Click += (o, e) => ExportModelPackage();

            expWavefrontOBJ.Click += (o, e) => ExportWavefrontOBJ();

            expAllTextures.Click += (o, e) => ExportAllTextures();

            extractTextures.Click += (o, e) => ExtractAllTexturesFromDirectory();

            /*
            blenderSync.Click += (o, e) => ConnectToBlender();
            blenderSendCmd.Click += (o, e) => SendCommandToBlender();
            */

            

            

            var d3Log = new Action<string>((s) => {
                Console.WriteLine(s);
            });
            
            Viewer.Loaded += (o, e) => {
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
            TextureDifferCommand = new RelayCommand(delegate (object o)
            {
                if (IsFileOpened)
                {
                    MessageBox.Show("Please close the current file before using this tool.", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    AT.CurrentState.CreateDialog<TextureDiffView>(true, true);
                }
            });

            InitializeComponent();
        }
    }
}
