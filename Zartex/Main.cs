using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using DSCript;
using DSCript.Legacy;

using Zartex.MissionObjects;
using Zartex.LogicExport;

using Zartex.Settings;

// HACK: Fix discrepencies between "Form.DialogResult" and "System.Windows.Forms.DialogResult"
using DialogResult = System.Windows.Forms.DialogResult;

namespace Zartex
{
    public partial class Main : Form
    {
        string title;

        MPCFile MissionPackage;

        string Filename;

        public Main()
        {
            InitializeComponent();
            PopulateMainMenu();

            title = this.Text;

            Console.WriteLine("Driv3r Directory: {0}\nLocale: {1}",
                Configuration.Settings.InstallDirectory,
                Configuration.Settings.Locale);

            Console.WriteLine(MPCFile.MissionScriptDebug(77));

            foreach (Control control in Controls.Find("LeftMenu", true)[0].Controls)
            {
                if (control.Name.StartsWith("btn"))
                {
                    var b = BitConverter.ToInt32(Encoding.UTF8.GetBytes(control.Name.Substring(3, (control.Name.Length - 3))), 0);

                    control.Click += (o, e) => ChunkButtonClick((Button)o, b);

                    Console.WriteLine("Added an event handler to {0}", control.Name);
                }
            }
        }

        public void MenuLoadMission(object sender, EventArgs e)
        {
            int missionID = (int)((ToolStripMenuItem)sender).Tag;
            //LoadScriptFile(MPCFile.GetMissionScriptFilepath(missionID));
            LoadScriptFile(missionID);
        }

        public void PopulateMainMenu()
        {
            // Leaving these here for if they're ever needed..
            // const string miami          = "Miami";
            // const string nice           = "Nice";
            // const string istanbul       = "Istanbul";

            const string undercover     = "Undercover";
            const string takeARide      = "Take a Ride";
            const string takeARideSemi  = "Take a Ride (Semi)";
            const string quickChase     = "Quick Chase";
            const string quickGetaway   = "Quick Getaway";
            const string trailBlazer    = "Trail Blazer";
            const string survival       = "Survival";
            const string checkpointRace = "Checkpoint Race";
            const string gateRace       = "Gate Race";

            const string seperator      = "----";

            const string subGame        = "Sub-game";

            string[] menuNames = {
                undercover,
                seperator,
                takeARide,
                takeARideSemi,
                seperator,
                quickChase,
                quickGetaway,
                trailBlazer,
                survival,
                checkpointRace,
                gateRace
            };
            
            // Each array represents a city and its respective mission IDs
            // NOTE: To avoid jagged arrays, -1 is used as a terminator
            int[,] undercoverMissions = {
                { 01, 02, 03, 04, 05, 06, 07, 08, 09, 10 }, // Miami
                { 11, 13, 14, 15, 16, 17, 18, 19, 21, -1 }, // Nice
                { 22, 24, 25, 27, 28, 30, 31, -1, -1, -1 }  // Istanbul
            };

            int[,] takeARideIDs = {
                { 77, 78 },
                { 80, 81 },
                { 83, 84 }
            };

            // Array is structured like this:
            // --[City]
            // ----Quick Chase
            // ----Quick Getaway
            // ----Trail Blazer
            // ----Survival
            // ----Checkpoint Race
            // ----Gate Race

            // NOTE: To avoid jagged arrays, -1 is used as a terminator
            int[,,] drivingGames = {
                { // Miami
                    { 32, 33, -1 },
                    { 38, 39, 40 },
                    { 50, 51, -1 },
                    { 56, -1, -1 },
                    { 59, 60, 61 },
                    { 71, 72, -1 }
                },
                { // Nice
                    { 34, 35, -1 },
                    { 42, 43, 44 },
                    { 52, 53, -1 },
                    { 57, -1, -1 },
                    { 62, 63, 64 },
                    { 73, 74, -1 }
                },
                { // Istanbul
                    { 36, 37, -1 },
                    { 46, 47, 48 },
                    { 54, 55, -1 },
                    { 58, -1, -1 },
                    { 65, 66, 67 },
                    { 75, 76, -1 }
                },
            };

            ToolStripMenuItem[] menus = {
                mnMiami,
                mnNice,
                mnIstanbul
            };

            // Loop for each city
            for (int i = 0; i < menus.Length; i++)
            {
                ToolStripMenuItem menu = menus[i];

                for (int m = 0, n = 0; m < menuNames.Length; m++)
                {
                    if (menuNames[m] == seperator)
                    {
                        menu.DropDownItems.Add(new ToolStripSeparator());
                    }
                    else
                    {
                        ToolStripMenuItem subMenu = new ToolStripMenuItem(menuNames[m]);

                        switch (menuNames[m])
                        {
                        case undercover:
                            {
                                for (int v = 0, missionId; v < undercoverMissions.GetLength(1) && ((missionId = undercoverMissions[i, v]) != -1); v++)
                                {
                                    ToolStripMenuItem newMenu = new ToolStripMenuItem() {
                                        Text = MPCFile.ScriptFiles[missionId],
                                        Tag = missionId
                                    };

                                    newMenu.Click += (o, e) => MenuLoadMission(o, e);
                                    subMenu.DropDownItems.Add(newMenu);
                                }
                            }
                            break;
                        case takeARide:
                            subMenu.Tag = takeARideIDs[i, 0];
                            break;
                        case takeARideSemi:
                            subMenu.Tag = takeARideIDs[i, 1];
                            break;
                        default:
                            {
                                for (int k = 0, missionId; k < drivingGames.GetLength(2) && ((missionId = drivingGames[i, n, k]) != -1); k++)
                                {
                                    if (menuNames[m] == survival)
                                        subMenu.Tag = missionId;
                                    else
                                    {
                                        ToolStripMenuItem newMenu = new ToolStripMenuItem() {
                                            Text = String.Format("{0} {1}", subGame, k + 1),
                                            Tag = missionId,
                                        };

                                        newMenu.Click += (o, e) => MenuLoadMission(o, e);
                                        subMenu.DropDownItems.Add(newMenu);
                                    }
                                }
                                ++n;
                            }
                            break;
                        }

                        if (subMenu.Tag != null)
                            subMenu.Click += (o, e) => MenuLoadMission(o, e);

                        menu.DropDownItems.Add(subMenu);
                    }
                }
            }
        }

