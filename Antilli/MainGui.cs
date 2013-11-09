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

        ModelPackagePC ModelPackage { get; set; }

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

            }
            catch (Exception e)
            {
                Console.WriteLine("Antilli::Exception -> {0}", e);
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

                ModelPackage = new ModelPackagePC(ChunkFile.Chunk.FirstOrNull(CTypes.MODEL_PACKAGE_PC));

                ModelPackage.Read(ChunkFile);
            }
        }

        /// <summary>Not implemented!</summary>
        public void SaveFile()
        {
            throw new NotImplementedException();
        }

        private void mn_View_Models_Click(object sender, EventArgs e)
        {
            if (ModelPackage != null)
            {
                Console.WriteLine("Adding items.");

                foreach (ModelPackage.ModelGroup group in ModelPackage.Groups)
                    MeshList.Items.Add(group.UID);
            }
        }
    }
}
