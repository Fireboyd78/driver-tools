using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public partial class Main : Form
    {
        private string[] _filters = {
            "Mission Script|*.mpc;*.mps;*.mxb",
            "Mission Scripts Package|*_missions.sp",
            "Any file|*.*"
        };

        private OpenFileDialog dlg_openFile;
        private MissionScriptFile mpcFile;

        OpenFileDialog ScriptFileDialog
        {
            get
            {
                if (dlg_openFile == null)
                {
                    dlg_openFile = new OpenFileDialog() {
                        AddExtension = true,
                        CheckFileExists = true,
                        CheckPathExists = true,

                        Filter = String.Join("|", _filters)
                    };
                }

                return dlg_openFile;
            }
        }
        
        public Main()
        {
            InitializeComponent();
            InitializeMenuClickEvents();
            InitializeItemStates();

            UpdateButtonStates();
        }

        private void LoadMissionScript(string filename)
        {
            if (File.Exists(filename))
            {
                // close the old script
                if (CloseMissionScript())
                {
                    mpcFile = new MissionScriptFile();
                    
                    mpcFile.SpoolerLoaded += (s, e) => {
                        s.Description = s.Description.Split(':')[0] + ": Modified by Zartex V2 @ " + DateTime.Now.ToString();
                    };

                    mpcFile.Load(filename);
                    UpdateButtonStates();
                }
            }
            else
            {
                MessageBox.Show("File not found!", "Zartex - ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool CloseMissionScript()
        {
            if (mpcFile != null)
            {
                var canClose = true;

                if (mpcFile.AreChangesPending)
                {
                    var result = MessageBox.Show("All unsaved changes will be lost. Do you wish to continue?", "Zartex", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    canClose = (result == DialogResult.Yes);
                }

                if (!canClose)
                    return false;

                mpcFile.Dispose();
                mpcFile = null;

                UpdateButtonStates();
            }
            return true;
        }

        private void SaveMissionScript()
        {
            if (mpcFile != null)
            {
                var result = MessageBox.Show("This will overwrite the original file! Do you wish to continue?", "Zartex", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    mpcFile.CommitChanges();
                    mpcFile.Save();

                    UpdateButtonStates();
                }
            }
        }

        private bool HasSummary()
        {
            return (mpcFile != null && mpcFile.MissionSummary != null);
        }

        private bool CanSave()
        {
            return (mpcFile != null && mpcFile.AreChangesPending && mpcFile.CanSave);
        }

        private bool ScriptLoaded()
        {
            return (mpcFile != null && mpcFile.IsLoaded);
        }
        
        /// <summary>
        /// Initializes click events for menu buttons.
        /// </summary>
        private void InitializeMenuClickEvents()
        {
            // 'Open...' button
            mmFile_Open.Click += (o, e) => {
                if (ScriptFileDialog.ShowDialog() == DialogResult.OK)
                    LoadMissionScript(dlg_openFile.FileName);
            };

            mmFile_Save.Click += (o, e) => {
                SaveMissionScript();
            };

            mmFile_Close.Click += (o, e) => {
                CloseMissionScript();
            };

            mmView_MiSummary.Click += (o, e) => {
                if (HasSummary())
                    MessageBox.Show(mpcFile.MissionSummary.GetSummaryAsString(), "Zartex", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
        }

        #region State handlers (enable/disable menu buttons)
        private Dictionary<ToolStripItem, MethodInfo> _stateHandlers;

        private void InitializeItemStates()
        {
            if (_stateHandlers == null)
            {
                _stateHandlers = new Dictionary<ToolStripItem, MethodInfo>();

                var methods = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
                
                foreach (ToolStripItem item in MenuBar.Items)
                    ParseItem(item, methods);
            }
        }

        private void UpdateButtonStates()
        {
            foreach (var state in _stateHandlers)
                state.Key.Enabled = (bool)state.Value.Invoke(this, null);
        }

        private void ParseItem(ToolStripItem item, MethodInfo[] methods)
        {            
            if (item is ToolStripMenuItem)
            {
                var menuItem = item as ToolStripMenuItem;

                if (menuItem.HasDropDownItems)
                {
                    foreach (ToolStripItem dropItem in menuItem.DropDownItems)
                        ParseItem(dropItem, methods);
                }
            }

            var tag = item.Tag as String;

            if (tag != null && tag.StartsWith("$"))
            {
                var lookup = tag.Substring(1);
                var method = methods.FirstOrDefault((m) => m.Name == lookup);

                if (method == null)
                    throw new Exception("Undefined state handler!");
                if (method.ReturnType != typeof(bool))
                    throw new Exception("State handler is not a boolean method!");

                _stateHandlers.Add(item, method);
                Debug.WriteLine("Adding state handler for '{0}' -> '{1}'", item.Name, method.Name);
            }
        }
        #endregion

    }
}