        private void GenerateExportedMissionObjects()
        {
            InspectorWidget Widget = new InspectorWidget();
            TreeView Nodes = Widget.Nodes;

            Cursor = Cursors.WaitCursor;

            TreeNode master = new TreeNode() {
                Text = "Exported Mission Objects"
            };

            Console.WriteLine("There's {0} mission objects.", MissionPackage.ExportedMissionObjects.Count);

            for (int i = 0; i < MissionPackage.ExportedMissionObjects.Count; i++)
                master.Nodes.Add(new TreeNode() {
                    Text = String.Format("{0}: Exported Mission Object", i),
                    Tag = MissionPackage.ExportedMissionObjects[i]
                });

            Nodes.Nodes.Add(master);

            SafeAddControl(Widget);

            Cursor = Cursors.Default;
        }

        private void GenerateWireCollection()
        {
            // Get widget ready
            InspectorWidget Widget = new InspectorWidget();
            TreeView Nodes = Widget.Nodes;

            Cursor = Cursors.WaitCursor;

            int nWires = MissionPackage.WireCollections.Count;

            IDictionary<int, string> opcodes = LogicData.Types.NodeDefinitionTypes;

            for (int w = 0; w < nWires; w++)
            {
                WireCollectionGroup wires = MissionPackage.WireCollections[w];

                LogicDefinition lNode = MissionPackage.LogicNodeDefinitions.First((def) => (int)def.Properties[0].Value == w);

                int nodeId = lNode.Opcode;

                TreeNode wireGroupNode = new TreeNode() {
                    Text = String.Format(
                        //"{0}: Wire Collection",
                        "{0}: {1}",
                        w,
                        opcodes.ContainsKey(nodeId) ? opcodes[nodeId] : "unknown"
                        //MissionPackage.StringCollection[MissionPackage.LogicNodeDefinitions[w].StringId]
                    ),
                    Tag = wires
                };

                for (int n = 0; n < wires.Count; n++)
                {
                    WireCollectionEntry wire = wires.Entries[n];

                    TreeNode wireNode = new TreeNode() {
                        Text = String.Format(
                            "{0}: {1}",
                            wire.NodeId,
                            (opcodes.ContainsKey(wire.Opcode) ? opcodes[wire.Opcode] : wire.Opcode.ToString())
                        ),
                        Tag = wire
                    };

                    wireGroupNode.Nodes.Add(wireNode);
                }

                Nodes.Nodes.Add(wireGroupNode);
            }

            Nodes.ExpandAll();

            SafeAddControl(Widget);
            
            Cursor = Cursors.Default;
        }

