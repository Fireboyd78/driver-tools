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
                mn_File_Open.Click += (o, e) => { ChooseFile(); };
                mn_File_Save.Click += (o, e) => { SaveFile();  };
                mn_File_Exit.Click += (o, e) => { Application.Exit(); };

                mn_View_Models.Click += (o, e) => { /* PopulateLists(); */ };

                mn_Tools_ExportOBJ.Click += (o, e) => { SaveOBJFile(); };

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

                MeshList.SelectedIndexChanged += (o, e) => { DrawMeshes(); };

                ShowDamage.CheckedChanged += (o, e) => {
                    useBlendWeights = (ShowDamage.Checked) ? true : false;
                    DrawMeshes();
                };

                MeshBuilder box = new MeshBuilder();

                box.AddBox(new Point3D(0, 0, 0), 1, 1, 1);

                MeshGeometry3D mesh = box.ToMesh();

                Material material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)));

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

                LoadMeshParts(ModelFile.Models[modelIdx], ModelFile.Models[modelIdx].Parts.Find((p) => p.UID == (long)MeshList.Items[partIdx]));
            }
        }

        public void SaveOBJFile()
        {
            SaveFileDialog file = new SaveFileDialog() {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".obj",
                Filter = "Wavefront .OBJ|*.obj",
                OverwritePrompt = true,
                RestoreDirectory = true,
                ValidateNames = true
            };

            DialogResult result = file.ShowDialog();

            if (result == DialogResult.OK)
            {
                int modelIdx = PackList.SelectedIndex;
                int partIdx = MeshList.SelectedIndex;

                OBJExporter.ExportOBJ(file.FileName, ModelFile.Models[modelIdx], ModelFile.Models[modelIdx].Parts.Find((p) => p.UID == (long)MeshList.Items[partIdx]));
            }
        }

        /// <summary>Opens a dialog to select a chunk file</summary>
        public void ChooseFile()
        {
            OpenFileDialog file = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "All supported formats|*.vvs;*.vvv;*.vgt;*.d3c;*.pcs;*.cpr;*.dam;*.map;*.gfx;*.pmu;*.d3s;*.mec;*.bnk|Any file|*.*",
                ValidateNames = true,
            };

            DialogResult result = file.ShowDialog();

            if (result == DialogResult.OK)
            {
                IModelFile modelFile = null;

                Cursor = Cursors.WaitCursor;

                switch (Path.GetExtension(file.FileName))
                {
                case ".vvs":
                    modelFile = new VVSFile(file.FileName); break;
                case ".vvv":
                    modelFile = new VVVFile(file.FileName); break;
                default:
                    modelFile = new ModelFile(file.FileName); break;
                }

                bool useOldMethod = false;

                if (!useOldMethod)
                {
                    if (modelFile.Models != null)
                    {
                        ModelFile = modelFile;

                        Text = String.Format("{0} - {1}", title, file.FileName);

                        if (ModelFile.Models.Count > 0)
                            LoadModels();
                    }
                    else
                    {
                        MessageBox.Show("No models found!", "Antilli", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        ((ModelFile)modelFile).Dispose();
                    }

                    DSC.Log("Memory usage: {0:N}MB", GC.GetTotalMemory(true) / 1048576.0);
                }
                else // Use old method
                {
                    ModelsPackage mpak = ModelFile.Models[2];

                    Material mat = new DiffuseMaterial() {
                        Brush = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128))
                    };

                    int nVertices = mpak.Vertices.Buffer.Length;

                    Point3DCollection vertices = new Point3DCollection(nVertices);
                    Vector3DCollection normals = new Vector3DCollection(nVertices);
                    PointCollection coords = new PointCollection(nVertices);

                    Int32Collection indices = new Int32Collection();

                    foreach (Models.Vertex v in mpak.Vertices.Buffer)
                    {
                        vertices.Add(v.Positions);
                        normals.Add(v.Normals);
                        coords.Add(v.UVs);
                    }

                    foreach (PartsGroup part in mpak.Parts)
                    {
                        MeshGroup group = part.Parts[0].Group;

                        foreach (IndexedPrimitive prim in group.Meshes)
                        {
                            Mesh mesh = Mesh.Create(mpak, prim, false);

                            for (int t = 0; t < mesh.Faces.Count; t++)
                            {
                                indices.Add(mesh.Faces[t].P1 + prim.BaseVertexIndex);
                                indices.Add(mesh.Faces[t].P2 + prim.BaseVertexIndex);
                                indices.Add(mesh.Faces[t].P3 + prim.BaseVertexIndex);
                            }
                        }
                    }

                    MeshGeometry3D geometry = new MeshGeometry3D() {
                        Positions = vertices,
                        Normals = normals,
                        TextureCoordinates = coords,
                        TriangleIndices = indices
                    };

                    Viewer.Models.Content = new GeometryModel3D() {
                        Geometry = geometry,
                        Material = mat,
                        BackMaterial = mat
                    };
                }

                Cursor = Cursors.Default;
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
            LoadMeshParts(modelPackage, null);
        }

        public void LoadMeshParts(ModelsPackage modelPackage, PartsGroup partBasedOn)
        {
            Model3DGroup models = new Model3DGroup();

            Viewer.Models.Content = models;

            foreach (PartsGroup part in modelPackage.Parts)
            {
                int g = 0;

                MeshGroup group = part.Parts[g].Group;

                while (g < part.Parts.Count && part.Parts[g].Group == null)
                {
                    if (g + 1 > part.Parts.Count)
                        break;

                    group = part.Parts[g++].Group;
                }

                if (group != null && ((partBasedOn != null && part.UID == partBasedOn.UID) || partBasedOn == null))
                {
                    foreach (IndexedPrimitive prim in group.Meshes)
                    {
                        DriverModel3D model = new DriverModel3D(modelPackage, prim);
                        models.Children.Add(model.ToGeometry(useBlendWeights));
                    }
                }
            }

            ShowDamage.Enabled = (modelPackage.Vertices.VertexType == FVFType.Vertex15 && modelPackage.Parts.Count > 0) ? true : false;
            Viewer.VP3D.ZoomExtents();
            Viewer.VP3D.Camera.LookAt(new Point3D(0, 0 , Viewer.Models.Content.Bounds.SizeZ / 2.0), 0.0);
            Viewer.VP3D.CameraController.Zoom(-0.15);
        }

        /// <summary>Not implemented!</summary>
        public void SaveFile()
        {
            throw new NotImplementedException();
        }
    }
}
