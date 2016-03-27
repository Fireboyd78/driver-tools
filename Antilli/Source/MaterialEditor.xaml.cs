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

        bool globalMaterials = false;

        public bool ShowGlobalMaterials
        {
            get { return globalMaterials; }
            set
            {
                globalMaterials = value;

                MaterialsBox.Header = "Global Materials";
                
                if (IsVisible)
                    RaisePropertyChanged(nameof(Materials));
            }
        }

        public TextureData SelectedTexture { get; private set; }

        public BitmapSource CurrentTexture
        {
            get
            {
                var item = MaterialsList.SelectedItem as TextureData;

                if (item != null)
                {
                    SelectedTexture = item;

                    TextureBox.Width = SelectedTexture.Width;
                    TextureBox.Height = SelectedTexture.Height;

                    TextureBox.Visibility = Visibility.Visible;

                    return (TextureCache.GetCachedTexture(SelectedTexture).GetBitmapSource());
                }
                else
                {
                    TextureBox.Visibility = Visibility.Collapsed;
                    return null;
                }
            }
        }

        public List<MaterialTreeItem> Materials
        {
            get
            {
                if (Parent == null)
                    return null;

                var materials = new List<MaterialTreeItem>();

                int count = 0;

                if (ShowGlobalMaterials)
                {
                    if (Parent.ModelFile is Driv3rVehiclesFile)
                    {
                        var modelFile = Parent.ModelFile as Driv3rVehiclesFile;

                        if (modelFile.HasVehicleGlobals)
                        {
                            var modelPackage = modelFile.VehicleGlobals.GetModelPackage();

                            if (modelPackage != null)
                            {
                                foreach (var material in modelPackage.Materials)
                                    materials.Add(new MaterialTreeItem(++count, material));
                            }
                        }
                    }
                }
                else  if (Parent.SelectedModelPackage.HasMaterials)
                {
                    foreach (var material in Parent.SelectedModelPackage.Materials)
                        materials.Add(new MaterialTreeItem(++count, material));
                }
                    
                return materials;
            }
        }

        public void UpdateMaterials()
        {
            RaisePropertyChanged(nameof(Materials));
        }

        public string ContentInfo
        {
            get
            {
                var item = MaterialsList.SelectedItem;

                if (item != null)
                {
                    StringBuilder str = new StringBuilder();

                    var col = 12;

                    if (item is MaterialTreeItem)
                    {
                        var material = ((MaterialTreeItem)item).Material;

                        str.AppendLine("== Material Information ==");

                        str.AppendColumn("Animated", col, true).AppendLine("{0}", material.Animated);
                        str.AppendColumn("AnimSpeed", col, true).AppendLine("{0}", material.AnimationSpeed);

                        InfoBox.Visibility = Visibility.Visible;
                    }
                    else if (item is SubstanceTreeItem)
                    {
                        var substance = ((SubstanceTreeItem)item).Substance;

                        str.AppendLine("== Substance Information ==");

                        str.AppendColumn("Flags", col, true).AppendLine("0x{0:X8}", substance.Flags);

                        str.AppendColumn("Mode", col, true).AppendLine("0x{0:X4}", substance.Mode);
                        str.AppendColumn("Type", col, true).AppendLine("0x{0:X4}", substance.Type);

                        str.AppendColumn("Transparent", col, true).AppendLine(substance.Transparency);
                        str.AppendColumn("Damage", col, true).AppendLine(substance.Damage);
                        str.AppendColumn("Mask", col, true).AppendLine(substance.AlphaMask);
                        str.AppendColumn("Specular", col, true).AppendLine(substance.Specular);
                        str.AppendColumn("Emissive", col, true).AppendLine(substance.Emissive);

                        InfoBox.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        InfoBox.Visibility = Visibility.Collapsed;
                        return null;
                    }

                    return str.ToString();
                }
                else
                {
                    InfoBox.Visibility = Visibility.Collapsed;
                    return null;
                }
            }
        }

        void MaterialsList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            RaisePropertyChanged(nameof(ContentInfo));
            RaisePropertyChanged(nameof(CurrentTexture));
        }

        private void ReplaceTexture(object sender, RoutedEventArgs e)
        {
            Parent.ReplaceTexture(SelectedTexture);
            RaisePropertyChanged(nameof(CurrentTexture));
        }

        private void ExportTexture(object sender, RoutedEventArgs e)
        {
            Parent.ExportTexture(SelectedTexture, this);
        }

        private void AddMaterialTemplate(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as MenuItem;

            if (item != null)
            {                
                ModelPackagePC modelPackage = null;

                if (item.Tag == null)
                    throw new Exception("ERROR: Item has no tag!");

                if (Parent.ModelFile is Driv3rVehiclesFile)
                {
                    var modelFile = Parent.ModelFile as Driv3rVehiclesFile;

                    if (ShowGlobalMaterials)
                        modelPackage = modelFile.VehicleGlobals.GetModelPackage();
                }

                if (modelPackage == null && Parent.SelectedModelPackage != null)
                    modelPackage = Parent.SelectedModelPackage;

                string tag = ((string)item.Tag).ToUpper();

                switch (tag)
                {
                case "STANDARD":
                    {
                        var newMtl = new MaterialData();

                        var subMtl = new SubstanceData();
                        var texInfo = new TextureData();

                        texInfo.Width   = 128;
                        texInfo.Height  = 128;

                        texInfo.Type    = 1;

                        subMtl.Textures.Add(texInfo);
                        modelPackage.Textures.Add(texInfo);

                        subMtl.Flags = 4;

                        subMtl.Mode = 0;
                        subMtl.Type = 0;

                        newMtl.Substances.Add(subMtl);
                        modelPackage.SubMaterials.Add(subMtl);

                        modelPackage.Materials.Add(newMtl);

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

            Titlebar.MouseLeftButtonDown += (o, e) => {
                DragMove();
            };

            MaterialsList.SelectedItemChanged += MaterialsList_SelectedItemChanged;

            addMtlMenu.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(AddMaterialTemplate));

            btnClose.Click += (o, e) => Close();
        }
    }

    public class SubstanceTreeItem
    {
        public string Name { get; private set; }

        public SubstanceData Substance { get; private set; }

        public List<TextureData> Textures
        {
            get { return (Substance != null) ? Substance.Textures : null; }
        }

        public SubstanceTreeItem(int id, SubstanceData subMaterial)
        {
            Name = String.Format("Substance {0}", id);
            Substance = subMaterial;
        }
    }

    public class MaterialTreeItem
    {
        public string Name { get; private set; }
        
        public MaterialData Material { get; private set; }

        public List<SubstanceTreeItem> Substances
        {
            get
            {
                List<SubstanceTreeItem> substances = new List<SubstanceTreeItem>();

                int count = 0;

                foreach (SubstanceData substance in Material.Substances)
                    substances.Add(new SubstanceTreeItem(++count, substance));

                return substances;
            }
        }

        public MaterialTreeItem(int id, MaterialData material)
        {
            Name = String.Format("Material {0}", id);
            Material = material;
        }
    }
}