        private void CreateNodes(IList<LogicDefinition> definition)
        {
            // Get widget ready
            InspectorWidget LogicWidget = new InspectorWidget();
            TreeView Nodes = LogicWidget.Nodes;

            Cursor = Cursors.WaitCursor;

            int nodeCount = definition.Count;

            IDictionary<int, string> opcodes =
                (definition[0].Properties[0].Opcode == 19)
                ? LogicData.Types.NodeDefinitionTypes
                : LogicData.Types.ActorDefinitionTypes;

            // Build main nodes
            for (int i = 0; i < nodeCount; i++)
            {
                LogicDefinition def = definition[i];

                string strName = MissionPackage.StringCollection[def.StringId];
                string nodeName = (strName == "Unknown" || strName == "Unnamed") ? String.Empty : String.Format("\"{0}\"", strName);
                string opcodeName = opcodes.ContainsKey(def.Opcode) ? opcodes[def.Opcode] : def.Opcode.ToString();

                TreeNode node = new TreeNode() {
                    BackColor = Color.FromArgb(def.A, def.R, def.G, def.B),
                    Text = String.Format("{0}: {1} {2}", i, opcodeName, nodeName),
                    Tag = def
                };

                // Build property (sub) nodes
                for (int p = 0; p < def.Properties.Count; p++)
                {
                    LogicProperty prop = def.Properties[p];

                    if (prop.Opcode == 19 && MissionPackage.WireCollections[(int)prop.Value].Count == 0)
                        continue;

                    string propName = MissionPackage.StringCollection[prop.StringId];

                    if (prop.Opcode == 20 && MissionPackage.HasLocale)
                    {
                        int val = (int)prop.Value;
                        string localeStr = (!MissionPackage.LocaleStrings.ContainsKey(val)) ? "<NULL>" : String.Format("\"{0}\"", MissionPackage.LocaleStrings[val]);

                        propName = String.Format("{0} -> {1}", propName, localeStr);
                    }
                    if (prop.Opcode == 7 && ((int)prop.Value) > -1)
                    {
                        int val = MissionPackage.ActorDefinitions[(int)prop.Value].Opcode;

                        propName = String.Format("{0} -> {1}", propName, ((LogicData.Types.ActorDefinitionTypes.ContainsKey(val)) ? LogicData.Types.ActorDefinitionTypes[val] : prop.Value.ToString()));
                    }

                    TreeNode property = new TreeNode() {
                        Text = propName,
                        Tag = prop
                    };

                    // Add property node to main node
                    node.Nodes.Add(property);
                }

                // Add main node to master node list
                Nodes.Nodes.Add(node);
            }

            // Load wires (logic nodes only)
            for (int i = 0; i < nodeCount; i++)
            {
                LogicDefinition def = definition[i];

                // Skip actor definitions
                if (def is ActorDefinition)
                    break;

                LogicProperty prop = def.Properties[0];

                var nodeWireCollection = Nodes.Nodes[i];

                if (nodeWireCollection.Nodes.Count > 0)
                    nodeWireCollection = nodeWireCollection.Nodes[0];
                else
                    continue;

                int wireId = (int)prop.Value;

                var wires = MissionPackage.WireCollections[wireId];
                int nWires = wires.Count;

                for (int w = 0; w < nWires; w++)
                {
                    var wire = wires.Entries[w];
                    var defNode = Nodes.Nodes[wire.NodeId];

                    // int wireTypeId = definition[wire.NodeId].StringId;

                    // string strName = MissionPackage.StringCollection[wireTypeId];
                    // string nodeName = (strName == "Unknown" || strName == "Unnamed") ? String.Empty : String.Format("\"{0}\"", strName);
                    // string opcodeName = opcodes.ContainsKey(wire.Opcode) ? opcodes[wire.Opcode] : wire.Opcode.ToString();

                    TreeNode wireNode = new TreeNode() {
                        BackColor = defNode.BackColor,
                        Text = defNode.Text,
                        Tag = wire
                    };

                    nodeWireCollection.Nodes.Add(wireNode);
                    //LogicNodes.Nodes[i].Nodes[0].Collapse(false);
                }
            }

            Nodes.ExpandAll();

            SafeAddControl(LogicWidget);
            
            Cursor = Cursors.Default;
        }


