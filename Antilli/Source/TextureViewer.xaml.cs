using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for TextureViewer.xaml
    /// </summary>
    public partial class TextureViewer : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementations
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }
        #endregion

        public new MainWindow Parent { get; private set; }

        bool useAlpha = false;
        bool alphaOnly = false;

        int texChannel
        {
            get { return (useAlpha) ? (alphaOnly) ? 2 : 1 : 0; }
        }

        TextureData currentTexture;

        public TextureData CurrentTexture
        {
            get { return currentTexture; }
            set
            {
                currentTexture = value;
                RaisePropertyChanged("Texture");
            }
        }

        public Visibility HasGlobals
        {
            get { return (Parent.ModelFile is Driv3rVehiclesFile) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public BitmapSource Texture
        {
            get
            {
                if (CurrentTexture == null)
                    return null;

                TextureBox.Width = CurrentTexture.Width;
                TextureBox.Height = CurrentTexture.Height;

                CachedTexture tex = TextureCache.GetCachedTexture(CurrentTexture);

                return tex.GetBitmapSource((BitmapSourceLoadFlags)texChannel);

                //switch (texChannel)
                //{
                //case 0: return tex.GetBitmapSource();
                //case 1: return tex.GetBitmapSource(true);
                //case 2: return tex.GetBitmapSourceAlphaChannel();
                //default: return null;
                //}
            }
        }

        public List<TextureData> Textures
        {
            get
            {
                List<TextureData> textures = (Parent.SelectedModelPackage.HasMaterials) ? new List<TextureData>(Parent.SelectedModelPackage.Textures) : new List<TextureData>();

                if (Parent.ModelFile is Driv3rVehiclesFile)
                    RaisePropertyChanged("GlobalTextures");
                else
                    RaisePropertyChanged("HasGlobals");

                return textures;
            }
        }

        public List<TextureData> GlobalTextures
        {
            get
            {
                var modelFile = Parent.ModelFile as Driv3rVehiclesFile;
                
                if (modelFile != null && modelFile.HasVehicleGlobals)
                {
                    var globals = modelFile.VehicleGlobals;

                    if (globals.HasTextures)
                    {
                        return new List<TextureData>(globals.StandaloneTextureData.ModelPackage.Textures);
                    }
                }

                return null;
            }
        }

        public void SelectTexture(TextureData texture)
        {
            var listBox = (TextureList.Items.Contains(texture)) ? TextureList : GlobalTextureList;

            if (listBox.SelectedItem == texture)
            {
                CurrentTexture = texture;
                return;
            }

            listBox.SelectedItem = texture;
            listBox.ScrollIntoView(texture);
        }

        public void UpdateTextures()
        {
            RaisePropertyChanged("Textures");
            TextureList.SelectedIndex = (Parent.SelectedModelPackage.HasMaterials) ? 0 : -1;
        }

        public void ReloadTexture()
        {
            RaisePropertyChanged("Texture");
        }

        void OnTextureSelected(object sender, SelectionChangedEventArgs e)
        {
            var list = sender as ListBox;

            if (list == null)
                return;

            if (list.SelectedIndex != -1)
            {
                var texture = list.SelectedItem as TextureData;

                if (texture != null)
                    CurrentTexture = texture;
            }
            else
            {
                CurrentTexture = null;   
            }

            RaisePropertyChanged("Texture");
        }

        public void OnKeyDownReceived(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
            case Key.A:
                {
                    if (CurrentTexture != null)
                    {
                        alphaOnly = (useAlpha) ? (alphaOnly) ? false : !alphaOnly : false;
                        useAlpha = (alphaOnly) ? true : !useAlpha;

                        RaisePropertyChanged("Texture");
                    }
                }
                break;
            default:
                break;
            }
        }
        
        public TextureViewer(MainWindow parent)
        {
            InitializeComponent();

            Owner = parent;
            Parent = parent;
            
            DataContext = this;

            Titlebar.MouseLeftButtonDown += (o, e) => {
                DragMove();
            };

            KeyDown += OnKeyDownReceived;

            btnClose.Click += (o, e) => Close();

            TextureList.SelectionChanged += OnTextureSelected;
            GlobalTextureList.SelectionChanged += OnTextureSelected;
        }

        private void ReplaceTexture(object sender, RoutedEventArgs e)
        {
            Parent.ReplaceTexture(CurrentTexture);
            RaisePropertyChanged("Texture");
            RaisePropertyChanged("Textures");
        }

        private void ExportTexture(object sender, RoutedEventArgs e)
        {
            Parent.ExportTexture(CurrentTexture, this);
        }
    }
}