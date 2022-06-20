using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using Microsoft.Win32;

using DSCript;
using DSCript.Models;
using DSCript.Spooling;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for TextureViewWidget.xaml
    /// </summary>
    public partial class TextureViewWidget : EditorControl
    {
        public struct Selection : IEqualityComparer<Selection>
        {
            public readonly ListBox Source;
            public readonly int Index;
            public readonly object Item;

            bool IEqualityComparer<Selection>.Equals(Selection x, Selection y)
            {
                if (ReferenceEquals(x.Source, y.Source) && ReferenceEquals(x.Item, y.Item))
                    return (x.Index == y.Index);

                return false;
            }

            int IEqualityComparer<Selection>.GetHashCode(Selection obj)
            {
                return Source.GetHashCode() * 642 ^ Item.GetHashCode() * 021 ^ Index.GetHashCode();
            }

            public Selection(ListBox source)
            {
                Source = source;
                Index = source.SelectedIndex;
                Item = source.SelectedItem;
            }
        }

        ModelFile m_modelFile;
        string m_filename;
        bool m_hasGlobals;
        Texture m_texture;
        Selection m_selection;

        public ModelFile ModelsFile
        {
            get { return m_modelFile; }
            set
            {
                if (m_modelFile != null)
                    m_modelFile.Dispose();

                m_modelFile = value;

                NotifyChange("IsFileOpened");
                NotifyChange("IsFileDirty");
                NotifyChange("Textures");
                NotifyChange("GlobalTextures");
            }
        }

        public string FileName
        {
            get { return m_filename; }
            set { SetValue(ref m_filename, value, "FileName"); }
        }

        public bool IsFileOpened
        {
            get { return (ModelsFile != null) && (ModelsFile.HasModels || ModelsFile.HasGlobals); }
        }

        public bool IsFileDirty
        {
            get { return IsFileOpened && ModelsFile.AreChangesPending; }
        }

        public Texture CurrentTexture
        {
            get { return m_texture; }
            set
            {
                if (m_texture != null)
                {
                    TextureCache.Release(m_texture);
                    TextureCache.Flush();
                }

                m_texture = value;

                NotifyChange("CurrentImage");
            }
        }

        public BitmapSource CurrentImage
        {
            get
            {                
                if (m_texture != null)
                    return m_texture.Bitmap?.GetBitmapSource(BitmapSourceLoadFlags.Default);

                return null;
            }
        }

        public Selection CurrentSelection
        {
            get { return m_selection; }
            set
            {
                // this may not always update, but we do need to update texture afterwards
                SetValue(ref m_selection, value, "CurrentSelection");

                // free the old texture from the cache!
                CurrentTexture = null;

                var item = value.Item as TextureTreeItem;

                Texture texture = null;

                if (item != null && item.Texture != null)
                    texture = TextureCache.GetTexture(item.Texture);

                CurrentTexture = texture;
            }
        }

        public bool HasGlobalTextures
        {
            get { return m_hasGlobals; }
            set { SetValue(ref m_hasGlobals, value, "HasGlobalTextures"); }
        }

        public int MatTexRowSpan
        {
            get { return m_hasGlobals ? 1 : 2; }
        }

        public List<TextureTreeItem> Textures
        {
            get
            {
                if (ModelsFile == null || !ModelsFile.HasModels)
                    return null;

                var textures = new List<TextureTreeItem>();
                int count = 0;

                foreach (var package in ModelsFile.Packages)
                {
                    if (package == null || !package.HasMaterials)
                        continue;

                    foreach (var texture in package.Textures)
                    {
                        //if (texture.Width < 64 || texture.Height < 64)
                        //    continue;

                        textures.Add(new TextureTreeItem(count++, texture, true) { Owner = package });
                    }
                }

                return textures;
            }
        }

        public List<TextureTreeItem> GlobalTextures
        {
            get
            {
                if (ModelsFile == null || !ModelsFile.HasGlobals)
                    return null;

                var globals = ModelsFile.GlobalTextures;

                var textures = new List<TextureTreeItem>();
                int count = 0;

                foreach (var texture in globals.Textures)
                {
                    //if (texture.Width < 64 || texture.Height < 64)
                    //    continue;

                    textures.Add(new TextureTreeItem(count++, texture, true) { Owner = globals });
                }

                return textures;
            }
        }

        private void OnTextureListSelectionChanged(object sender, RoutedEventArgs e)
        {
            var source = e.Source as ListBox;

            if (source != null)
            {
                // handles everything for us
                CurrentSelection = new Selection(source);
            }
        }

        public override void UpdateView()
        {
            if (CurrentSelection.Source != null)
            {
                // reselect the selection, releasing the old texture and reloading it
                CurrentSelection = new Selection(CurrentSelection.Source);
                CurrentSelection.Source.Items.Refresh();
            }

            NotifyChange("CurrentImage");
            NotifyChange("IsFileDirty");
        }

        public override void ResetView()
        {
            CurrentTexture = null;
            NotifyChange("CurrentImage");

            NotifyChange("IsFileDirty");
            NotifyChange("Textures");
            NotifyChange("GlobalTextures");
        }

        private bool AskUserPrompt(string message)
        {
            return MessageBox.Show(message, "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        private void OnFileOpened(string filename)
        {
            ModelsFile = new ModelFile() { AllowPackageRegistry = false };

            if (ModelsFile.Load(filename))
            {
                FileName = filename;

                // load everything
                foreach (var package in ModelsFile.Packages)
                    SpoolableResourceFactory.Load(package);

                NotifyChange("IsFileOpened");
                NotifyChange("Textures");
                NotifyChange("GlobalTextures");

                if (!ModelsFile.HasModels)
                    ModelsFile.Dispose();
            }
        }

        private void OnOpenModelsFileCommand(object sender)
        {
            var dialog = FileManager.OpenDialog;

            if (dialog.ShowDialog() ?? false)
            {
                dialog.InitialDirectory = Path.GetDirectoryName(dialog.FileName);
                OnFileOpened(dialog.FileName);

                if (!ModelsFile.IsLoaded)
                {
                    MessageBox.Show("Unsupported file type, please try another file.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);

                    ModelsFile = null;
                    FileName = null;
                }
            }
        }

        private void OnCloseModelsFileCommand(object sender)
        {
            bool readyToClose = true;

            if (IsFileDirty && !AskUserPrompt("All unsaved changes will be lost. Are you sure?"))
                readyToClose = false;

            if (readyToClose)
            {
                ModelsFile.Dispose();
                ModelsFile = null;

                FileName = null;
            }
        }

        private bool CanSaveModelsFileCommand(object sender)
        {
            return IsFileDirty;
        }

        private void OnSaveModelsFileCommand(object sender)
        {
            if (AskUserPrompt($"All pending changes will be saved to '{FileName}'. Do you wish to OVERWRITE the original file? (NO BACKUPS WILL BE CREATED)"))
            {
                if (ModelsFile.Save())
                {
                    NotifyChange("IsFileDirty");
                    MessageBox.Show($"Successfully saved changes to '{FileName}'!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to save '{FileName}'! No changes were made.", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public RelayCommand OpenModelsFileCommand { get; set; }
        public RelayCommand CloseModelsFileCommand { get; set; }
        public RelayCommand SaveModelsFileCommand { get; set; }

        public TextureViewWidget()
        {
            OpenModelsFileCommand = new RelayCommand(OnOpenModelsFileCommand);
            CloseModelsFileCommand = new RelayCommand(OnCloseModelsFileCommand);
            SaveModelsFileCommand = new RelayCommand(OnSaveModelsFileCommand, CanSaveModelsFileCommand);

            InitializeComponent();

            // shouldn't hook up here, at least not for now, anyways
            //AT.CurrentState.PropertyChanged += (o, e) => {
            //    NotifyChange(e.PropertyName);
            //};
        }
    }
}