        private void GenerateLogicNodes()
        {
            CreateNodes(MissionPackage.LogicNodeDefinitions);
            //CreateLogicNodesFlowgraph(MissionPackage.LogicNodeDefinitions);

            // // Nest wires
            // for (int i = 0; i < nodeCount; i++)
            // {
            //     LogicDefinition def = MissionPackage.LogicNodeDefinitions[i];
            //     LogicProperty prop = def.Properties[0];
            // 
            //     int wireId = (int)prop.Value;
            // 
            //     for (int w = 0; w < MissionPackage.WireCollections[wireId].Count; w++)
            //     {
            //         WireCollectionEntry wire = MissionPackage.WireCollections[wireId].Entries[w];
            // 
            //         LogicNodes.Nodes[i].Nodes[0].Nodes[w] = LogicNodes.Nodes[wire.NodeId];
            //     }
            // }
        }

        private void GenerateActors()
        {
            CreateNodes(MissionPackage.ActorDefinitions);
        }

        public void GenerateDefinition(FlowgraphWidget flowgraph, LogicDefinition def, int x, int y)
        {
            IDictionary<int, string> opcodes =
                (def.Properties[0].Opcode == 19)
                ? LogicData.Types.NodeDefinitionTypes
                : LogicData.Types.ActorDefinitionTypes;

            string strName = MissionPackage.StringCollection[def.StringId];
            string nodeName = (strName == "Unknown" || strName == "Unnamed") ? String.Empty : String.Format("\"{0}\"", strName);
            string opcodeName = opcodes.ContainsKey(def.Opcode) ? opcodes[def.Opcode] : def.Opcode.ToString();

            NodeWidget node = new NodeWidget() {
                Flowgraph = flowgraph,
                //BackColor = Color.FromArgb(def.Byte4, def.Byte1, def.Byte2, def.Byte3),
                HeaderText = String.Format("{0}: {1} {2}", MissionPackage.LogicNodeDefinitions.IndexOf(def), opcodeName, nodeName),
                Left = x,
                Top = y,
                Tag = def
            };

            if (def.Properties.Count > 4)
                node.Width += 100;

            flowgraph.AddNode(node);

            // y += node.Height + 80;

            for (int p = 0; p < def.Properties.Count; p++)
            {
                LogicProperty prop = def.Properties[p];

                string propName = MissionPackage.StringCollection[prop.StringId];

                // if (prop.Opcode == 20 && MissionPackage.HasLocale)
                // {
                //     int val = (int)prop.Value;
                //     string localeStr = (!MissionPackage.LocaleStrings.ContainsKey(val)) ? "<NULL>" : String.Format("\"{0}\"", MissionPackage.LocaleStrings[val]);
                // 
                //     propName = String.Format("{0} -> {1}", propName, localeStr);
                // }
                // if (prop.Opcode == 7 && ((int)prop.Value) != -1)
                // {
                //     int val = MissionPackage.ActorDefinitions[(int)prop.Value].Opcode;
                // 
                //     propName = String.Format("{0} -> {1}", propName, ((LogicData.Types.ActorDefinitionTypes.ContainsKey(val)) ? LogicData.Types.ActorDefinitionTypes[val] : prop.Value.ToString()));
                // }

                Label property = new Label() {
                    Text = String.Format("{0} = {1}", propName, prop.Value),
                    Font = new Font(Font.SystemFontName, 9F, FontStyle.Regular, GraphicsUnit.Pixel),
                    //Width = node.Properties.Width / 2 - 12,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Tag = prop
                };

                node.Properties.Controls.Add(property);
            }

            int oldY = y;
            int oldX = x;

            int wireId = (int)def.Properties[0].Value;

            // for (int w = 0; w < MissionPackage.WireCollections[wireId].Count; w++)
            // {
            //     GenerateDefinition(flowgraph, MissionPackage.LogicNodeDefinitions[MissionPackage.WireCollections[wireId].Entries[w].NodeId], x, y);
            //     y += node.Height + 50;
            // }

            y = oldY;
            x = oldX;
        }

