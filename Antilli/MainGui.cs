using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
//using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;

using HelixToolkit.Wpf;

using DSCript;
using DSCript.IO;
using DSCript.Methods;

using Antilli.IO;
using Antilli.Models;

// HACK: Fix discrepencies between a Form's 'DialogResult' and the enumeration
using DialogResult = System.Windows.Forms.DialogResult;


namespace Antilli
{
    public partial class MainGui : Form
    {
        public MainGui()
        {
            InitializeComponent();
            AntilliMain();
        }

        bool useBlendWeights = false;
        string title = "";

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

        public List<ModelsPackage> ModelPackages
        {
            get { return ModelFile.Models; }
        }

        public ChunkFile ChunkFile { get; set; }
        public List<ModelPackage> MPackages { get; set; }

        /// <summary>Checks to see if a chunk file is currently opened</summary>
        public bool FileOpened
        {
            get { return ((ModelFile != null) ? true : false); }
        }

        private void AntilliMain()
        {
            // Capture exceptions
            try
            {
                title = Text;

                // Setup events
                mn_File_Open.Click += (o, e) => OpenFile();
                mn_File_Save.Click += (o, e) => SaveFile();
                mn_File_Exit.Click += (o, e) => Application.Exit();

                //mn_View_Models.Click += (o, e) => PopulateLists();

                mn_Tools_ExportOBJ.Click += (o, e) => SaveOBJFile();

                PackList.SelectedIndexChanged += (o, e) => {
                    if (ModelFile.Models != null)
                    {
                        MeshList.Items.Clear();

                        int idx = PackList.SelectedIndex;

                        if (ModelFile.Models[idx].Parts.Count > 0)
                        {
                            for (int i = 0; i < ModelFile.Models[idx].Parts.Count; i++)
                            {
                                long uid = ModelFile.Models[idx].Parts[i].UID;

                                if (!MeshList.Items.Contains(uid))
                                    MeshList.Items.Add(uid);
                            }

                            MeshList.SelectedIndex = 0;
                        }
                        else
                            MeshList.Items.Add("N/A");
                    }
                };

                MeshList.SelectedIndexChanged += (o, e) => DrawMeshes();

                ShowDamage.CheckedChanged += (o, e) => {
                    useBlendWeights = (ShowDamage.Checked) ? true : false;
                    DrawMeshes();
                };

                ShowDamage.EnabledChanged += (o, e) => {
                    if (!ShowDamage.Enabled && ShowDamage.Checked)
                        ShowDamage.Checked = false;
                };

                MeshBuilder box = new MeshBuilder();
                
                box.AddBox(new Point3D(0, 0, 0), 1.5, 1.5, 1.5);
                
                MeshGeometry3D mesh = box.ToMesh();
                
                Material material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)));
                
                GeometryModel3D model = new GeometryModel3D(mesh, material);
                
                Viewer.Models.Content = model;

            }
            catch (Exception e)
            {
                Console.WriteLine("Antilli::Exception -> {0}", e);
            }
        }

        public void DrawMeshes()
        {
            if (ModelFile.Models != null)
            {
                int modelIdx = PackList.SelectedIndex;
                int partIdx = MeshList.SelectedIndex;

                LoadMeshParts(ModelFile.Models[modelIdx], (long)MeshList.Items[partIdx]);
            }
        }

        public void LoadModels()
        {
            if (PackList.Items.Count > 0)
                PackList.Items.Clear();
            if (MeshList.Items.Count > 0)
                MeshList.Items.Clear();

            for (int i = 0; i < ModelFile.Models.Count; i++)
                PackList.Items.Add(String.Format("Model Package {0}", i));

            PackList.SelectedIndex = 0;
        }

        public void LoadMeshParts(ModelsPackage modelPackage)
        {
            LoadMeshParts(modelPackage, -1);
        }

        public void LoadMeshParts(ModelsPackage modelPackage, long uid)
        {
            Model3DGroup models = new Model3DGroup();

            Viewer.Models.Content = models;

            foreach (PartsGroup part in modelPackage.Parts)
            {
                if (uid != -1 && part.UID != uid)
                    continue;

                // Load the "HIGH" mesh only until object selecting is implemented
                for (int g = 0; g < 1 /*part.Parts.Count*/; g++)
                {
                    MeshGroup group = part.Parts[g].Group;

                    if (group == null)
                        continue;

                    foreach (IndexedPrimitive prim in group.Meshes)
                    {
                        DriverModel3D model = new DriverModel3D(modelPackage, prim);
                        models.Children.Add(model.ToGeometry(useBlendWeights));
                    }
                }
            }

            ShowDamage.Enabled = (modelPackage.Vertices.VertexType == FVFType.Vertex15 && modelPackage.Parts.Count > 0) ? true : false;

            if (!ShowDamage.Checked)
            {
                Viewer.VP3D.ZoomExtents();
                Viewer.VP3D.Camera.LookAt(new Point3D(0, 0, Viewer.Models.Content.Bounds.SizeZ / 2.0), 0.0);
                Viewer.VP3D.CameraController.Zoom(-0.15);
            }
        }

        static OpenFileDialog openFile = new OpenFileDialog() {
            CheckFileExists = true,
            CheckPathExists = true,
            Filter = "All supported formats|*.vvs;*.vvv;*.vgt;*.d3c;*.pcs;*.cpr;*.dam;*.map;*.gfx;*.pmu;*.d3s;*.mec;*.bnk|Any file|*.*",
            ValidateNames = true,
        };

        static SaveFileDialog saveFile = new SaveFileDialog() {
            AddExtension = true,
            CheckPathExists = true,
            DefaultExt = ".obj",
            Filter = "Wavefront .OBJ|*.obj",
            OverwritePrompt = true,
            RestoreDirectory = true,
            ValidateNames = true
        };

        public void SaveOBJFile()
        {
            DialogResult result = saveFile.ShowDialog();

            if (result == DialogResult.OK)
            {
                int modelIdx = PackList.SelectedIndex;
                int partIdx = MeshList.SelectedIndex;

                OBJFile.Export(saveFile.FileName, ModelFile.Models[modelIdx], (long)MeshList.Items[partIdx]);
            }
        }

        /// <summary>Opens a dialog to select a chunk file</summary>
        public void OpenFile()
        {
            DialogResult result = openFile.ShowDialog();

            if (result == DialogResult.OK)
            {
                IModelFile modelFile = null;

                Cursor = Cursors.WaitCursor;

                switch (Path.GetExtension(openFile.FileName))
                {
                case ".vvs":
                    modelFile = new VVSFile(openFile.FileName);
                    break;
                case ".vvv":
                    modelFile = new VVVFile(openFile.FileName);
                    break;
                default:
                    modelFile = new ModelFile(openFile.FileName);
                    break;
                }

                if (modelFile.Models != null)
                {
                    ModelFile = modelFile;

                    Text = String.Format("{0} - {1}", title, openFile.FileName);

                    if (ModelFile.Models.Count > 0)
                        LoadModels();
                }
                else
                {
                    MessageBox.Show("No models found!", "Antilli", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    ((ModelFile)modelFile).Dispose();
                }

                DSC.Log("Memory usage: {0:N}MB", GC.GetTotalMemory(true) / 1048576.0);
                
                Cursor = Cursors.Default;
            }
        }

        /// <summary>Not implemented!</summary>
        public void SaveFile()
        {
            throw new NotImplementedException();
        }
    }
}
