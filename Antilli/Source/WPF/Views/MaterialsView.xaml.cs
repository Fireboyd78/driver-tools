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
using System.Xml;

using Microsoft.Win32;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for MaterialsView.xaml
    /// </summary>
    public partial class MaterialsView : EditorControl
    {
        object SelectedObject = null;
        bool SkipNextUpdate = false;

        public bool IsFileOpened
        {
            get { return AT.CurrentState.IsFileOpened; }
        }

        public bool CanEditMaterials
        {
            get
            {
#if DEBUG
                return IsFileOpened;
#else
                // currently for Debug builds only, sorry!
                return false;
#endif
            }
        }

        public bool CanShowGlobals
        {
            get { return AT.CurrentState.CanUseGlobals; }
        }

        public int MatTexRowSpan
        {
            get { return (AT.CurrentState.CanUseGlobals) ? 1 : 2; }
        }

        public List<MaterialTreeItem> Materials
        {
            get
            {
                var package = AT.CurrentState.SelectedModelPackage;

                if (package == null || !package.HasMaterials)
                    return null;

                var materials = new List<MaterialTreeItem>();
                int count = 0;

                foreach (var material in package.Materials)
                    materials.Add(new MaterialTreeItem(++count, material) { Owner = package });

                return materials;
            }
        }

        public List<MaterialTreeItem> GlobalMaterials
        {
            get
            {
                var modelFile = AT.CurrentState.ModelFile as IVehiclesFile;

                if (modelFile == null || !modelFile.HasGlobals)
                    return null;

                var globals = modelFile.GlobalTextures;

                var materials = new List<MaterialTreeItem>();
                int count = 0;

                foreach (var material in globals.Materials)
                    materials.Add(new MaterialTreeItem(++count, material) { Owner = globals });

                return materials;
            }
        }

        private void UpdateImageWidget()
        {
            var obj = SelectedObject;

            if (obj != null)
            {
                if (obj is MaterialTreeItem)
                    MaterialViewWidget.SetMaterial(((MaterialTreeItem)obj).Material);

                else if (obj is SubstanceTreeItem)
                    MaterialViewWidget.SetSubstance(((SubstanceTreeItem)obj).Substance);

                else if (obj is TextureTreeItem)
                    MaterialViewWidget.SetTexture(((TextureTreeItem)obj).Texture);
            }
        }

        private void OnMaterialListSelectionChanged(object sender, RoutedEventArgs e)
        {
            var source = e.Source as TreeView;

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
                    MaterialViewWidget.SetTexture(texture);
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

                MaterialViewWidget.SetTexture(item.Texture);
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

        private void AddSubstance(object sender, RoutedEventArgs e)
        {
            var item = ((sender as FrameworkElement).DataContext) as MaterialTreeItem;

            if (item != null)
            {
                var package = item.Owner;

                var material = item.Material as MaterialDataPC;

                SubstanceDataPC lastSubstance = material.Substances.LastOrDefault();

                var chooser = new MKChooserBox("Substance Creator", "Select a substance type:", new[] {
                    "Basic",
                    "Vehicle Body",
                    "Vehicle Body w/ Color Mask",
                    "Vehicle Body w/ Damage",
                    "Vehicle Body w/ Damage + Color Mask",
                    "Vehicle Lights",
                    "Vehicle Wheel",
                })
                {
                    ShowOptionCheckbox = true,
                    OptionName = "Use DRIV3R Format",
                };

                chooser.ShowDialog();

                var substance = new SubstanceDataPC();

                switch (chooser.SelectedIndex)
                {
                case 0: // Basic
                    substance.Bin = RenderBinType.Building;
                    substance.Flags = 4;
                    substance.TS1 = 0;
                    substance.TS2 = 0;
                    substance.TS3 = 0;
                    substance.TextureFlags = 0;
                    break;
                case 1: // Vehicle Body
                    substance.Bin = RenderBinType.Car;
                    substance.Flags = 0x14;
                    substance.TS1 = 0;
                    substance.TS2 = 0;
                    substance.TS3 = 0;
                    substance.TextureFlags = 0;
                    break;
                case 2: // Vehicle Body w/ Color Mask
                    substance.Bin = RenderBinType.Car;
                    substance.Flags = 0x14;
                    substance.TS1 = 0;
                    substance.TS2 = 0;
                    substance.TS3 = 0;
                    substance.TextureFlags = 0;
                    break;
                case 3: // Vehicle Body w/ Damage
                    substance.Bin = RenderBinType.Car;
                    substance.Flags = 0x14;
                    substance.TS1 = 0;
                    substance.TS2 = 0;
                    substance.TS3 = 0;
                    substance.TextureFlags = 0;
                    break;
                case 4: // Vehicle Body w/ Damage + Color Mask
                    substance.Bin = RenderBinType.Car;
                    substance.Flags = 0x14;
                    substance.TS1 = 0;
                    substance.TS2 = 0;
                    substance.TS3 = 0;
                    substance.TextureFlags = 0;
                    break;
                case 5: // Vehicle Lights
                    substance.Bin = RenderBinType.FullBrightOverlay;
                    substance.Flags = 0x4;
                    substance.TS1 = 0;
                    substance.TS2 = 0;
                    substance.TS3 = 0;
                    substance.TextureFlags = 0;
                    break;
                case 6: // Vehicle Wheel
                    substance.Bin = RenderBinType.Car;
                    substance.Flags = 4;
                    substance.TS1 = 0;
                    substance.TS2 = 0;
                    substance.TS3 = 0;
                    substance.TextureFlags = 0;
                    break;
                }
                
                material.Substances.Add(substance);

                if (lastSubstance == null)
                    throw new Exception("Material has no existing substances. Too lazy to resolve this error.");

                var index = package.Substances.IndexOf(lastSubstance);

                if (index == -1)
                    throw new Exception("Something went horribly wrong !!");

                // insert the substance into the package
                package.Substances.Insert(index + 1, substance);
                package.NotifyChanges();

                TreeView tv = null;

                if (package is IVehiclesFile)
                {
                    tv = GlobalMaterialsList;
                    NotifyChange("GlobalMaterials");
                }
                else
                {
                    tv = MaterialsList;
                    NotifyChange("Materials");
                }

                // reselect the material..
                TreeViewItem node = null;
                if (TrySelectMaterial(tv, material, out node))
                {
                    node.Focus();
                    node.IsExpanded = true;
                }
            }
        }

        private bool TrySelectMaterial(TreeView tree, IMaterialData material, out TreeViewItem node)
        {
            node = null;

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

                        node = childNode;
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

        private bool TrySelectMaterial(TreeView tree, IMaterialData material)
        {
            TreeViewItem node = null;
            return TrySelectMaterial(tree, material, out node);
        }

        private bool AddMaterialTemplate(IMaterialPackage package, string template)
        {
            MaterialDataPC material = null;

            switch (template.ToUpper())
            {
            case "NONE":
                MessageBox.Show("No material template specified!", "Material Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                break;
            case "STANDARD":
            {
                material = new MaterialDataPC();

                var texture = new TextureDataPC();
                var substance = new SubstanceDataPC();

                texture.UID = 0x01010101;
                texture.Handle = new Random().Next();

                texture.Width = 128;
                texture.Height = 128;

                texture.Type = 1;

                substance.Textures.Add(texture);

                substance.Flags = 4;
                substance.TS1 = 0;
                substance.TS2 = 0;
                substance.TS3 = 0;
                substance.TextureFlags = 0;

                material.Substances.Add(substance);
            }
            break;
            default:
                MessageBox.Show($"Material template '{template}' not implemented.", "Material Editor", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
            }

            if (material != null)
            {
                foreach (var substance in material.Substances)
                {
                    package.Substances.Add(substance);

                    foreach (var texture in substance.Textures)
                        package.Textures.Add(texture);
                }

                package.Materials.Add(material);

                AT.CurrentState.IsFileDirty = true;
                package.NotifyChanges();

                return true;
            }

            return false;
        }

        private void OnAddMaterialTemplate(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as MenuItem;

            MenuItem parent = item.Parent as MenuItem;

            if (parent != null)
            {
                while (parent.Parent is MenuItem)
                    parent = parent.Parent as MenuItem;
            }
            else
            {
                // already the parent
                parent = item;
            }

            var treeView = ((ContextMenu)parent.Parent).PlacementTarget as TreeView;

            var template = "NONE";

            if (item != null)
            {
                var tag = item.Tag as string;

                if (tag != null)
                    template = tag;
            }

            if (treeView != null)
            {
                var type = treeView.Tag as string ?? "?";

                switch (type)
                {
                case "model":
                    var package = AT.CurrentState.SelectedModelPackage;
                    if (package != null && package.HasMaterials)
                    {
                        if (AddMaterialTemplate(package, template))
                            NotifyChange("Materials");
                    }
                    break;
                case "global":
                    var modelFile = AT.CurrentState.ModelFile as IVehiclesFile;
                    if (modelFile != null && modelFile.HasGlobals)
                    {
                        if (AddMaterialTemplate(modelFile.GlobalTextures, template))
                            NotifyChange("GlobalMaterials");
                    }
                    break;
                default:
                    MessageBox.Show("Operation failed.", "Material Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }
            }
        }

        private void OnShowModelUsage(object sender, RoutedEventArgs e)
        {
            var item = ((sender as FrameworkElement).DataContext) as MaterialTreeItem;

            var package = AT.CurrentState.SelectedModelPackage;

            var targetMaterial = item.Material as MaterialDataPC;
            var targetHandle = package.Materials.IndexOf(targetMaterial);

            var usedModels = new List<Model>();

            foreach (var submodel in package.SubModels)
            {
                var material = submodel.Material;

                if (material.UID != package.UID)
                    continue;

                if (material.Handle == targetHandle)
                    usedModels.Add(submodel.Model);
            }

            if (usedModels.Count > 0)
            {
                var message = new List<String>();

                message.Add($"Used by {usedModels.Count} model(s) in this package:");

                foreach (var model in usedModels)
                    message.Add($"\t{model.UID}");

                MessageBox.Show(String.Join("\r\n", message), "Material Editor", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("This material is not used by any models in this package.", "Material Editor", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public override void HandleKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
            case Key.X:
                var xmlDoc = new XmlDocument();

                var matPkg = xmlDoc.CreateElement("MaterialPackage");
                var outPath = Path.Combine(Settings.ExportDirectory, "MaterialPackage.xml");

                AT.CurrentState.SelectedModelPackage.SaveMaterials(matPkg);

                xmlDoc.AppendChild(matPkg);
                xmlDoc.Save(outPath);

                Debug.WriteLine($"Saved material package to '{outPath}'.");
                break;
            }

            MaterialViewWidget.OnKeyPressed(sender, e);

            base.HandleKeyDown(sender, e);
        }

        public override void ResetView()
        {
            SelectedObject = null;
            NotifyChange("CanEditMaterials");
            MaterialViewWidget.Clear();
        }

        public override void UpdateView()
        {
            if (SkipNextUpdate)
                SkipNextUpdate = false;
            else
                UpdateImageWidget();
        }

        public RelayCommand ExpandAllCommand { get; set; }
        public RelayCommand CollapseAllCommand { get; set; }

        public RelayCommand NewMaterialCommand { get; set; }

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var contextMenu = sender as ContextMenu;

            if (contextMenu != null)
            {
                var tag = contextMenu.Tag as string;

                if (tag != null)
                {
                    if (tag == "editor" && !CanEditMaterials)
                        contextMenu.IsOpen = false;
                }
            }
        }

        public MaterialsView()
        {
            InitializeComponent();

            AT.CurrentState.MaterialEditor = this;

            AT.CurrentState.PropertyChanged += (o, e) => {
                //Debug.WriteLine($">> State change: '{e.PropertyName}'");
                NotifyChange(e.PropertyName);
            };

            AT.CurrentState.MaterialSelectQueried += (o, e) => {
                var selected = AT.CurrentState.OnQuerySelection<TreeView, IMaterialData>(TrySelectMaterial, GlobalMaterialsList, MaterialsList, o, 1);

                AT.CurrentState.MaterialSelectQueryResult = (!selected) ? o : null;
            };

            ExpandAllCommand = new RelayCommand(delegate (object o)
            {
                var treeView = o as TreeView;

                if (treeView != null)
                    treeView.ExpandAll(true);
            });

            CollapseAllCommand = new RelayCommand(delegate (object o)
            {
                var treeView = o as TreeView;

                if (treeView != null)
                    treeView.ExpandAll(false);
            });
        }
    }
}
