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

        PCMPTexture currentTexture;

        public PCMPTexture CurrentTexture
        {
            get { return currentTexture; }
            set
            {
                currentTexture = value;
                RaisePropertyChanged("Texture");
            }
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

        public List<PCMPTexture> Textures
        {
            get
            {
                // if (ModelPackage.HasGlobalTextures)
                // {
                //     List<PCMPTextureInfo> textures = new List<PCMPTextureInfo>();
                // 
                //     foreach (PCMPTextureInfo texInfo in SelectedModelPackage.MaterialData.Textures)
                //         textures.Add(texInfo);
                // 
                //     foreach (PCMPMaterial gMat in ModelPackage.GlobalTextures)
                //         foreach (PCMPSubMaterial gSubMat in gMat.SubMaterials)
                //             foreach (PCMPTextureInfo gTexInfo in gSubMat.Textures)
                //             {
                //                 if (textures.Contains(gTexInfo))
                //                     continue;
                // 
                //                 textures.Add(gTexInfo);
                //             }
                // 
                //     return textures;
                // }

                List<PCMPTexture> textures = (Parent.SelectedModelPackage.HasMaterials)
                    ? new List<PCMPTexture>(Parent.SelectedModelPackage.MaterialData.Textures)
                    : new List<PCMPTexture>();

                if (Parent.ModelFile.HasSpooledFile)
                    foreach (PCMPTexture texture in Parent.ModelFile.SpooledFile.MaterialData.Textures)
                        textures.Add(texture);

                return textures;
            }
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
            if (TextureList.SelectedIndex != -1)
            {
                object item = TextureList.SelectedItem;

                if (item is PCMPTexture)
                {
                    CurrentTexture = (PCMPTexture)item;

                    TextureBox.Width = CurrentTexture.Width;
                    TextureBox.Height = CurrentTexture.Height;
                }
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
        }
    }
}