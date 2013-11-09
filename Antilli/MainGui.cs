using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DSCript;
using DSCript.IO;

using Antilli.IO;

// HACK: Fix discrepencies between a Form's 'DialogResult' and the enumeration
using DialogResult = System.Windows.Forms.DialogResult;

namespace Antilli
{
    public partial class MainGui : Form
    {
        /* =============== Constructor(s) =============== */
        public MainGui()
        {
            InitializeComponent();

            // Call the main method so as not to clutter up this constructor
            AntilliMain();
        }

        /* =============== Properties =============== */
        /// <summary>Gets or sets the chunk file used to retrieve data for models/textures</summary>
        ChunkReader ChunkFile { get; set; }

        List<ModelPackage> ModelPackages { get; set; }

        /// <summary>Checks to see if a chunk file is currently opened</summary>
        public bool FileOpened
        {
            get { return ((ChunkFile != null) ? true : false); }
        }

        /* ==================== Methods ==================== */
        private void AntilliMain()
        {
            // Capture exceptions
            try
            {
                // Setup events
                mn_File_Open.Click += (o, e) => { ChooseFile(); };
                mn_File_Save.Click += (o, e) => { SaveFile();  };
                mn_File_Exit.Click += (o, e) => { Application.Exit(); };

                mn_View_Models.Click += (o, e) => { PopulateLists(); };

                mn_Tools_ExportOBJ.Click += (o, e) => { SaveOBJFile(); };
            }
            catch (Exception e)
            {
                Console.WriteLine("Antilli::Exception -> {0}", e);
            }
        }

        public void SaveOBJFile()
        {
            int idx = PackList.SelectedIndex;
            if (idx >= 0 && ModelPackages[idx] != null)
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

                    OBJExporter.ExportOBJ(file.FileName, ModelPackages[idx]);
                }
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
                ChunkFile = new ChunkReader(file.FileName);
                ModelPackages = new List<ModelPackage>();

                ChunkBlock mChunk = ChunkFile.Chunk[0];

                PackList.Items.Clear();
                MeshList.Items.Clear();

                if (mChunk.Subs[0].Magic != 0x0)
                {
                    for (int c = (mChunk.Subs.Count / 2); c < mChunk.Subs.Count; c++)
                    {
                        ModelPackagePC model = new ModelPackagePC(mChunk.Subs[c]);
                        model.Read(ChunkFile);

                        ModelPackages.Add(model);
                    }
                }
                else
                {
                    ChunkBlock chunk = ChunkFile.Chunk[1];
                    SubChunkBlock models = chunk.Subs[(chunk.Subs.Count - 1)];

                    ModelPackagePC model = new ModelPackagePC(models);
                    model.Read(ChunkFile);

                    ModelPackages.Add(model);
                }

                PopulateLists();

                PackList.SelectedIndex = 0;
            }
        }

        /// <summary>Not implemented!</summary>
        public void SaveFile()
        {
            throw new NotImplementedException();
        }

        private void PopulateLists()
        {
            if (ModelPackages != null)
            {
                Console.WriteLine("Adding items.");

                PackList.SelectedIndexChanged += (o, ee) => {
                    MeshList.Items.Clear();

                    for (int i = 0; i < ModelPackages[PackList.SelectedIndex].Groups.Count; i++)
                        MeshList.Items.Add(ModelPackages[PackList.SelectedIndex].Groups[i].UID.ToString("X"));
                };

                if (ModelPackages.Count != 1)
                {
                    for (int i = 0; i < ModelPackages.Count; i++)
                        PackList.Items.Add(i + 1);

                    return;
                }

                PackList.Items.Add("1");
            }
        }
    }
}
