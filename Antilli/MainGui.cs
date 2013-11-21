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

        ModelFile ModelFile { get; set; }

        List<ModelsPackage> ModelPackages
        {
            get { return ModelFile.Models; }
        }

        ChunkFile ChunkFile { get; set; }
        List<ModelPackage> MPackages { get; set; }

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
                // Setup events
                mn_File_Open.Click += (o, e) => { ChooseFile(); };
                mn_File_Save.Click += (o, e) => { SaveFile();  };
                mn_File_Exit.Click += (o, e) => { Application.Exit(); };

                mn_View_Models.Click += (o, e) => { /* PopulateLists(); */ };

                mn_Tools_ExportOBJ.Click += (o, e) => { SaveOBJFile(); };

                MeshBuilder box = new MeshBuilder();

                box.AddBox(new Point3D(0, 0, 0), 1, 1, 1);

                MeshGeometry3D mesh = box.ToMesh();

                Material material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 128, 0, 0)));

                GeometryModel3D model = new GeometryModel3D(mesh, material);

                Viewer.Models.Content = model;
            }
            catch (Exception e)
            {
                Console.WriteLine("Antilli::Exception -> {0}", e);
            }
        }

        public void SaveOBJFile()
        {
            SaveFileDialog file = new SaveFileDialog() {
                CheckPathExists = true,
                Filter = "Wavefront .OBJ|*.obj",
                ValidateNames = true,
                OverwritePrompt = true,
                AddExtension = true,
                DefaultExt = ".obj"
            };
            
            DialogResult result = file.ShowDialog();
            
            if (result == DialogResult.OK)
            {
                //MessageBox.Show(String.Format("I would be creating the file '{0}' right about now...", file.FileName));
            
                //OBJExporter.ExportOBJ(file.FileName, ModelPackages[idx]);
            }
        }

        public void ExportModelPackage(ModelsPackage modelsPackage)
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
                OBJExporter.ExportOBJ(file.FileName, modelsPackage);
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
                switch (Path.GetExtension(file.FileName))
                {
                case ".vvs":
                    ModelFile = new VVSFile(file.FileName); break;
                default:
                    ModelFile = new ModelFile(file.FileName); break;
                }

                //ExportModelPackage(ModelFile.Models[0]);

                bool useOldMethod = false;

                if (useOldMethod)
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
                        vertices.Add(v.Position.ToPoint3D());
                        normals.Add(v.Normals.ToVector3D());
                        coords.Add(v.UVMap.ToPoint());
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
                else
                {
                    if (PackList.Items.Count > 0)
                        PackList.Items.Clear();
                    if (MeshList.Items.Count > 0)
                        MeshList.Items.Clear();

                    for (int i = 0; i < ModelFile.Models.Count; i++)
                        PackList.Items.Add(String.Format("Model Package {0}", i));

                    PackList.SelectedIndexChanged += (o, e) => {
                        MeshList.Items.Clear();

                        int idx = PackList.SelectedIndex;

                        for (int i = 0; i < ModelFile.Models[idx].Parts.Count; i++)
                        {
                            long uid = ModelFile.Models[idx].Parts[i].UID;

                            if (!MeshList.Items.Contains(uid))
                                MeshList.Items.Add(uid);
                        }

                        MeshList.SelectedIndex = 0;
                    };

                    MeshList.SelectedIndexChanged += (o, e) => {
                        int modelIdx = PackList.SelectedIndex;
                        int partIdx = MeshList.SelectedIndex;

                        LoadMeshParts(ModelFile.Models[modelIdx], ModelFile.Models[modelIdx].Parts.Find((p) => p.UID == (long)MeshList.Items[partIdx]));
                    };

                    PackList.SelectedIndex = 0;

                    //LoadMeshParts(ModelFile.Models[2]);
                }
            }
        }

        public void LoadMeshParts(ModelsPackage modelPackage)
        {
            LoadMeshParts(modelPackage, null);
        }

        public void LoadMeshParts(ModelsPackage modelPackage, PartsGroup partBasedOn)
        {
            Model3DGroup models = new Model3DGroup();

            Viewer.Models.Content = models;

            Color[] colors = {
                                Color.FromArgb(255, 255, 128, 128),
                                Color.FromArgb(255, 128, 255, 128),
                                Color.FromArgb(255, 128, 128, 255),
                                Color.FromArgb(255, 128, 32, 32),
                                Color.FromArgb(255, 32, 128, 32),
                                Color.FromArgb(255, 32, 32, 128),
                                Color.FromArgb(255, 128, 255, 32),
                                Color.FromArgb(255, 32, 128, 255),
                                Color.FromArgb(255, 255, 128, 32),
                                Color.FromArgb(255, 128, 128, 255),
                                Color.FromArgb(255, 128, 255, 255),
                                Color.FromArgb(255, 128, 255, 128),
                                Color.FromArgb(255, 32, 255, 255),
                                Color.FromArgb(255, 255, 32, 255),
                                Color.FromArgb(255, 255, 255, 255),
                                Color.FromArgb(255, 32, 32, 32),
                                Color.FromArgb(255, 31, 41, 76),
                                Color.FromArgb(255, 41, 76, 31),
                                Color.FromArgb(255, 76, 41, 31),
                                Color.FromArgb(255, 76, 31, 41),
                                Color.FromArgb(255, 76, 31, 76),
                                Color.FromArgb(255, 31, 76, 76),
                                Color.FromArgb(255, 31, 76, 31),
                            };

            int colorIdx = 0;

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
                    //int colorIdx = new Random((int)DateTime.Now.ToBinary()).Next(0, colors.Length);

                    foreach (IndexedPrimitive prim in group.Meshes)
                    {
                        Mesh mesh = Mesh.Create(modelPackage, prim, false);

                        int nVertices = mesh.Vertices.Count;

                        //Vector3DCollection normals = new Vector3DCollection(nVertices);

                        MeshBuilder meshBuilder = new MeshBuilder(true, false);

                        for (int t = 0; t < mesh.Faces.Count; t++)
                        {
                            int i0, i1, i2;

                            i0 = mesh.Faces[t].P1 + prim.BaseVertexIndex;
                            i1 = mesh.Faces[t].P2 + prim.BaseVertexIndex;
                            i2 = mesh.Faces[t].P3 + prim.BaseVertexIndex;

                            Models.Vertex v1 = modelPackage.Vertices[i0];
                            Models.Vertex v2 = modelPackage.Vertices[i1];
                            Models.Vertex v3 = modelPackage.Vertices[i2];

                            List<Point3D> positions = new List<Point3D>(nVertices) {
                                v1.Position.ToPoint3D(true),
                                v2.Position.ToPoint3D(true),
                                v3.Position.ToPoint3D(true)
                            };

                            List<Vector3D> normals = new List<Vector3D>(nVertices) {
                                v1.Normals.ToVector3D(),
                                v2.Normals.ToVector3D(),
                                v3.Normals.ToVector3D()
                            };

                            List<System.Windows.Point> coords = new List<System.Windows.Point>(nVertices) {
                                v1.UVMap.ToPoint(),
                                v2.UVMap.ToPoint(),
                                v3.UVMap.ToPoint()
                            };

                            meshBuilder.AddTriangleStrip(positions, normals, coords);
                        }

                        MeshGeometry3D geometry = meshBuilder.ToMesh();

                       //int idx = (colorIdx % 3 == 0.0) 
                       //             ? colorIdx++
                       //             : new Random((int)DateTime.Now.ToBinary() * new Random((int)DateTime.Now.ToBinary()).Next(21, 100 * colorIdx)).Next(0, colors.Length);

                        Material material = new DiffuseMaterial(new SolidColorBrush(colors[(colorIdx < colors.Length) ? colorIdx++ : colorIdx = 0])) {
                            AmbientColor = Color.FromArgb(255, 128, 128, 128)
                        };

                        models.Children.Add(new GeometryModel3D() {
                            Geometry        = geometry,
                            Material        = material,
                            BackMaterial    = material
                        });   
                    }
                }
            }
        }

        /// <summary>Not implemented!</summary>
        public void SaveFile()
        {
            throw new NotImplementedException();
        }

        // private void PopulateLists()
        // {
        //     if (ModelPackages != null)
        //     {
        //         Console.WriteLine("Adding items.");
        // 
        //         PackList.SelectedIndexChanged += (o, ee) => {
        //             MeshList.Items.Clear();
        // 
        //             for (int i = 0; i < ModelPackages[PackList.SelectedIndex].Groups.Count; i++)
        //                 MeshList.Items.Add(ModelPackages[PackList.SelectedIndex].Groups[i].UID.ToString("X"));
        //         };
        // 
        //         if (ModelPackages.Count != 1)
        //         {
        //             for (int i = 0; i < ModelPackages.Count; i++)
        //                 PackList.Items.Add(i + 1);
        // 
        //             return;
        //         }
        // 
        //         PackList.Items.Add("1");
        //     }
        // }
    }
}
