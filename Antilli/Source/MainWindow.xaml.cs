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
        
        public ModelFile CurrentModelFile
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

        public ModelGroupListItem SelectedModelGroup
        {
            get { return Groups.SelectedItem as ModelGroupListItem; }
        }

        public List<ModelGroupListItem> ModelGroups
        {
            get
            {
                if (CurrentModelPackage == null || CurrentModelPackage.Models == null)
                    return null;

                List<ModelGroupListItem> items = new List<ModelGroupListItem>();

                Model curModel = null;

                foreach (var model in CurrentModelPackage.Models)
                {
                    if (curModel == null || curModel.UID != model.UID)
                    {
                        items.Add(new ModelGroupListItem(CurrentModelPackage, model));
                        curModel = model;
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
        
        public bool ShowWaitCursor
        {
            set { Mouse.OverrideCursor = (value) ? Cursors.Wait : null; }
        }

        public bool IsViewWidgetVisible
        {
            get { return CurrentViewWidget != null; }
        }

        public int MatTexRowSpan
        {
            get { return (AT.CurrentState.CanUseGlobals) ? 1 : 2; }
        }
        
        public bool IsFileOpened
        {
            get { return CurrentModelFile != null; }
        }

        private bool m_isFileDirty;

        public bool IsFileDirty
        {
            get { return m_isFileDirty; }
            set
            {
                if (m_isFileDirty != value)
                {
                    m_isFileDirty = value;

                    if (IsFileOpened)
                    {
                        var filename = CurrentModelFile.FileName;

                        SubTitle = (m_isFileDirty) ? $"{filename} *" : filename;
                    }

                    UpdateFileStatus();
                }
            }
        }

        public bool AreChangesPending
        {
            get { return IsFileOpened && (IsFileDirty || CurrentModelFile.AreChangesPending); }
        }

        public bool CanSaveFile
        {
            get { return IsFileOpened && AreChangesPending; }
        }
        
        private void UpdateFileStatus()
        {
            OnPropertyChanged("IsFileOpened");
            OnPropertyChanged("CanSaveFile");
        }

        private void ReleaseModels()
        {
            if (AT.CurrentState.ModelFile != null)
            {
                AT.CurrentState.SelectedModelPackage = null;
                AT.CurrentState.CanUseGlobals = false;
                AT.CurrentState.CanUseBlendWeights = false;

                AT.CurrentState.ModelFile.Dispose();
                AT.CurrentState.ModelFile = null;

                Viewer.ClearModels();
                ResetViewWidgets();
            }
        }

        private void CloseFile(bool exiting)
        {
            ReleaseModels();

            IsFileDirty = false;
            SetCurrentFile(null);
        }

        private void SetCurrentFile(string filename)
        {
            SubTitle = filename;
            UpdateFileStatus();
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
                vgtFile = Driv3r.GetVehicleGlobals(city);

            if (File.Exists(vgtFile))
                vehicleFile.VehicleGlobals = new StandaloneTextureFile(vgtFile);

            AT.CurrentState.ModelFile = vehicleFile;
            AT.CurrentState.CanUseGlobals = vehicleFile.HasVehicleGlobals;
        }

        private void OnFileOpened(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();
            var filter = FileManager.FindFilter(extension, GameType.Driv3r, (GameFileFlags.Models | GameFileFlags.Textures));

            AT.CurrentState.UseDPLHacks = false;

            if (filter.Flags == GameFileFlags.None)
            {
                filter = FileManager.FindFilter(extension, GameType.DriverPL, (GameFileFlags.Models | GameFileFlags.Textures | GameFileFlags.Resource));

                if (filter.Flags != GameFileFlags.None)
                {
                    AT.CurrentState.UseDPLHacks = true;
                }
                else
                {
                    MessageBox.Show("Unsupported file type selected, please try another file.", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var timer = new Stopwatch();
            timer.Start();

            var modelFile = new ModelFile();
            var setupModels = true;
            
            switch (extension)
            {
            case ".dam":
                modelFile.SpoolerLoaded += (s, e) => {
                    if (s.Context == (int)ChunkType.CharacterSkeletons)
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
            case ".vvs":
            case ".vvv":
                LoadDriv3rVehicles(filename);
                setupModels = false;
                break;
            }

            if (setupModels)
            {
                modelFile.Load(filename);

                AT.CurrentState.ModelFile = modelFile;
                AT.CurrentState.CanUseGlobals = false;
            }

            SetCurrentFile(filename);

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
            var dialog = FileManager.OpenDialog;
            
            if (dialog.ShowDialog() ?? false)
            {
                dialog.InitialDirectory = Path.GetDirectoryName(dialog.FileName);
                OnFileOpened(dialog.FileName);

            }
        }

        private bool AskUserPrompt(string message)
        {
            return MessageBox.Show(message, "Antilli", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
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
                }
            }
            else
            {
                if (AskUserPrompt($"All pending changes will be saved to '{CurrentModelFile.FileName}'. Do you wish to OVERWRITE the original file? (NO BACKUPS WILL BE CREATED)"))
                {
                    if (CurrentModelFile.Save())
                    {
                        IsFileDirty = false;
                        MessageBox.Show($"Successfully saved changes to '{filename}'!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to save '{filename}'! No changes were made.", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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
                Environment.Exit(0);
            }
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

            if (SelectedModelGroup != null)
            {
                foreach (var model in SelectedModelGroup.Models)
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
        
        private void ExportModelFile()
        {
            if (CurrentModelPackage == null)
                MessageBox.Show("Nothing to export!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
            else
            {
                var prompt = CreateDialog<ExportModelDialog>(true, false);

                if (!SelectedModelGroup.IsNull)
                    prompt.FolderName = SelectedModelGroup.Text.Replace(':', '-');

                if (prompt.ShowDialog() ?? false)
                {
                    var flags = prompt.Flags;
                    var name = prompt.FolderName;

                    var path = Path.Combine(Settings.ModelsDirectory, name);

                    switch (prompt.Format)
                    {
                    case ExportModelFormat.WavefrontObj:
                        {
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
                                var part = SelectedModelGroup;
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
                        } break;
                    default:
                        MessageBox.Show("Unsupported format!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
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

                IsFileDirty = false;
                MessageBox.Show($"Successfully exported to '{path}'!", "ModelPackage Exporter", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void ExportAntilliModelFile()
        {
            if (CurrentModelPackage == null || CurrentModelFile == null)
            {
                MessageBox.Show("Nothing to export!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var modelFile = CurrentModelFile as Driv3rVehiclesFile;

            var idx = (!modelFile.IsMissionVehicleFile) ? modelFile.Models.IndexOf(CurrentModelPackage) : Groups.SelectedIndex;

            var hierarchy = modelFile.Hierarchies[idx];

            var dir = Settings.ExportDirectory;
            var path = String.Format("{0}\\{1}_{2}.mod.xml", dir, Path.GetFileName(CurrentModelFile.FileName).Replace('.', '_'), hierarchy.UID);

            var m = AntilliModel.Create(CurrentModelPackage);
            m.SaveXML(path);

#if false
            var header = Encoding.UTF8.GetBytes("ANTILLI!");

            var version = 1;
            var flags = 0; // reserved for future use

            using (var ms = new MemoryStream())
            {
                ms.Write(header);
                ms.Write((short)((flags << 8) | version));
                ms.Write((short)MagicNumber.FB); // ;)

                // size of model data
                ms.Position += 4;

                var parts = CurrentModelPackage.Parts;
                var vBuffers = CurrentModelPackage.VertexBuffers;
                var materials = CurrentModelPackage.Materials;
                var textures = CurrentModelPackage.Textures;

                // model package stuff
                ms.Write(CurrentModelPackage.UID);

                ms.Write((short)parts.Count);
                ms.Write((short)vBuffers.Count);
                ms.Write((short)materials.Count);
                ms.Write((short)textures.Count);

                // reserved -- must be -1 to skip
                ms.Write(-1);

                // vertex buffers
                foreach (var vBuf in vBuffers)
                    vBuf.WriteTo(ms, true);

                var texLookup = new Dictionary<int, int>();
                var texLookupCount = 0;

                var texNames = new List<String>();

                // individual texture names
                foreach (var tex in textures)
                {
                    if (texLookup.ContainsKey(tex.CRC32))
                        continue;

                    var texName = $"{tex.CRC32}.dds";
                    texLookup.Add(tex.CRC32, texLookupCount++);

                    texNames.Add(texName);
                }

                ms.Write(texLookupCount);

                foreach (var texName in texNames)
                    ms.Write(texName + '\0');

                // materials
                foreach (var mat in materials)
                {
                    ms.Write((short)mat.Substances.Count);
                    ms.Write((short)mat.Type);

                    ms.Write(mat.AnimationSpeed);

                    foreach (var substance in mat.Substances)
                    {
                        ms.Write(substance.Flags);
                        ms.Write((short)substance.Mode);
                        ms.Write((short)substance.Type);

                        ms.Write(substance.Textures.Count);

                        foreach (var texture in substance.Textures)
                        {
                            ms.Write(texture.Reserved);
                            ms.Write((short)texture.Type);
                            ms.Write((short)texLookup[texture.CRC32]);
                            ms.Write(texture.Unknown);
                        }
                    }
                }

                // TODO: Write model data

                var hierPtr = ms.Position;
                var modelSize = (int)(hierPtr - 16);

                ms.Position = 0xC;
                ms.Write(modelSize);

                ms.Position = hierPtr;
                // TODO: Write hierarchy data

                // commit changes
                ms.SetLength(ms.Position);

                File.WriteAllBytes(path, ms.ToArray());
            }
#endif
            MessageBox.Show($"Successfully exported AIModel file to '{path}'!", "Antilli Model Exporter", MessageBoxButton.OK, MessageBoxImage.Information);
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

                // fill in a null texture's UID
                // otherwise, leave it alone (bad idea?)
                if (texRef.Data.UID == 0)
                    texRef.Data.UID = (int)Memory.GetCRC32(ddsBuffer);
                
                Viewer.UpdateActiveModel();

                // reload the active texture if necessary
                if (IsViewWidgetVisible)
                    CurrentViewWidget.SetTexture(texture);

                IsFileDirty = true;
                CurrentModelPackage.NotifyChanges();
            }
        }

        private void ReplaceTexture(object sender, RoutedEventArgs e)
        {
            var item = ((sender as FrameworkElement).DataContext) as TextureTreeItem;

            if (item != null)
            {
                var tex = item.Texture;
                ReplaceTexture(tex);

                item.UpdateName();
                
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
            OnPropertyChanged("ModelGroups");
            m_deferSelectionChange = false;

            if (CurrentModelPackage != null && CurrentModelPackage.HasModels)
            {
                Groups.SelectedIndex = 0;
            }
            else
            {
                Groups.SelectedIndex = -1;

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
            ResetViewWidgets();
        }

        private void OnModelGroupItemSelected(object sender, EventArgs e)
        {
            if (m_deferSelectionChange)
                return;

            if (Groups.SelectedIndex == -1)
            {
                Viewer.RemoveActiveModel();
                ResetLODButtons();

                // select the first group
                m_deferSelectionChange = true;
                Groups.SelectedIndex = 0;
            }

            if (SelectedModelGroup != null)
                Viewer.SetActiveModel(SelectedModelGroup.Models);

            UpdateRenderingOptions();

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

            // unhandled type or null view widget?!
            if (objId == -1 || (CurrentViewWidget == null))
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

        private Dictionary<Type, Window> m_dialogs = new Dictionary<Type, Window>();
        
        private T CreateDialog<T>(bool modal, bool show = true)
            where T : Window, new()
        {
            var wndType = typeof(T);

            T dialog = null;

            if (modal && m_dialogs.ContainsKey(wndType))
            {
                dialog = (T)m_dialogs[wndType];

                if (show)
                    dialog.Activate();
            }
            else
            {
                dialog = new T() {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                if (modal)
                {
                    dialog.Closed += (o, e) => {
                        m_dialogs.Remove(wndType);
                    };

                    m_dialogs.Add(wndType, dialog);
                }

                if (show)
                    dialog.Show();
            }

            dialog.Closed += (o, e) => {
                // focus main window so everything doesn't disappear
                Focus();
            };
            
            return dialog;
        }
        
        private void Initialize()
        {
            Settings.Verify();

            if (!Debugger.IsAttached)
            {
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
            }

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
                    case Key.X:
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
                case 1:
                    MaterialViewWidget.OnKeyPressed(o, e);

                    switch (e.Key)
                    {
                    case Key.X:
                        var xmlDoc = new XmlDocument();

                        var matPkg = xmlDoc.CreateElement("MaterialPackage");
                        var outPath = Path.Combine(Settings.ExportDirectory, "MaterialPackage.xml");
                        matPkg.SetAttribute("Version", "6");

                        CurrentModelPackage.SaveMaterials(matPkg);

                        xmlDoc.AppendChild(matPkg);
                        xmlDoc.Save(outPath);

                        Debug.WriteLine($"Saved material package to '{outPath}'.");
                        break;
                    }
                    break;
                case 2:
                    TextureViewWidget.OnKeyPressed(o, e);
                    break;
                }
            };
            
            Packages.SelectionChanged += OnModelPackageItemSelected;
            Groups.SelectionChanged += OnModelGroupItemSelected;
            
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
            fileClose.Click += (o, e) => OnFileCloseClick();

            fileSave.Click += (o, e) => OnFileSaveClick(false);
            fileSaveAs.Click += (o, e) => OnFileSaveClick(true);

            fileExit.Click += (o, e) => OnFileExitClick();

            exportSel.Click += (o, e) => ExportModelFile();
            exportMDPC.Click += (o, e) => ExportModelPackage();
            exportVPK.Click += (o, e) => ExportVehicleHierarchyVPK();
            exportAIModel.Click += (o, e) => ExportAntilliModelFile();

            chunkViewer.Click += (o, e) => CreateDialog<ChunkViewer>(false);
            modelTool.Click += (o, e) => CreateDialog<Importer>(true);

            optionsDlg.Click += (o, e) => CreateDialog<OptionsDialog>(true);

            mtlListExpandAll.Click += (o, e) => MaterialsList.ExpandAll(true);
            mtlListCollapseAll.Click += (o, e) => MaterialsList.ExpandAll(false);
        
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
            Thread.CurrentThread.CurrentCulture = AT.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = AT.CurrentCulture;

            Initialize();
        }
    }
}