        public void CreateLogicNodesFlowgraph(IList<LogicDefinition> definition)
        {
            InspectorWidget Widget = new InspectorWidget();

            SplitterPanel Panel1 = Widget.SplitPanel.Panel1;
            Panel1.Controls.Clear();
            Panel1.AutoScroll = true;

            FlowgraphWidget Flowgraph = new FlowgraphWidget() {
                Parent = Panel1
            };

            // Never forget.
            Flowgraph.GotFocus += (o, e) => { Flowgraph.Parent.Focus(); };
            Panel1.LostFocus += (o, e) => { Panel1.Focus(); };

            //Flowgraph.Dock = DockStyle.Fill;

            int nodeCount = definition.Count;

            IDictionary<int, string> opcodes =
                (definition[0].Properties[0].Opcode == 19)
                ? LogicData.Types.NodeDefinitionTypes
                : LogicData.Types.ActorDefinitionTypes;

            int x = 3, y = 6;

            //GenerateDefinition(Flowgraph, definition[0], x, y);

            for (int i = 0; i < definition.Count; i++)
                GenerateDefinition(Flowgraph, definition[i], x, y);

            x = 3;
            y = 6;

            for (int i = 0; i < definition.Count; i++)
            {
                int wireId = (int)definition[i].Properties[0].Value;

                int oldY = y;

                for (int w = 0; w < MissionPackage.WireCollections[wireId].Count; w++)
                {
                    Flowgraph.Nodes[i].Left = x;
                    Flowgraph.Nodes[i].Top = y;

                    Flowgraph.Nodes[MissionPackage.WireCollections[wireId].Entries[w].NodeId].Left = x + Flowgraph.Nodes[i].Left + 235;
                    Flowgraph.Nodes[MissionPackage.WireCollections[wireId].Entries[w].NodeId].Top = y + Flowgraph.Nodes[i].Top;

                    Flowgraph.LinkNodes(Flowgraph.Nodes[i], Flowgraph.Nodes[MissionPackage.WireCollections[wireId].Entries[w].NodeId]);

                    y += 75;
                }
                x += Flowgraph.Nodes[i].Width + 25;
                y = oldY + 75;
            }

            // // Build main nodes
            // for (int i = 0; i < nodeCount; i++)
            // {
            //     LogicDefinition def = definition[i];
            // 
            //     string strName = MissionPackage.StringCollection[def.StringId];
            //     string nodeName = (strName == "Unknown" || strName == "Unnamed") ? String.Empty : String.Format("\"{0}\"", strName);
            //     string opcodeName = opcodes.ContainsKey(def.Opcode) ? opcodes[def.Opcode] : def.Opcode.ToString();
            // 
            //     NodeWidget node = new NodeWidget() {
            //         Flowgraph = Flowgraph,
            //         //BackColor = Color.FromArgb(def.Byte4, def.Byte1, def.Byte2, def.Byte3),
            //         HeaderText = String.Format("{0}: {1} {2}", i, opcodeName, nodeName),
            //         Left = x,
            //         Top = y,
            //         Tag = def
            //     };
            // 
            //     x += 350;
            // 
            //     // Build property (sub) nodes
            //     for (int p = 0; p < def.Properties.Count; p++)
            //     {
            //         LogicProperty prop = def.Properties[p];
            // 
            //         string propName = MissionPackage.StringCollection[prop.StringId];
            // 
            //         // if (prop.Opcode == 20 && MissionPackage.HasLocale)
            //         // {
            //         //     int val = (int)prop.Value;
            //         //     string localeStr = (!MissionPackage.LocaleStrings.ContainsKey(val)) ? "<NULL>" : String.Format("\"{0}\"", MissionPackage.LocaleStrings[val]);
            //         // 
            //         //     propName = String.Format("{0} -> {1}", propName, localeStr);
            //         // }
            //         // if (prop.Opcode == 7 && ((int)prop.Value) != -1)
            //         // {
            //         //     int val = MissionPackage.ActorDefinitions[(int)prop.Value].Opcode;
            //         // 
            //         //     propName = String.Format("{0} -> {1}", propName, ((LogicData.Types.ActorDefinitionTypes.ContainsKey(val)) ? LogicData.Types.ActorDefinitionTypes[val] : prop.Value.ToString()));
            //         // }
            // 
            //         Label property = new Label() {
            //             Text = String.Format("{0} = {1}", propName, prop.Value),
            //             Font = new Font(Font.SystemFontName, 9F, FontStyle.Regular, GraphicsUnit.Pixel),
            //             Width = node.Properties.Width / 2 - 12,
            //             TextAlign = ContentAlignment.MiddleLeft,
            //             Tag = prop
            //         };
            // 
            //         node.Properties.Controls.Add(property);
            //     }
            // 
            //     Flowgraph.AddNode(node);
            // }

            

            //// Load wires (logic nodes only)
            //for (int i = 0; i < nodeCount; i++)
            //{
            //    LogicDefinition def = definition[i];
            //    LogicProperty prop = def.Properties[0];
            //
            //    // it's actor defs, don't try to load
            //    if (prop.Opcode != 19) break;
            //
            //    int wireId = (int)prop.Value;
            //
            //    for (int w = 0; w < MissionPackage.WireCollections[wireId].Count; w++)
            //    {
            //        WireCollectionEntry wire = MissionPackage.WireCollections[wireId].Entries[w];
            //
            //        // int wireTypeId = definition[wire.NodeId].StringId;
            //
            //        // string strName = MissionPackage.StringCollection[wireTypeId];
            //        // string nodeName = (strName == "Unknown" || strName == "Unnamed") ? String.Empty : String.Format("\"{0}\"", strName);
            //        // string opcodeName = opcodes.ContainsKey(wire.Opcode) ? opcodes[wire.Opcode] : wire.Opcode.ToString();
            //
            //        TreeNode wireNode = new TreeNode() {
            //            Text = Nodes.Nodes[wire.NodeId].Text,
            //            Tag = wire
            //        };
            //
            //        Nodes.Nodes[i].Nodes[0].Nodes.Add(wireNode);
            //        //LogicNodes.Nodes[i].Nodes[0].Collapse(false);
            //    }
            //}

            Panel1.Controls.Add(Flowgraph);

            Panel1.Focus();

            SafeAddControl(Widget);
        }

