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

using Antilli.IO;
using Antilli.Models;

using DialogResult = System.Windows.Forms.DialogResult;
using Visibility = System.Windows.Visibility;

namespace Antilli
{
    public partial class MainGui : Form
    {
        string _title = String.Empty;

        public MainGui()
        {
            InitializeComponent();

            _title = Text;

            Load += (o, e) => {
                string dir = DSC.Configuration.GetDirectory("DRIV3R");

                if (!Directory.Exists(dir))
                {
                    MessageBoxEx.Show(String.Format("\"{0}\" does not exist, please correct this in DSCript.ini and try again.", dir),
                        MessageBoxExFlags.ErrorBoxOK);

                    Application.Exit();
                }

                string models = Settings.Configuration.GetDirectory("Models");
                string textures = Settings.Configuration.GetDirectory("Textures");

                if (!Directory.Exists(models))
                    Directory.CreateDirectory(models);
                if (!Directory.Exists(textures))
                    Directory.CreateDirectory(textures);
            };

            AntilliMain();
        }

        public void ClearSubTitle()
        {
            Text = _title;
        }

        public void SetSubTitle(string subTitle)
        {
            Text = String.Format("{0} - {1}", _title, subTitle);
        }

        private void AntilliMain()
        {
            // Capture exceptions
            try
            {
                // Setup events
                mn_File_Open.Click += (o, e) => OpenFile();
                mn_File_Save.Click += (o, e) => SaveFile();
                mn_File_Exit.Click += (o, e) => Application.Exit();

                mn_Tools_ExportOBJ.Click += (o, e) => {
                    ModelPackage model = WPFGui.SelectedModelsPackage;
                    long uid = WPFGui.SelectedMeshUID;

                    if (model == null || uid == -1 || (uid != -1 && model.Meshes.Count < 1))
                    {
                        MessageBoxEx.Show("Nothing was selected!", MessageBoxExFlags.WarningBoxOK);
                        return;
                    }

                    ExportOBJ export = new ExportOBJ(WPFGui.SelectedModelsPackage, WPFGui.SelectedMeshUID);

                    System.Drawing.Point pt = new System.Drawing.Point(
                        this.Location.X + 25 + (this.Bounds.Width - (export.Bounds.Width)) / 2,
                        this.Location.Y + (this.Bounds.Height - (export.Bounds.Height)) / 2);

                    export.Location = pt;

                    export.ShowDialog();
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("Antilli::Exception -> {0}", e);
            }
        }

        #region Open/Save File methods
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

        /// <summary>Opens a dialog to select a chunk file</summary>
        public void OpenFile()
        {
            DialogResult result = openFile.ShowDialog();

            if (result == DialogResult.OK)
            {
                IModelFile modelFile = null;

                bool useSharedTextures = false;

                Cursor = Cursors.WaitCursor;

                switch (Path.GetExtension(openFile.FileName))
                {
                case ".vvs":
                    modelFile = new VVSFile(openFile.FileName);
                    goto vehicles;
                case ".vvv":
                    modelFile = new VVVFile(openFile.FileName);
                    goto vehicles;
                vehicles:
                    VGTFile sharedTextures = null;

                    string carGlobalsMiami      = @"{0}\Miami\CarGlobalsMiami.vgt";
                    string carGlobalsNice       = @"{0}\Nice\CarGlobalsNice.vgt";
                    string carGlobalsIstanbul   = @"{0}\Istanbul\CarGlobalsIstanbul.vgt";

                    // until configuration is implemented, do this to find vehicle globals
                    string[] dirs = openFile.FileName.Split('\\');
                    string dir = "";

                    StringBuilder root = new StringBuilder();

                    if (dirs[dirs.Length - 2] != "Vehicles")
                    {
                        root.AppendFormat(@"{0}\", dirs[0]);

                        int idx = dirs.Length - 1;
                        int rootIdx = 1;

                        while ((idx - 1 > 0) && dir != "Vehicles")
                        {
                            dir = dirs[idx--];
                            root.AppendFormat(@"{0}\", dirs[rootIdx++]);
                        }

                        if (dir != "Vehicles")
                            throw new Exception("Failed to find Vehicles directory!");

                        root.Append(dir);
                    }
                    else
                    {
                        for (int i = 0; i < dirs.Length - 1; i++)
                            root.AppendFormat(@"{0}\", dirs[i]);
                    }

                    if (openFile.FileName.Contains("miami"))
                    {
                        sharedTextures = new VGTFile(String.Format(carGlobalsMiami, root.ToString()));
                        ModelPackage.GlobalTexturesName = "CarGlobalsMiami";
                    }
                    else if (openFile.FileName.Contains("nice"))
                    {
                        sharedTextures = new VGTFile(String.Format(carGlobalsNice, root.ToString()));
                        ModelPackage.GlobalTexturesName = "CarGlobalsNice";
                    }
                    else if (openFile.FileName.Contains("istanbul"))
                    {
                        sharedTextures = new VGTFile(String.Format(carGlobalsIstanbul, root.ToString()));
                        ModelPackage.GlobalTexturesName = "CarGlobalsIstanbul";
                    }
                    else if (openFile.FileName.Contains("mission"))
                    {
                        string path = Path.GetFileNameWithoutExtension(openFile.FileName);

                        /* === mission%02d.vvv ===
                        miami: 	    01 - 10, 32-33, 38-40, 50-51, 56, 59-61, 71-72, 77-78
                        nice: 	    11 - 21, 34-35, 42-44, 52-53, 57, 62-64, 73-74, 80-81
                        istanbul: 	22 - 31, 36-37, 46-48, 54-55, 58, 65-67, 75-76, 83-84 */

                        //-- Be careful not to break the formatting!!
                        switch (int.Parse(path.Substring(path.Length - 2, 2)))
                        {
                        case 01: case 02: case 03: case 04: case 05: case 06:
                        case 07: case 08: case 09: case 10: case 32: case 33:
                        case 38: case 39: case 40: case 50: case 51: case 56:
                        case 59: case 60: case 61: case 71: case 72:
                            sharedTextures = new VGTFile(String.Format(carGlobalsMiami, root.ToString()));
                            ModelPackage.GlobalTexturesName = "CarGlobalsMiami";
                            break;
                        case 11: case 12: case 13: case 14: case 15: case 16:
                        case 17: case 18: case 19: case 20: case 21: case 34:
                        case 35: case 42: case 43: case 44: case 52: case 53:
                        case 57: case 62: case 63: case 64: case 73: case 74:
                            sharedTextures = new VGTFile(String.Format(carGlobalsNice, root.ToString()));
                            ModelPackage.GlobalTexturesName = "CarGlobalsNice";
                            break;
                        case 22: case 23: case 24: case 25: case 26: case 27:
                        case 28: case 29: case 30: case 31: case 36: case 37:
                        case 46: case 47: case 48: case 54: case 55: case 58:
                        case 65: case 66: case 67: case 75: case 76:
                            sharedTextures = new VGTFile(String.Format(carGlobalsIstanbul, root.ToString()));
                            ModelPackage.GlobalTexturesName = "CarGlobalsIstanbul";
                            break;
                        }
                    }

                    if (sharedTextures != null)
                    {
                        useSharedTextures = true;
                        ModelPackage.GlobalTextures = sharedTextures.StandaloneTextures;
                    }

                    break;
                default:
                    modelFile = new ModelFile(openFile.FileName);
                    break;
                }

                if (!useSharedTextures && ModelPackage.HasGlobalTextures)
                    ModelPackage.GlobalTextures = null;

                if (modelFile.Models != null)
                {
                    SetSubTitle(openFile.FileName);

                    WPFGui.LoadModelFile(modelFile);
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
        #endregion

        /// <summary>Not implemented!</summary>
        public void SaveFile()
        {
            throw new NotImplementedException();
        }
    }
}
