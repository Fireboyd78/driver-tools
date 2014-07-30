using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using DSCript;

using Antilli.IO;
using Antilli.Models;

namespace Antilli
{
    public partial class ExportOBJ : Form
    {
        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HT_CAPTION = 0x2;

        void MoveForm(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        public bool ExportMaterials
        {
            get { return ExportGUI.ExportMaterials.IsChecked ?? false; }
        }

        public string NamePrefix
        {
            get { return ExportGUI.NamePrefix.Text; }
        }

        public ExportOBJ(ModelPackage modelPackage, long modelUid)
        {
            InitializeComponent();

            ExportGUI.btnClose.Click += (o, e) => Close();
            ExportGUI.MouseDown += MoveForm;

            ExportGUI.btnExport.Click += (o, e) => {
                StringBuilder directory = new StringBuilder();

                directory.Append(Settings.Configuration.GetDirectory("Models"));
                directory.AppendFormat(@"\{0}", NamePrefix);

                string dir = directory.ToString();

                ExportResult result = OBJFile.Export(dir, modelUid.ToString(), modelPackage, modelUid, ExportMaterials);

                if (result == ExportResult.Success)
                {
                    StringBuilder output = new StringBuilder();

                    output.AppendLine("The following files were exported successfully:").AppendLine();
                    output.AppendFormat("- {0}\\{1}.obj", dir, modelUid);

                    if (ExportMaterials)
                    {
                        output.AppendLine();
                        output.AppendFormat("- {0}\\{1}.mtl", dir, modelUid);
                    }

                    MessageBoxEx.Show(output.ToString(), MessageBoxExFlags.InfoBoxOK);
                }
                else
                    Close();
            };
        }
    }
}
