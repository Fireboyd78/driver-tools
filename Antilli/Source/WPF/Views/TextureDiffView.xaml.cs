using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;
using System.Xml.Linq;

using DSCript;
using DSCript.Menus; // TEMPORARY HACKS
using DSCript.Models;
using DSCript.Spooling;

namespace Antilli
{
    using Selection = TextureViewWidget.Selection;

    /// <summary>
    /// Interaction logic for TextureDiffView.xaml
    /// </summary>
    public partial class TextureDiffView : ObservableWindow
    {
        public struct DiffState : IDisposable
        {
            TextureViewWidget m_View;
            TextureReference m_Owner;

            public ITextureData Input;
            public ITextureData Output;

            public ITextureData Backup;

            public bool RevertOnlyHack;

            private void NotifyChanges(bool dirty)
            {
                if (m_Owner != null)
                    m_Owner.NotifyChanges(dirty);
                if (m_View != null)
                    m_View.UpdateView();
            }

            public bool Apply()
            {
                if (CopyCatFactory.CopyToB(Input, Output, CopyClassType.DeepCopy))
                {
                    NotifyChanges(true);
                    return true;
                }

                return false;
            }

            public bool Undo()
            {
                if (CopyCatFactory.CopyToB(Backup, Output, CopyClassType.DeepCopy))
                {
                    NotifyChanges(false);
                    return true;
                }

                return false;
            }

            public void Dispose()
            {
                // remove our references
                m_View = null;
                m_Owner = null;
                Input = null;
                Output = null;
                Backup = null;
            }

            public DiffState(TextureViewWidget view, TextureReference input, TextureReference output, bool revertOnlyHack = false)
            {
                m_View = view;

                m_Owner = output;

                Input = input.Texture;
                Output = output.Texture;

                Backup = CopyCatFactory.GetCopy(Output, CopyClassType.DeepCopy);

                RevertOnlyHack = revertOnlyHack;
            }
        }

        public RelayCommand<string> DiffCommand { get; set; }
        public RelayCommand<string> CloneCommand { get; set; }

        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }

        public KeyGesture UndoGesture { get; }
        public KeyGesture RedoGesture { get; }

        public KeyBinding UndoBinding { get; }
        public KeyBinding RedoBinding { get; }

        public RelayCommand LoadDiffCommand { get; }
        public RelayCommand SaveDiffCommand { get; }

        public KeyGesture LoadDiffGesture { get; }
        public KeyGesture SaveDiffGesture { get; }

        public KeyBinding LoadDiffBinding { get; }
        public KeyBinding SaveDiffBinding { get; }

        public RelayCommand ApplyAllCommand { get; }
        public RelayCommand RevertAllCommand { get; }

        public KeyGesture ApplyAllGesture { get; }
        public KeyGesture RevertAllGesture { get; }

        public KeyBinding ApplyAllBinding { get; }
        public KeyBinding RevertAllBinding { get; }

        protected Stack<DiffState> DiffStack { get; }
        protected Stack<DiffState> SaveStack { get; }

        public bool HasChangesPending => DiffStack.Count != 0;
        public bool HasRevertedChanges => SaveStack.Count != 0;

        public bool AreFilesLoaded => ViewLeft.IsFileOpened && ViewRight.IsFileOpened;

        public bool CanApplyChanges => HasRevertedChanges;
        public bool CanRevertChanges => HasChangesPending /*|| (ViewLeft.IsFileDirty || ViewRight.IsFileDirty)*/;

        public bool CanLoadDiff => ViewLeft.IsFileOpened;
        public bool CanSaveDiff => AreFilesLoaded && HasChangesPending;

        private string[] sillyMessages = {
            "irrevocably, I'm afraid",
            "hope you won't miss them..",
            "hasta la vista, baby!",
            "wait, did I actually?",
            "bye-bye!",
            "thanks for nothing!",
            "now get back to work!",
            "brought to you by HACKS INC.",
            "be good, be bad, be...",
        };

        private string m_StatusText;

