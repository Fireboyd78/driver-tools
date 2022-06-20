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
    /// Interaction logic for TexturesView.xaml
    /// </summary>
    public partial class TexturesView : EditorControl
    {
        object SelectedObject = null;
        bool SkipNextUpdate = false;

        public bool CanShowGlobals
        {
            get { return AT.CurrentState.CanUseGlobals; }
        }

        public int MatTexRowSpan
        {
            get { return (AT.CurrentState.CanUseGlobals) ? 1 : 2; }
        }

        public List<TextureTreeItem> Textures
        {
            get
            {
                var package = AT.CurrentState.SelectedModelPackage;

                if (package == null || !package.HasMaterials)
                    return null;

                var textures = new List<TextureTreeItem>();
                int count = 0;

                foreach (var texture in package.Textures)
                    textures.Add(new TextureTreeItem(count++, texture) { Owner = package });

                return textures;
            }
        }

        public List<TextureTreeItem> GlobalTextures
        {
            get
            {
                var modelFile = AT.CurrentState.ModelFile as IVehiclesFile;

                if (modelFile == null || !modelFile.HasGlobals)
                    return null;

                var globals = modelFile.GlobalTextures;

                var textures = new List<TextureTreeItem>();
                int count = 0;

                foreach (var texture in globals.Textures)
                    textures.Add(new TextureTreeItem(count++, texture) { Owner = globals });

                return textures;
            }
        }

        private void UpdateImageWidget()
        {
            var obj = SelectedObject;

            if (obj != null)
            {
                if (obj is TextureTreeItem)
                    TextureViewWidget.SetTexture(((TextureTreeItem)obj).Texture);
            }
        }

        private void OnTextureListSelectionChanged(object sender, RoutedEventArgs e)
        {
            var source = e.Source as ListBox;

            if (source != null)
            {
                SelectedObject = source.SelectedItem;
                UpdateImageWidget();
            }
        }

        private void ReplaceTexture(ITextureData texture)
        {
            if (texture.Flags == -666)
            {
                MessageBox.Show("Can't do that, sorry!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var openDlg = new OpenFileDialog()
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "Texture files|*.dds;*.tga;*.bmp;",
                Title = "Select a file:",
                ValidateNames = true
            };

            if (openDlg.ShowDialog() ?? false)
            {
                var buffer = File.ReadAllBytes(openDlg.FileName);

                var type = "biff";

                if (Utils.TryGetImageFormat(buffer, out type)
                    || (buffer.Length == 0))
                {
                    var texRef = TextureCache.GetTexture(texture);
                    texRef.SetBuffer(buffer);

                    AT.CurrentState.ModelView.Viewer.UpdateActiveModel();

                    // reload the active texture if necessary
                    TextureViewWidget.SetTexture(texture);
                    SkipNextUpdate = true;

                    AT.CurrentState.UpdateEditors();

                    AT.CurrentState.IsFileDirty = true;
                    AT.CurrentState.SelectedModelPackage.NotifyChanges();
                }
                else
                {
                    MessageBox.Show("Invalid texture file selected!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
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

                item.UpdateName();

                TextureViewWidget.SetTexture(item.Texture);
            }
        }

        private void ExportTexture(object sender, RoutedEventArgs e)
        {
            var item = ((sender as FrameworkElement).DataContext) as TextureTreeItem;

            if (item != null)
            {
                var tex = item.Texture;
                TextureUtils.ExportTexture(tex);
            }
        }

        private bool ExportGlobalTextures(bool silent = false)
        {
            var modelFile = AT.CurrentState.ModelFile as IVehiclesFile;

            if (modelFile != null && modelFile.HasGlobals)
            {
                var globs = modelFile.GlobalTextures;

                return TextureUtils.ExportTextures(globs.Textures, silent);
            }

            if (!silent)
                MessageBox.Show("Nothing to export!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);

            return false;
        }

        private bool ExportTextures(ModelPackage modelPackage, bool silent = false)
        {
            var textures = modelPackage?.Textures;

            if (textures != null)
                return TextureUtils.ExportTextures(textures, silent);

            if (!silent)
                MessageBox.Show("Nothing to export!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);

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

        public override void HandleKeyDown(object sender, KeyEventArgs e)
        {
            TextureViewWidget.OnKeyPressed(sender, e);

            base.HandleKeyDown(sender, e);
        }

        public override void ResetView()
        {
            SelectedObject = null;
            TextureViewWidget.Clear();
        }

        public override void UpdateView()
        {
            if (SkipNextUpdate)
                SkipNextUpdate = false;
            else
                UpdateImageWidget();
        }

        public TexturesView()
        {
            InitializeComponent();

            AT.CurrentState.TextureEditor = this;

            AT.CurrentState.PropertyChanged += (o, e) => {
                //Debug.WriteLine($">> State change: '{e.PropertyName}'");
                NotifyChange(e.PropertyName);
            };

            AT.CurrentState.TextureSelectQueried += (o, e) => {
                var selected = AT.CurrentState.OnQuerySelection<ListBox, ITextureData>(TrySelectTexture, GlobalTextureList, TextureList, o, 2);

                if (!selected)
                    MessageBox.Show("No texture found!", "Antilli", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            btnExportAllTextures.Click += (o, e) => ExportTextures(AT.CurrentState.SelectedModelPackage);
            btnExportAllGlobalTextures.Click += (o, e) => ExportGlobalTextures();
        }
    }
}
