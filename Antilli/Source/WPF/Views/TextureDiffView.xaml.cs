using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

using DSCript;
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
            TextureTreeItem m_Owner;

            public ITextureData Input;
            public ITextureData Output;

            public ITextureData Backup;

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

            public DiffState(TextureViewWidget view, TextureTreeItem input, TextureTreeItem output)
            {
                m_View = view;

                m_Owner = output;

                Input = input.Texture;
                Output = output.Texture;

                Backup = CopyCatFactory.GetCopy(Output, CopyClassType.DeepCopy);
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

        protected Stack<DiffState> DiffStack { get; }
        protected Stack<DiffState> SaveStack { get; }

        public bool HasChangesPending => DiffStack.Count != 0;
        public bool HasRevertedChanges => SaveStack.Count != 0;

        public bool CanRevertChanges => HasChangesPending || (ViewLeft.IsFileDirty || ViewRight.IsFileDirty);
        public bool AreFilesLoaded => ViewLeft.IsFileOpened && ViewRight.IsFileOpened;

        public bool CanSaveDiff => AreFilesLoaded && HasChangesPending;

        private string m_StatusText;

        public string StatusText
        {
            get { return m_StatusText; }
            set { SetValue(ref m_StatusText, value, "StatusText"); }
        }

        private void UpdateStatus(string status = "")
        {
            OnPropertyChanged("HasChangesPending");
            OnPropertyChanged("HasRevertedChanges");
            OnPropertyChanged("AreFilesLoaded");
            OnPropertyChanged("CanRevertChanges");
            OnPropertyChanged("CanSaveDiff");

            if (String.IsNullOrEmpty(status))
                status = "Ready";

            StatusText = status;
        }

        private void OnStatusUpdated(object o)
        {
            UpdateStatus();
        }

        private bool CanUndoCommand(object o) => HasChangesPending;
        private bool CanRedoCommand(object o) => HasRevertedChanges;

        private void OnUndoCommand(object o)
        {
            var undo = DiffStack.Pop();

            undo.Undo();
            SaveStack.Push(undo);

            UpdateStatus($"Reverted changes to texture {undo.Backup}");
        }

        private void OnRedoCommand(object o)
        {
            var redo = SaveStack.Pop();

            redo.Apply();
            DiffStack.Push(redo);

            UpdateStatus($"Restored changes to texture {redo.Backup}");
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
            var item1 = input.Item as TextureTreeItem;
            var item2 = output.Item as TextureTreeItem;

            var diff = new DiffState(owner, item1, item2);

            SaveStack.Clear();
            DiffStack.Push(diff);

            if (diff.Apply())
            {
                UpdateStatus($"Texture {diff.Backup} diff'd against {diff.Input}");
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

            DiffStack = new Stack<DiffState>();
            SaveStack = new Stack<DiffState>();

            InitializeComponent();

            ViewLeft.OpenModelsFileCommand.ExecuteDelegate += OnStatusUpdated;
            ViewLeft.SaveModelsFileCommand.ExecuteDelegate += OnStatusUpdated;
            ViewLeft.CloseModelsFileCommand.ExecuteDelegate += OnStatusUpdated;

            ViewRight.OpenModelsFileCommand.ExecuteDelegate += OnStatusUpdated;
            ViewRight.SaveModelsFileCommand.ExecuteDelegate += OnStatusUpdated;
            ViewRight.CloseModelsFileCommand.ExecuteDelegate += OnStatusUpdated;
        }
    }
}