        public string StatusText
        {
            get { return m_StatusText; }
            set { SetValue(ref m_StatusText, value, "StatusText"); }
        }

        private void SetStatusColorBar(Color color)
        {
            var animation = new ColorAnimation(Colors.WhiteSmoke, color, TimeSpan.FromSeconds(0.785), FillBehavior.HoldEnd);

            animation.AutoReverse = true;
            animation.AccelerationRatio = 0.125;

            var oldBrush = StatusTextBox.Background;
            var newBrush = new SolidColorBrush(Colors.Transparent);

            StatusTextBox.Background = newBrush;

            animation.Completed += (o, e) =>
            {
                newBrush.ApplyAnimationClock(SolidColorBrush.ColorProperty, null);
                StatusTextBox.Background = oldBrush;
            };

            newBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation, HandoffBehavior.Compose);
        }

        private void UpdateStatus(string status = "")
        {
            OnPropertyChanged(nameof(HasChangesPending));
            OnPropertyChanged(nameof(HasRevertedChanges));
            OnPropertyChanged(nameof(AreFilesLoaded));
            OnPropertyChanged(nameof(CanApplyChanges));
            OnPropertyChanged(nameof(CanRevertChanges));
            OnPropertyChanged(nameof(CanLoadDiff));
            OnPropertyChanged(nameof(CanSaveDiff));

            if (String.IsNullOrEmpty(status))
                status = "Ready";

            StatusText = status;
        }

        private void OnStatusUpdated(object o)
        {
            UpdateStatus();
        }

        private bool NoRevertHacksNeeded
        {
            get
            {
                if (HasChangesPending)
                {
                    foreach (var diff in DiffStack)
                    {
                        if (diff.RevertOnlyHack)
                            return false;
                    }
                }

                return true;
            }
        }

        private bool CanUndoCommand(object o) => NoRevertHacksNeeded;
        private bool CanRedoCommand(object o) => HasRevertedChanges;

        private bool CanLoadDiffCommand(object o) => CanLoadDiff;
        private bool CanSaveDiffCommand(object o) => CanSaveDiff;

        private bool CanApplyAllCommand(object o) => CanApplyChanges;
        private bool CanRevertAllCommand(object o) => CanRevertChanges;

        private void OnUndoCommand(object o)
        {
            var undo = DiffStack.Pop();

            undo.Undo();
            SaveStack.Push(undo);

            UpdateStatus($"Reverted changes to texture {undo.Backup}");
            SetStatusColorBar(Colors.MediumSeaGreen);
        }

        private void OnRedoCommand(object o)
        {
            var redo = SaveStack.Pop();

            redo.Apply();
            DiffStack.Push(redo);

            UpdateStatus($"Restored changes to texture {redo.Backup}");
            SetStatusColorBar(Colors.MediumSeaGreen);
        }

        private void OnApplyAllCommand(object o)
        {
            if (AskUserPrompt("All pending changes will be applied - continue?\n\nPlease note, you will still need to save the files you wish to keep any changes of."))
            {
                var count = 0;

                while (SaveStack.Count != 0)
                {
                    var redo = SaveStack.Pop();

                    redo.Apply();
                    DiffStack.Push(redo);

                    count++;
                }

                UpdateStatus($"Applied {count} changes - these can be undone one at a time");
                SetStatusColorBar(Colors.MediumSeaGreen);
            }
        }

        private bool AskUserOKToRevertAll()
        {
            if (!NoRevertHacksNeeded)
                return AskUserPrompt("All current changes will be undone - this CANNOT be reverted. Continue?");

            return AskUserPrompt("All current changes will be undone - continue?\n\nPlease note, you will only be able to redo up until the point at which all changes were reverted.");
        }

        private void OnRevertAllCommand(object o)
        {
            if (AskUserOKToRevertAll())
            {
                // clear all changes up to this point
                SaveStack.Clear();

                var stupidHacks = !NoRevertHacksNeeded;
                var count = 0;

                while (DiffStack.Count != 0)
                {
                    var undo = DiffStack.Pop();

                    undo.Undo();

                    if (!stupidHacks)
                        SaveStack.Push(undo);

                    count++;
                }

                if (stupidHacks)
                {
                    var sillyRandom = new Random(12345);

                    ViewLeft.TextureList.Items.Refresh();
                    UpdateStatus($"Reverted {count} change(s) - {sillyMessages[sillyRandom.Next(0, sillyMessages.Length - 1)]}");
                }
                else
                {
                    UpdateStatus($"Reverted {count} changes(s) - these can be redone one at a time");
                }

                SetStatusColorBar(Colors.MediumSeaGreen);
            }
        }

        private int ParseInt(string input)
        {
            if (input.StartsWith("0x"))
                return int.Parse(input.Substring(2), NumberStyles.HexNumber);

            return int.Parse(input);
        }

        private void OnLoadDiffCommand(object o)
        {
            var dialog = FileManager.GetOpenDialog("Please select a diff file:", "Diff file|*.diff.xml");

            if (dialog.ShowDialog(this) ?? false)
            {
                var xml = XDocument.Load(dialog.FileName);

                var root = xml.Root;

                if (root.Name == "TextureDiffs")
                {
                    var version = MenuData.GetAttribute(root, "Version", float.Parse);

                    if (version >= 1.00f)
                    {
                        var diffPath = Path.GetDirectoryName(dialog.FileName);
                        var sourceDir = root.Attribute("SourceDirectory").Value;
                        var texDir = Path.GetFullPath(Path.Combine(diffPath, sourceDir));

                        var errors = new List<String>();
                        var count = 0;

                        foreach (var node in root.Elements().OfType<XElement>())
                        {
                            switch (node.Name.LocalName)
                            {
                            case "Texture":
                                var texUID = MenuData.GetAttribute(node, "UID", ParseInt, 0x01010101);
                                var texHandle = MenuData.GetAttribute(node, "Handle", ParseInt);

                                TextureReference texRef = null;
                                TextureReference newRef = null;

                                foreach (var t in ViewLeft.TextureList.Items.OfType<TextureReference>())
                                {
                                    var tex = t.Texture;

                                    if (tex.UID == texUID && texHandle == tex.Handle)
                                    {
                                        texRef = t;
                                        newRef = new TextureReference(texRef);
                                        break;
                                    }
                                }

                                if (texRef == null)
                                {
                                    if (texUID != 0x01010101)
                                    {
                                        errors.Add(new UID(texUID, texHandle).ToString(":"));
                                    }
                                    else
                                    {
                                        errors.Add(texHandle.ToString("X8"));
                                    }

                                    // texture not found, continue...
                                    continue;
                                }

                                foreach (var opNode in node.Elements().OfType<XElement>())
                                {
                                    switch (opNode.Name.LocalName)
                                    {
                                    case "ReplaceWith":
                                        var uid = opNode.Attribute("UID");

                                        if (uid != null)
                                        {
                                            var newUID = ParseInt(uid.Value);

                                            if (newUID != texUID)
                                                newRef.Texture.UID = newUID;
                                        }

                                        var handle = opNode.Attribute("Handle");

                                        if (handle != null)
                                        {
                                            var newHandle = ParseInt(handle.Value);

                                            if (newHandle != texHandle)
                                                newRef.Texture.Handle = newHandle;
                                        }

                                        var texFile = opNode.Attribute("File").Value;
                                        var texPath = Path.GetFullPath(Path.Combine(texDir, texFile));

                                        if (File.Exists(texPath))
                                        {
                                            var texture = TextureCache.GetTexture(newRef.Texture);
                                            var buffer = File.ReadAllBytes(texPath);

                                            texture.SetBuffer(buffer);

                                            var diff = new DiffState(ViewLeft, newRef, texRef, revertOnlyHack: true);

                                            SaveStack.Clear();
                                            DiffStack.Push(diff);

                                            diff.Apply();
                                            count++;

                                            TextureCache.Release(texture);
                                        }
                                        else
                                        {
                                            errors.Add(texPath);
                                        }
                                        
                                        break;
                                    }
                                }
                                break;
                            }
                        }

                        ViewLeft.TextureList.Items.Refresh();

                        if (errors.Count > 0)
                        {
                            MessageBox.Show("The following textures could not be resolved:\n\n - " + string.Join("\n - ", errors),
                                "Texture Differ", MessageBoxButton.OK, MessageBoxImage.Information);

                            UpdateStatus($"Replaced {count} texture slots - {errors.Count} warnings(s)");
                            SetStatusColorBar(Colors.BlanchedAlmond);
                        }
                        else
                        {
                            UpdateStatus($"Replaced {count} texture slots successfully");
                            SetStatusColorBar(Colors.MediumSeaGreen);
                        }
                    }
                    else
                    {
                        UpdateStatus($"Unsupported diff file version - {version:F2}");
                        SetStatusColorBar(Colors.LightGoldenrodYellow);
                    }
                }
                else
                {
                    UpdateStatus("Invalid diff file!");
                }
            }
        }

        private void OnSaveDiffCommand(object o)
        {
            var dialog = FileManager.GetSaveDialog("Please enter a filename", ".diff.xml");

            dialog.InitialDirectory = Settings.TexturesDirectory;
            dialog.Filter = "Diff file|*.diff.xml";
            dialog.FileName = "CustomTextures";

            if (dialog.ShowDialog(this) ?? false)
            {
                var xml = new XDocument();

                var root = new XElement("TextureDiffs");
                xml.Add(root);

                root.SetAttributeValue("Version", "1.00");
                root.SetAttributeValue("DateCreated", DateTime.Now);

                var diffName = FileManager.StripExtension(dialog.FileName);
                var diffPath = Path.GetDirectoryName(dialog.FileName);

                var texDir = $"{diffName}_files";
                var texPath = Path.Combine(diffPath, texDir);

                if (!Directory.Exists(texPath))
                    Directory.CreateDirectory(texPath);

                root.SetAttributeValue("SourceDirectory", texDir);
                
                foreach (var diff in DiffStack)
                {
                    var original = diff.Backup;
                    var modified = diff.Input;

                    var texture = new XElement("Texture");
                    root.Add(texture);

                    if (original.UID != 0x01010101)
                        texture.SetAttributeValue("UID", $"0x{original.UID:X}");

                    texture.SetAttributeValue("Handle", $"0x{original.Handle:X}");

                    var replace = new XElement("ReplaceWith");
                    texture.Add(replace);

                    var texName = "texture";
                    var texExt = TextureUtils.GetFileExtension(modified);
                    
                    if (modified.UID != 0x01010101)
                    {
                        var texUID = new UID(modified.UID, modified.Handle);
                        texName = texUID.ToString("_");
                    }
                    else
                    {
                        texName = $"{modified.Handle:X8}";
                    }

                    var texFile = $"{texName}.{texExt}";

                    if (modified.UID != original.UID)
                        replace.SetAttributeValue("UID", $"0x{modified.UID:X}");
                    if (modified.Handle != original.Handle)
                        replace.SetAttributeValue("Handle", $"0x{modified.UID:X}");

                    replace.SetAttributeValue("File", texFile);

                    FileManager.WriteFile(Path.Combine(texPath, texFile), modified.Buffer);
                }

                xml.Save(dialog.FileName);

                UpdateStatus($"Created diff file - {dialog.FileName}");
                SetStatusColorBar(Colors.MediumSeaGreen);
            }
        }

        private bool CanDiffCommand(string which)
        {
            switch (which)
            {
            case "l2r": return ViewLeft.CurrentSelection.Item != null && ViewRight.IsFileOpened;
            case "r2l": return ViewRight.CurrentSelection.Item != null && ViewLeft.IsFileOpened;
            }

            return false;
        }

        private void OnDiffCommand(string which)
        {
            // expected to have exactly 2 elements
            Selection[] selections = null;
            TextureViewWidget owner = null;

            switch (which)
            {
            case "l2r":
                // diff left onto right
                selections = new[] { ViewLeft.CurrentSelection, ViewRight.CurrentSelection };
                owner = ViewRight;
                break;
            case "r2l":
                // diff right onto left
                selections = new[] { ViewRight.CurrentSelection, ViewLeft.CurrentSelection };
                owner = ViewLeft;
                break;
            }

            if (selections != null && owner != null)
                DiffSelections(owner, ref selections[0], ref selections[1]);
        }

        private bool DiffSelections(TextureViewWidget owner, ref Selection input, ref Selection output)
        {
            var item1 = input.Item as TextureReference;
            var item2 = output.Item as TextureReference;

            var diff = new DiffState(owner, item1, item2);

            SaveStack.Clear();
            DiffStack.Push(diff);

            if (diff.Apply())
            {
                UpdateStatus($"Texture {diff.Backup} diff'd against {diff.Input}");
                SetStatusColorBar(Colors.MediumSeaGreen);
                return true;
            }

            return false;
        }

        private bool CanCloneCommand(string which)
        {
            switch (which)
            {
            case "l2r": return ViewLeft.IsFileOpened && ViewRight.IsFileOpened;
            }

            return false;
        }

        private void OnCloneCommand(string which)
        {
            ModelFile[] files = null;
            TextureViewWidget owner = null;

            switch (which)
            {
            case "l2r":
                files = new[] { ViewLeft.ModelsFile, ViewRight.ModelsFile };
                owner = ViewRight;
                break;
            }

            if (files != null)
            {
                if (CloneTextures(files[0], files[1]))
                    owner.ResetView();
            }
        }

        private bool AskUserPrompt(string message)
        {
            return MessageBox.Show(message, "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        private bool CloneTextures(ModelFile input, ModelFile output)
        {
            var packs1 = input.Packages;
            var packs2 = output.Packages;

            var completed = new HashSet<ModelPackage>();
            var packIndex = 0;

            var numTotal = 0;
            var numCollisions = 0;

            for (int i = packIndex; i < packs1.Count; i++)
            {
                var p1 = packs1[i];
                var done = false;

                if (!p1.HasMaterials)
                    continue;

                if (p1.UID == 0xFF)
                {
                    // advanced diffing not ready yet; this isn't possible
                    packIndex++;
                    continue;
                }

                for (int k = 0; k < packs2.Count; k++)
                {
                    var p2 = packs2[k];

                    if (!p2.HasMaterials || completed.Contains(p2) || (p2.UID != p1.UID))
                        continue;

                    if (p2.UID == 0xFF)
                    {
                        // advanced diffing not ready yet; this isn't possible
                        completed.Add(p2);
                        continue;
                    }

                    // advanced diffing not ready yet :P
#if DIFF_ADVANCED
                    var luTextures = new Dictionary<UID, TextureDataPC>();
                    var luCollisions = new HashSet<UID>();

                    foreach (var texture in p1.Textures)
                    {
                        var uid = new UID(texture.UID, texture.Handle);

                        if (luCollisions.Contains(uid))
                            continue;

                        if (luTextures.ContainsKey(uid))
                        {
                            luCollisions.Add(uid);
                            luTextures.Remove(uid);
                        }

                        luTextures.Add(uid, texture);
                    }
#else
                    // RISKY BUSINESS: assume these are exactly the same...
                    if (p1.Textures.Count == p2.Textures.Count)
                    {
                        for (int n = 0; n < p1.Textures.Count; n++)
                        {
                            var t1 = p1.Textures[n];
                            var t2 = p2.Textures[n];

                            // copy input texture to ouput texture
                            if (CopyCatFactory.CopyToB(t1, t2, CopyClassType.DeepCopy))
                                numTotal++;
                        }

                        p2.NotifyChanges(true);
                    }
#endif
                    completed.Add(p2);
                    done = true;
                }

                if (done)
                    packIndex++;
            }

            if (numTotal > 0)
            {
                MessageBox.Show($"Successfully cloned {numTotal} textures in {packIndex} package(s) to the right side!",
                    "Texture Differ", MessageBoxButton.OK, MessageBoxImage.Information);

                UpdateStatus($"Cloned {numTotal} textures in {packIndex} package(s)");
                return true;
            }
            else
            {
                MessageBox.Show($"Could not find any suitable candidates for cloning from the left to right.",
                    "Texture Differ", MessageBoxButton.OK, MessageBoxImage.Error);

                UpdateStatus("Clone operation failed!");
                return false;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (AreFilesLoaded && (HasChangesPending || HasRevertedChanges))
            {
                if (!AskUserPrompt("All unsaved changes will be lost. Are you sure?"))
                    e.Cancel = true;
            }

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewLeft.ModelsFile = null;
            ViewRight.ModelsFile = null;

            base.OnClosed(e);
        }

        public TextureDiffView()
        {
            DiffCommand = new RelayCommand<string>(OnDiffCommand, CanDiffCommand);
            CloneCommand = new RelayCommand<string>(OnCloneCommand, CanCloneCommand);

            UndoCommand = new RelayCommand(OnUndoCommand, CanUndoCommand);
            RedoCommand = new RelayCommand(OnRedoCommand, CanRedoCommand);

            UndoGesture = new KeyGesture(Key.Z, ModifierKeys.Control, "Ctrl-Z");
            RedoGesture = new KeyGesture(Key.Y, ModifierKeys.Control, "Ctrl-Y");

            UndoBinding = new KeyBinding(UndoCommand, UndoGesture);
            RedoBinding = new KeyBinding(RedoCommand, RedoGesture);

            InputBindings.Add(UndoBinding);
            InputBindings.Add(RedoBinding);

            LoadDiffCommand = new RelayCommand(OnLoadDiffCommand, CanLoadDiffCommand);
            SaveDiffCommand = new RelayCommand(OnSaveDiffCommand, CanSaveDiffCommand);

            LoadDiffGesture = new KeyGesture(Key.O, ModifierKeys.Control, "Ctrl-O");
            SaveDiffGesture = new KeyGesture(Key.S, ModifierKeys.Control, "Ctrl-S");

            LoadDiffBinding = new KeyBinding(LoadDiffCommand, LoadDiffGesture);
            SaveDiffBinding = new KeyBinding(SaveDiffCommand, SaveDiffGesture);

            InputBindings.Add(LoadDiffBinding);
            InputBindings.Add(SaveDiffBinding);

            ApplyAllCommand = new RelayCommand(OnApplyAllCommand, CanApplyAllCommand);
            RevertAllCommand = new RelayCommand(OnRevertAllCommand, CanRevertAllCommand);

            ApplyAllGesture = new KeyGesture(Key.D, ModifierKeys.Control, "Ctrl-D");
            RevertAllGesture = new KeyGesture(Key.R, ModifierKeys.Control, "Ctrl-R");

            ApplyAllBinding = new KeyBinding(ApplyAllCommand, ApplyAllGesture);
            RevertAllBinding = new KeyBinding(RevertAllCommand, RevertAllGesture);

            InputBindings.Add(ApplyAllBinding);
            InputBindings.Add(RevertAllBinding);

            DiffStack = new Stack<DiffState>();
            SaveStack = new Stack<DiffState>();

            InitializeComponent();

            StatusText = "Open a file to get started";
            SetStatusColorBar(Colors.LightSteelBlue);

            ViewLeft.OpenModelsFileCommand.ExecuteDelegate += OnStatusUpdated;
            ViewLeft.SaveModelsFileCommand.ExecuteDelegate += OnStatusUpdated;
            ViewLeft.CloseModelsFileCommand.ExecuteDelegate += OnStatusUpdated;

            ViewRight.OpenModelsFileCommand.ExecuteDelegate += OnStatusUpdated;
            ViewRight.SaveModelsFileCommand.ExecuteDelegate += OnStatusUpdated;
            ViewRight.CloseModelsFileCommand.ExecuteDelegate += OnStatusUpdated;
        }
    }
}