        private void GenerateStringCollection()
        {
            DataGridWidget DataGridWidget = new DataGridWidget();
            DataGridView DataGrid = DataGridWidget.DataGridView;

            Cursor = Cursors.WaitCursor;

            for (int i = 0; i < MissionPackage.StringCollection.Count; i++)
                DataGrid.Rows.Add(i, MissionPackage.StringCollection[i]);

            SafeAddControl(DataGridWidget);

            Cursor = Cursors.Default;
        }

        private void GenerateActorSetTable()
        {
            DataGridWidget DataGridWidget = new DataGridWidget();
            DataGridView DataGrid = DataGridWidget.DataGridView;

            Cursor = Cursors.WaitCursor;

            for (int i = 0; i < MissionPackage.ActorSetTable.Count; i++)
                DataGrid.Rows.Add(i, MissionPackage.ActorSetTable[i]);

            SafeAddControl(DataGridWidget);

            Cursor = Cursors.Default;
        }

        private void LoadScriptFile(int missionId)
        {
            MissionPackage = new MPCFile(missionId);
            Filename = MissionPackage.Filename;

            InitTools();
        }

        private void LoadScriptFile(string filename)
        {
            Filename = filename;
            MissionPackage = new MPCFile(Filename);

            InitTools();
        }

        private void LoadScriptFile(int missionId, string localeFile)
        {
            MissionPackage = new MPCFile(missionId, localeFile);
            Filename = MissionPackage.Filename;

            InitTools();
        }

