using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

using Microsoft.Win32;

using DSCript;
using DSCript.Models;

using FreeImageAPI;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for MaterialEditor.xaml
    /// </summary>
    public partial class MaterialEditor : Window, INotifyPropertyChanged
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

        public Image TextureBox = new Image() {
            Name="TextureBox",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        public TextBlock TextBlock = new TextBlock() { Name = "TextBlock" };

        bool globalMaterials = false;

        public bool ShowGlobalMaterials
        {
            get { return globalMaterials; }
            set
            {
                globalMaterials = value;

                MaterialsBox.Header = "Global Materials";
                
                if (IsVisible)
                    RaisePropertyChanged("Materials");
            }
        }

        PCMPTexture selectedTexture;

        public PCMPTexture SelectedTexture
        {
            get { return selectedTexture; }
            set
            {
                selectedTexture = value;
                RaisePropertyChanged("Texture");
            }
        }

        public BitmapSource Texture
        {
            get
            {
                if (SelectedTexture == null)
                    return null;

                TextureBox.Width = SelectedTexture.Width;
                TextureBox.Height = SelectedTexture.Height;

                return (TextureCache.GetCachedTexture(SelectedTexture).GetBitmapSource());
            }
        }

        public List<MaterialTreeItem> Materials
        {
            get
            {
                List<MaterialTreeItem> materials = new List<MaterialTreeItem>();

                int count = 0;

                if (ShowGlobalMaterials)
                {
                    if (Parent.ModelFile.HasSpooledFile)
                        foreach (PCMPMaterial material in Parent.ModelFile.SpooledFile.MaterialData.Materials)
                            materials.Add(new MaterialTreeItem(++count, material));
                }
                else  if (Parent.SelectedModelPackage.HasMaterials)
                {
                    foreach (PCMPMaterial material in Parent.SelectedModelPackage.MaterialData.Materials)
                        materials.Add(new MaterialTreeItem(++count, material));
                }
                    
                return materials;
            }
        }

        public void UpdateMaterials()
        {
            RaisePropertyChanged("Materials");
        }

        void MaterialsList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (MaterialsList.SelectedItem is PCMPTexture)
            {
                PCMPTexture tex = (PCMPTexture)MaterialsList.SelectedItem;

                if (ViewBox.Children.Count == 0 || (ViewBox.Children.Count > 0 && (!(ViewBox.Children[0] is Image))))
                {
                    ViewBox.Children.Clear();
                    ViewBox.Children.Add(TextureBox);

                    TextureBox.UpdateLayout();
                }

                if (ViewBox.Children.Count > 0 && ViewBox.Children[0] is Image)
                    SelectedTexture = tex;
            }
            else if (MaterialsList.SelectedItem is SubMaterialTreeItem)
            {
                PCMPSubMaterial subMat = ((SubMaterialTreeItem)MaterialsList.SelectedItem).SubMaterial;

                if (ViewBox.Children.Count == 0 || (ViewBox.Children.Count > 0 && (!(ViewBox.Children[0] is TextBlock))))
                {
                    ViewBox.Children.Clear();
                    ViewBox.Children.Add(TextBlock);

                    TextBlock.UpdateLayout();
                }

                bool transparency = false;

                bool damage = false;
                bool mask = false;

                bool specular = false;
                bool emissive = false;

                uint type = subMat.Flags;
                uint spec = subMat.Mode;
                uint flags = subMat.Type;

                if (flags == 0x400 || flags == 0x1000)
                    mask = true;
                if (flags == 0x800 || flags == 0x1000)
                    damage = true;
                if (spec == 0x201 || spec == 0x102)
                    specular = true;
                if (((type & 0x18000) == 0x18000) || ((type & 0x1E) == 0x1E))
                    emissive = true;
                if (((type & 0x1) == 0x1 && !specular) || type == 0x4 && !specular)
                    transparency = true;

                StringBuilder str = new StringBuilder();

                str.AppendLine("== SubMaterial Information ==");

                str.AppendFormat("Unk1: 0x{0:X8}", subMat.Flags).AppendLines(2);

                str.AppendFormat("Unk2: 0x{0:X4}", subMat.Mode).AppendLine();
                str.AppendFormat("Unk3: 0x{0:X4}", subMat.Type).AppendLines(2);

                str.AppendFormat("Transparent: {0}", transparency).AppendLine();
                str.AppendFormat("Damage: {0}", damage).AppendLine();
                str.AppendFormat("Mask: {0}", mask).AppendLine();
                str.AppendFormat("Specular: {0}", specular).AppendLine();
                str.AppendFormat("Emissive: {0}", emissive).AppendLine();

                if (ViewBox.Children.Count > 0 && ViewBox.Children[0] is TextBlock)
                    TextBlock.Text = str.ToString();
            }
            else
            {
                if (SelectedTexture != null)
                    SelectedTexture = null;
                if (ViewBox.Children.Count > 0)
                    ViewBox.Children.Clear();
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string initialDirectory = DSC.Configuration.GetDirectory("Driv3r");

            OpenFileDialog replaceTexture = new OpenFileDialog() {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "DDS Texture|*.dds",
                Title = "Choose DDS Texture:",
                //InitialDirectory = initialDirectory,
                ValidateNames = true
            };

            if (replaceTexture.ShowDialog() ?? false)
            {
                using (FileStream ddsFile = File.Open(replaceTexture.FileName, FileMode.Open))
                {
                    byte[] buffer = new byte[ddsFile.Length];

                    ddsFile.Read(buffer, 0, buffer.Length);
                    SelectedTexture.Buffer = buffer;

                    CachedTexture tex = TextureCache.GetCachedTexture(SelectedTexture);

                    tex.Reload();

                    BitmapSource bmap = tex.GetBitmapSource();

                    tex.Texture.Width = Convert.ToUInt16(bmap.Width);
                    tex.Texture.Height = Convert.ToUInt16(bmap.Height);

                    Parent.DEBUG_ExportModelPackage();
                    Parent.LoadSelectedModel();

                    if (!ShowGlobalMaterials)
                    {
                        if (Parent.IsTextureViewerOpen)
                            Parent.TextureViewer.ReloadTexture();
                    }

                    RaisePropertyChanged("Texture");
                }
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            SelectedTexture.ExportFile(@"C:\Users\Mark\Desktop\temp.dds");
            DSC.Log("Temporary file exported.");
        }

        private void AddMaterialTemplate(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem)
            {
                MenuItem item = (MenuItem)e.OriginalSource;

                PCMPData material = (ShowGlobalMaterials)
                    ? Parent.ModelFile.SpooledFile.MaterialData
                    : (Parent.SelectedModelPackage != null)
                        ? Parent.SelectedModelPackage.MaterialData
                        : null;

                if (material == null)
                    throw new Exception("FATAL ERROR: Failed to get MaterialData from ModelPackage!");
                if (item.Tag == null)
                    throw new Exception("ERROR: Item has no tag!");

                string tag = ((string)item.Tag).ToUpper();

                switch (tag)
                {
                case "STANDARD":
                    {
                        PCMPMaterial newMtl = new PCMPMaterial();

                        PCMPSubMaterial subMtl = new PCMPSubMaterial();
                        PCMPTexture texInfo = new PCMPTexture();

                        texInfo.Buffer  = EmbedRes.GetBytes("notex.dds");
                        
                        texInfo.Width   = 128;
                        texInfo.Height  = 128;

                        texInfo.Type    = 1;

                        subMtl.Textures.Add(texInfo);
                        material.Textures.Add(texInfo);

                        subMtl.Flags = 4;

                        subMtl.Mode = 0;
                        subMtl.Type = 0;

                        newMtl.SubMaterials.Add(subMtl);
                        material.SubMaterials.Add(subMtl);

                        material.Materials.Add(newMtl);

                        UpdateMaterials();
                    } break;
                default:
                    MessageBox.Show("Not implemented", "Material Editor", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                }
            }
        }

        public MaterialEditor(MainWindow parent)
        {
            InitializeComponent();

            Parent = parent;
            Owner = parent;

            DataContext = this;

            Titlebar.MouseLeftButtonDown += (o, e) => {
                DragMove();
            };

            TextureBox.SetBinding(Image.SourceProperty, new Binding("Texture"));

            MaterialsList.SelectedItemChanged += MaterialsList_SelectedItemChanged;

            addMtlMenu.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(AddMaterialTemplate));

            btnClose.Click += (o, e) => Close();
        }
    }

    public class SubMaterialTreeItem
    {
        public string Name { get; private set; }

        public PCMPSubMaterial SubMaterial { get; private set; }

        public List<PCMPTexture> Textures
        {
            get { return (SubMaterial != null) ? SubMaterial.Textures : null; }
        }

        public SubMaterialTreeItem(int id, PCMPSubMaterial subMaterial)
        {
            Name = String.Format("SubMaterial {0}", id);
            SubMaterial = subMaterial;
        }
    }

    public class MaterialTreeItem
    {
        public string Name { get; private set; }
        
        public PCMPMaterial Material { get; private set; }

        public List<SubMaterialTreeItem> SubMaterials
        {
            get
            {
                List<SubMaterialTreeItem> subMaterials = new List<SubMaterialTreeItem>();

                int count = 0;

                foreach (PCMPSubMaterial subMaterial in Material.SubMaterials)
                    subMaterials.Add(new SubMaterialTreeItem(++count, subMaterial));

                return subMaterials;
            }
        }

        public MaterialTreeItem(int id, PCMPMaterial material)
        {
            Name = String.Format("Material {0}", id);
            Material = material;
        }
    }
}
