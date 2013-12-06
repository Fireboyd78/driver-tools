using System;
using System.Collections.Generic;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

using HelixToolkit.Wpf;

using DSCript;
using DSCript.IO;

using Antilli.IO;
using Antilli.Models;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for WPFGui.xaml
    /// </summary>
    public partial class WPFGui : UserControl
    {
        IModelFile _modelFile;

        public IModelFile ModelFile
        {
            get { return _modelFile; }
            set
            {
                if (_modelFile != null)
                    ((ModelFile)_modelFile).Dispose();

                _modelFile = value;
            }
        }

        public List<ModelPackage> ModelPackages
        {
            get
            {
                if (ModelFile != null)
                    return (ModelFile.Models.Count > 0) ? ModelFile.Models : null;

                return null;
            }
        }

        public int PackageIndex
        {
            get { return PackList.SelectedIndex; }
            set { PackList.SelectedIndex = value; }
        }

        public int MeshIndex
        {
            get { return MeshList.SelectedIndex; }
            set { MeshList.SelectedIndex = value; }
        }

        public ModelPackage SelectedModelsPackage
        {
            get { return (ModelPackages != null) ? ModelPackages[PackageIndex] : null; }
        }

        public long SelectedMeshUID
        {
            get { return (MeshList.Items.Count > 0 && MeshList.SelectedItem.ToString() != "N/A") ? (long)MeshList.SelectedItem : -1; }
        }

        public void PopulatePackList()
        {
            PackList.Items.Clear();
            MeshList.Items.Clear();

            if (ModelPackages == null)
                return;

            for (int i = 0; i < ModelPackages.Count; i++)
                PackList.Items.Add(String.Format("Model Package {0}", i + 1));

            PackList.SelectedIndex = 0;
        }

        public void PopulateMeshList()
        {
            if (ModelPackages == null || PackageIndex == -1)
                return;

            nullModels = 0;

            MeshList.Items.Clear();

            ModelPackage modelPackage = ModelPackages[PackageIndex];

            if (modelPackage.Parts.Count > 0)
            {
                foreach (PartsGroup group in modelPackage.Parts)
                {
                    long uid = group.UID;

                    if (!MeshList.Items.Contains(uid))
                    {
                        if (group.Parts[0].Group != null)
                            MeshList.Items.Add(uid);
                        else
                            nullModels += 1;
                    }
                }
            }
            else
            {
                MeshList.Items.Add("N/A");
            }

            MeshList.SelectedIndex = 0;
        }

        public void LoadMeshes(bool zoomExtents = true)
        {
            if (ModelPackages == null || SelectedMeshUID == -1)
                return;

            foreach (ToggleButton lodType in LODTypes.Keys)
                lodType.IsEnabled = false;

            Model3DGroup models = new Model3DGroup();

            foreach (PartsGroup part in SelectedModelsPackage.Parts)
            {
                if (SelectedMeshUID != part.UID)
                    continue;

                for (int p = 0; p < part.Parts.Count; p++)
                {
                    PartDefinition partDef = part.Parts[p];

                    if (partDef.Group != null)
                        foreach (KeyValuePair<ToggleButton, int> lod in LODTypes)
                            if (lod.Value == p)
                            {
                                lod.Key.IsEnabled = true;
                                break;
                            }
                }

                MeshGroup group = part.Parts[LODTypes[CurrentLOD]].Group;

                if (group == null)
                    continue;

                Model3DGroup partModels = new Model3DGroup();

                foreach (IndexedPrimitive prim in group.Meshes)
                {
                    DriverModel3D model = new DriverModel3D(SelectedModelsPackage, prim);
                    partModels.Children.Add(model);
                }

                models.Children.Add(partModels);
            }

            if (SelectedModelsPackage.Vertices.VertexType == FVFType.Vertex15)
                Viewer.SetModel(models, true);
            else
                Viewer.SetModel(models);
        }

        public void LoadModelFile(IModelFile modelFile)
        {
            ModelFile = modelFile;

            TextureCache.Flush();

            PopulatePackList();
        }

        public void ToggleDamage()
        {
            DriverModel3D.UseBlendWeights = (Viewer.ShowDamage.IsChecked ?? false);
            LoadMeshes(false);
        }

        public ToggleButton _currentLOD;
        public ToggleButton CurrentLOD
        {
            get { return _currentLOD; }
            set
            {
                if (_currentLOD != null && _currentLOD != value)
                    _currentLOD.IsChecked = false;

                _currentLOD = value;
                _currentLOD.IsChecked = true;

                LoadMeshes();
            }
        }

        static Dictionary<ToggleButton, int> LODTypes = new Dictionary<ToggleButton, int>();
        static int nullModels = 0;

        public WPFGui()
        {
            InitializeComponent();

            PackList.SelectionChanged += (o, e) => {
                PopulateMeshList();

                if (nullModels > 0)
                    DSC.Log("{0} null models were skipped.", nullModels);
            };

            MeshList.SelectionChanged += (o, e) => LoadMeshes();

            Viewer.ShowDamage.Checked += (o, e) => ToggleDamage();
            Viewer.ShowDamage.Unchecked += (o, e) => ToggleDamage();

            LODTypes.Add(LODHigh, 0);
            LODTypes.Add(LODMedium, 1);
            LODTypes.Add(LODLow, 2);
            LODTypes.Add(LODVeryLow, 4);
            LODTypes.Add(LODShadow, 5);

            LODHigh.Checked    += (o, e) => { CurrentLOD = LODHigh; };
            LODMedium.Checked  += (o, e) => { CurrentLOD = LODMedium; };
            LODLow.Checked     += (o, e) => { CurrentLOD = LODLow; };
            LODVeryLow.Checked += (o, e) => { CurrentLOD = LODVeryLow; };
            LODShadow.Checked  += (o, e) => { CurrentLOD = LODShadow; };

            LODHigh.IsChecked = true;

            // Create a simple box
            MeshBuilder box = new MeshBuilder();

            box.AddBox(new Point3D(0, 0, 0), 1.5, 1.5, 1.5);

            Viewer.SetModel(new GeometryModel3D() {
                Geometry = box.ToMesh(),
                Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)))
            });
        }
    }
}