        private void LoadScriptFile(string filename, string localeFile)
        {
            Filename = filename;
            MissionPackage = new MPCFile(Filename, localeFile);

            InitTools();
        }

        private void InitTools()
        {
            if (MissionPackage.IsLoaded)
            {
                Text = String.Format("{0} - {1}", title, Filename);
                GenerateLogicNodes();

                mnTools.Enabled = true;
            }
        }

        /// <summary> Safely adds a control to the form.</summary>
        private void SafeAddControl(Control control)
        {
            control.Parent = this;
            control.Dock = DockStyle.Fill;

            Content.SuspendLayout();

            foreach (Control c in Content.Controls)
                c.Dispose();

            Content.Controls.Add(control);
            Content.ResumeLayout(true);
        }

        private void GetChunkData(SubChunkBlock subChunk)
        {
            
        }

        private void ChunkButtonClick(Button button, int magic)
        {
            if (MissionPackage != null && MissionPackage.IsLoaded)
            {
                var cType = (ChunkType)magic;

                switch (cType)
                {
                case ChunkType.ExportedMissionObjects:
                    GenerateExportedMissionObjects();
                    break;
                case ChunkType.LogicExportStringCollection:
                    GenerateStringCollection();
                    break;
                case ChunkType.LogicExportActorSetTable:
                    GenerateActorSetTable();
                    break;
                case ChunkType.LogicExportActorsChunk:
                    GenerateActors();
                    break;
                case ChunkType.LogicExportNodesChunk:
                    GenerateLogicNodes();
                    break;
                case ChunkType.LogicExportWireCollections:
                    GenerateWireCollection();
                    break;
                default:
                    MessageBox.Show("Not implemented!");
                    break;
                }

                Console.WriteLine("Couldn't find anything...");
            }
            else
                MessageBox.Show("No mission loaded!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private OpenFileDialog GetMissionScriptFile()
        {
            return new OpenFileDialog() {
                Title = "Select a mission script",
                Filter = "Mission Script (*.mpc)|*.mpc",
                InitialDirectory = MPCFile.GetMissionScriptDirectory()
            };
        }

        private OpenFileDialog GetMissionLocaleFile()
        {
            return new OpenFileDialog() {
                Title = "Select a mission locale file (optional)",
                Filter = "Mission Locale (*.txt)|*.txt",
                InitialDirectory = MPCFile.GetMissionLocaleDirectory()
            };
        }

        private void MenuLoadFile(object sender, EventArgs e)
        {
            OpenFileDialog ScriptFile = GetMissionScriptFile();

            if (ScriptFile.ShowDialog() == DialogResult.OK)
            {
                OpenFileDialog LocaleFile = GetMissionLocaleFile();

                if (LocaleFile.ShowDialog() == DialogResult.OK)
                    LoadScriptFile(ScriptFile.FileName, LocaleFile.FileName);
                else
                    LoadScriptFile(ScriptFile.FileName);
            }
        }

        private void LoadLocaleTool(object sender, EventArgs e)
        {
            OpenFileDialog LocaleOpen = new OpenFileDialog() {
                InitialDirectory = MPCFile.GetMissionLocaleDirectory(),
                Filter = "Mission Locale (*.txt)|*.txt"
            };

            if (LocaleOpen.ShowDialog() == DialogResult.OK)
            {
                MissionPackage.LoadLocaleFile(LocaleOpen.FileName);
                GenerateLogicNodes();
            }
        }

        private void onPaintFlowgraph(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);

            Panel Panel1 = (Panel)sender;
            PictureBox nodeOut = ((NodeWidget)Panel1.Controls[0]).nodeOut;
            PictureBox nodeIn = ((NodeWidget)Panel1.Controls[2]).nodeIn;

            Pen pen = new Pen(Color.Black, 2F);

            Point x = Panel1.PointToClient(nodeOut.Parent.PointToScreen(nodeOut.Location));
            Point y = Panel1.PointToClient(nodeIn.Parent.PointToScreen(nodeIn.Location));

            x.X += 17;
            x.Y += 7;

            y.Y += 7;



            //Console.WriteLine("Drawing a line from {0} to {1}", x, y);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawLine(pen, x, y);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            InspectorWidget Widget = new InspectorWidget();

            SplitterPanel Panel1 = Widget.SplitPanel.Panel1;
            Panel1.AutoScroll = true;
            Panel1.BackColor = Color.White;
            
            Panel1.Controls.Clear();

            //Panel1.Paint += (o, p) => onPaintFlowgraph(o, p);
            //Panel1.Click += (o, pp) => {
            //    MouseEventArgs ee = (MouseEventArgs)pp;
            //
            //    using (Pen pen = new Pen(Color.Black, 2F))
            //    using (Graphics g = Panel1.CreateGraphics())
            //    {
            //        g.DrawEllipse(pen, ee.Location.X, ee.Location.Y, 10, 10);
            //    }
            //
            //    Console.WriteLine(((MouseEventArgs)pp).Location);
            //};
            //
            //Panel1.Controls.Add(new NodeWidget() {
            //    Parent = Panel1,
            //    Left = 3,
            //    Top = 6,
            //    Visible = true,
            //});
            //
            //Panel1.Controls.Add(new NodeWidget() {
            //    Parent = Panel1,
            //    Left = 3,
            //    Top = 203,
            //    Visible = true,
            //});
            //
            //Panel1.Controls.Add(new NodeWidget() {
            //    Parent = Panel1,
            //    Left = 327,
            //    Top = 91,
            //    Visible = true,
            //});

            FlowgraphWidget flowgraph = new FlowgraphWidget();

            NodeWidget n1 = new NodeWidget() {
                Flowgraph = flowgraph,
                Name = "Node1",
                HeaderText = "Logic Node #1",
                Left = 3,
                Top = 6,
            };
            
            NodeWidget n2 = new NodeWidget() {
                Flowgraph = flowgraph,
                Name = "Node2",
                HeaderText = "Logic Node #2",
                Left = 327,
                Top = 91,
            };
            
            NodeWidget n3 = new NodeWidget() {
                Flowgraph = flowgraph,
                Name = "Node3",
                HeaderText = "Logic Node #3",
                Left = 3,
                Top = 203,
            };
            
            NodeWidget n4 = new NodeWidget() {
                Flowgraph = flowgraph,
                Name = "Node4",
                HeaderText = "Logic Node #4",
                Left = 327,
                Top = 273,
            };

            n1.Properties.Controls.Add(new Label() {
                Parent = n1.Properties,
                Text = "pProperty = 5"
            });
            n1.Properties.Controls.Add(new Label() {
                Parent = n1.Properties,
                Text = "pProperty = 1"
            });
            n1.Properties.Controls.Add(new Label() {
                Parent = n1.Properties,
                Text = "pProperty = 3"
            });
            n1.Properties.Controls.Add(new Label() {
                Parent = n1.Properties,
                Text = "pProperty = 69"
            });
            n1.Properties.Controls.Add(new Label() {
                Parent = n1.Properties,
                Text = "pProperty = 1100548"
            });
            n1.Properties.Controls.Add(new Label() {
                Parent = n1.Properties,
                Text = "pProperty = 495544841"
            });
            n1.Properties.Controls.Add(new Label() {
                Parent = n1.Properties,
                Text = "pProperty = 0x185784"
            });
            n1.Properties.Controls.Add(new Label() {
                Parent = n1.Properties,
                Text = "pProperty = FFFFFFFF"
            });

            n1.NodeClicked += (o, m) => Widget.AfterNodeWidgetSelected(o, m);
            n2.NodeClicked += (o, m) => Widget.AfterNodeWidgetSelected(o, m);
            n3.NodeClicked += (o, m) => Widget.AfterNodeWidgetSelected(o, m);
            n4.NodeClicked += (o, m) => Widget.AfterNodeWidgetSelected(o, m);

            //flowgraph.Dock = DockStyle.Fill;

            //flowgraph.AddNodes(n1, n2, n3, n4);

            flowgraph.LinkNodes(n1, n2);
            flowgraph.LinkNodes(n3, n2);
            flowgraph.LinkNodes(n3, n4);

            Panel1.Controls.Add(flowgraph);

            SafeAddControl(Widget);
        }
    }
}
