using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using DSCript;

using Zartex.Converters;
using Zartex.Settings;

// HACK: Fix discrepencies between "Form.DialogResult" and "System.Windows.Forms.DialogResult"
using DialogResult = System.Windows.Forms.DialogResult;

namespace Zartex
{
    public partial class Main : Form
    {
        static Main()
        {
            DSC.VerifyGameDirectory("Driv3r", "Zartex");

            TypeDescriptor.AddAttributes(typeof(Vector2), new TypeConverterAttribute(typeof(VectorTypeConverter)));
            TypeDescriptor.AddAttributes(typeof(Vector3), new TypeConverterAttribute(typeof(VectorTypeConverter)));
            TypeDescriptor.AddAttributes(typeof(Vector4), new TypeConverterAttribute(typeof(VectorTypeConverter)));
        }

        string title;

        MissionScriptFile MissionPackage;

        OpenFileDialog ScriptFile = new OpenFileDialog() {
            Title = "Select a mission script",
            Filter = "Mission Script|*.mpc;*.mps;*.mxb",
            InitialDirectory = MPCFile.GetMissionScriptDirectory(),
        };
        
        string Filename;

        public Main()
        {
            InitializeComponent();
            PopulateMainMenu();

            title = this.Text;

            Console.WriteLine("Driv3r Directory: {0}\nLocale: {1}",
                Driv3r.RootDirectory,
                Configuration.Settings.Locale);
            
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

        private void InitTools()
        {
            if (MissionPackage.IsLoaded)
            {
                Text = String.Format("{0} - {1}", title, Filename);
                GenerateLogicNodes();
            }

            mnFile_Save.Enabled = MissionPackage.IsLoaded;
        }

        private void LoadScriptFile(int missionId)
        {
            LoadScriptFile(Driv3r.GetMissionScript(missionId));
        }

        private void LoadScriptFile(string filename)
        {
            Filename = filename;
            MissionPackage = new MissionScriptFile(Filename);

            InitTools();
        }
        
        public void MenuLoadMission(object sender, EventArgs e)
        {
            int missionID = (int)((ToolStripMenuItem)sender).Tag;
            
            LoadScriptFile(missionID);
        }

        private void MenuLoadFile(object sender, EventArgs e)
        {
            var result = ScriptFile.ShowDialog();

            if (result == DialogResult.OK)
            {
                ScriptFile.InitialDirectory = Path.GetDirectoryName(ScriptFile.FileName);

                LoadScriptFile(ScriptFile.FileName);
            }
        }

        private void MenuSaveFile(object sender, EventArgs e)
        {
            var bakFile = MissionPackage.FileName + ".bak";
            
            if (File.Exists(bakFile))
            {
                var idx = 1;

                while (File.Exists(bakFile + idx))
                    idx++;

                bakFile += idx;
            }
            
            File.Copy(MissionPackage.FileName, bakFile);

            if (MissionPackage.Save())
            {
                var result = MessageBox.Show(String.Format("Successfully saved to \"{0}\"!", MissionPackage.FileName),
                    "Zartex", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (result == DialogResult.OK)
                {
                    var filename = MissionPackage.FileName;

                    // close the old file
                    MissionPackage.Dispose();
                    MissionPackage = null;

                    // reopen it
                    LoadScriptFile(filename);
                }
            }
            else
            {
                MessageBox.Show("Failed to save file!",
                    "Zartex", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public enum CityType
        {
            Miami,
            Nice,
            Istanbul,
        }

        public enum GameModeType
        {
            Undercover,

            TakeARide,

            QuickChase,
            QuickGetaway,
            TrailBlazer,
            Survival,
            CheckpointRace,
            GateRace,
        }

        public struct MissionDescriptor
        {
            public string Name { get; }

            public GameModeType GameMode { get; }
            public CityType City { get; }

            public int[] MissionIds { get; }

            public bool HasSubMissions
            {
                get { return MissionIds.Length > 1; }
            }
            
            // driving games/take a ride
            public MissionDescriptor(GameModeType gameMode, CityType city, params int[] missionIds)
                : this($"{gameMode.ToString()}, {city.ToString()}", gameMode, city, missionIds) { }

            // undercover
            public MissionDescriptor(string name, CityType city, params int[] missionIds)
                : this(name, GameModeType.Undercover, city, missionIds) { }

            public MissionDescriptor(string name, GameModeType gameMode, CityType city, params int[] missionIds)
            {
                Name = name;
                City = city;
                GameMode = gameMode;
                MissionIds = missionIds;
            }
        }

        public static readonly MissionDescriptor[] MissionDescriptors = new[] {
            /*
                Undercover (Miami)
            */
            new MissionDescriptor("Police HQ",                  CityType.Miami,         1,  101, 102),
            new MissionDescriptor("Lead on Baccus",             CityType.Miami,         2,  103),
            new MissionDescriptor("The Siege",                  CityType.Miami,         3,  105),
            new MissionDescriptor("Rooftops",                   CityType.Miami,         4,  106, 107),
            new MissionDescriptor("Impress Lomaz",              CityType.Miami,         5,  108, 109, 121),
            new MissionDescriptor("Gator's Yacht",              CityType.Miami,         6,  110, 111),
            new MissionDescriptor("The Hit",                    CityType.Miami,         7,  112, 122),
            new MissionDescriptor("Trapped",                    CityType.Miami,         8,  113, 114, 115),
            new MissionDescriptor("Dodge Island",               CityType.Miami,         9,  116, 117),
            new MissionDescriptor("Retribution",                CityType.Miami,         10, 118, 119, 120),
            /*
                Undercover (Nice)
            */
            new MissionDescriptor("Welcome to Nice",            CityType.Nice,          11, 130, 131),
            new MissionDescriptor("Smash and Run",              CityType.Nice,          13, 134),
            new MissionDescriptor("18-wheeler",                 CityType.Nice,          14, 135, 150),
            new MissionDescriptor("Hijack",                     CityType.Nice,          15, 136),
            new MissionDescriptor("Arms Deal",                  CityType.Nice,          16, 137, 138, 139, 149),
            new MissionDescriptor("Booby Trap",                 CityType.Nice,          17, 140, 151, 152),
            new MissionDescriptor("Calita in Trouble",          CityType.Nice,          18, 141, 142),
            new MissionDescriptor("Rescue Dubois",              CityType.Nice,          19, 143, 144),
            new MissionDescriptor("Hunted",                     CityType.Nice,          21, 146, 147, 148),
            /*
                Undercover (Istanbul)
            */
            new MissionDescriptor("Surveillance",               CityType.Istanbul,      22, 160, 161, 162),
            new MissionDescriptor("Tanner Escapes",             CityType.Istanbul,      24, 164, 165, 180),
            new MissionDescriptor("Another Lead",               CityType.Istanbul,      25, /*166, 167,*/ 168, 181),
            new MissionDescriptor("Alleyway",                   CityType.Istanbul,      27, 171, 172),
            new MissionDescriptor("The Chase",                  CityType.Istanbul,      28, 173, 174),
            new MissionDescriptor("Bomb Truck",                 CityType.Istanbul,      30, 176),
            new MissionDescriptor("Chase the Train",            CityType.Istanbul,      31, 177, 178, 179),
            /*
                Driving games
            */
            new MissionDescriptor(GameModeType.QuickChase,      CityType.Miami,         32, 33),
            new MissionDescriptor(GameModeType.QuickChase,      CityType.Nice,          34, 35),
            new MissionDescriptor(GameModeType.QuickChase,      CityType.Istanbul,      36, 37),
            new MissionDescriptor(GameModeType.QuickGetaway,    CityType.Miami,         38, 39, 40),
            new MissionDescriptor(GameModeType.QuickGetaway,    CityType.Nice,          42, 43, 44),
            new MissionDescriptor(GameModeType.QuickGetaway,    CityType.Istanbul,      46, 47, 48),
            new MissionDescriptor(GameModeType.TrailBlazer,     CityType.Miami,         50, 51),
            new MissionDescriptor(GameModeType.TrailBlazer,     CityType.Nice,          52, 53),
            new MissionDescriptor(GameModeType.TrailBlazer,     CityType.Istanbul,      54, 55),
            new MissionDescriptor(GameModeType.Survival,        CityType.Miami,         56),
            new MissionDescriptor(GameModeType.Survival,        CityType.Nice,          57),
            new MissionDescriptor(GameModeType.Survival,        CityType.Istanbul,      58),
            new MissionDescriptor(GameModeType.CheckpointRace,  CityType.Miami,         59, 60, 61),
            new MissionDescriptor(GameModeType.CheckpointRace,  CityType.Nice,          62, 63, 64),
            new MissionDescriptor(GameModeType.CheckpointRace,  CityType.Istanbul,      65, 66, 67),
            new MissionDescriptor(GameModeType.GateRace,        CityType.Miami,         71, 72),
            new MissionDescriptor(GameModeType.GateRace,        CityType.Nice,          73, 74),
            new MissionDescriptor(GameModeType.GateRace,        CityType.Istanbul,      75, 76),
            /*
                Take a Ride
            */
            new MissionDescriptor(GameModeType.TakeARide,       CityType.Miami,         77, 78),
            new MissionDescriptor(GameModeType.TakeARide,       CityType.Nice,          80, 81),
            new MissionDescriptor(GameModeType.TakeARide,       CityType.Istanbul,      83, 84),
        };

        private ToolStripMenuItem GetMenuItemByCity(CityType city)
        {
            switch (city)
            {
            case CityType.Miami:    return mnMiami;
            case CityType.Nice:     return mnNice;
            case CityType.Istanbul: return mnIstanbul;
            }

            return null;
        }

        private string GetItemNameForMission(MissionDescriptor mission)
        {
            switch (mission.GameMode)
            {
            case GameModeType.TakeARide:        return "Take A Ride";
            case GameModeType.QuickChase:       return "Quick Chase";
            case GameModeType.QuickGetaway:     return "Quick Getaway";
            case GameModeType.TrailBlazer:      return "Trail Blazer";
            case GameModeType.Survival:         return "Survival";
            case GameModeType.CheckpointRace:   return "Checkpoint Race";
            case GameModeType.GateRace:         return "Gate Race";
            }

            return mission.Name;
        }

        private ToolStripMenuItem BuildMenuItem(MissionDescriptor mission)
        {
            var itemText = GetItemNameForMission(mission);
            var menuItem = new ToolStripMenuItem(itemText) {
                Tag = mission.MissionIds[0]
            };

            menuItem.Click += MenuLoadMission;

            return menuItem;
        }

        private ToolStripMenuItem BuildMenuItem(MissionDescriptor mission, List<ToolStripMenuItem> subItems)
        {
            var itemText = GetItemNameForMission(mission);
            var menuItem = new ToolStripMenuItem(itemText);

            foreach (var subItem in subItems)
            {
                if (subItem.Tag != null)
                    subItem.Click += MenuLoadMission;

                menuItem.DropDownItems.Add(subItem);
            }

            return menuItem;
        }

        public void PopulateMainMenu()
        {
            CityType[] cityTypes = {
                CityType.Miami,
                CityType.Nice,
                CityType.Istanbul,
            };

            foreach (var city in cityTypes)
            {
                var menu = GetMenuItemByCity(city);

                if (menu == null)
                    throw new NullReferenceException($"FATAL: Could not find menu item for city '{city.ToString()}'!");

                var cityMissions = MissionDescriptors.Where((m) => m.City == city);

                GameModeType?[] gameModes = {
                    GameModeType.Undercover,
                    null,
                    GameModeType.TakeARide,
                    null,
                    GameModeType.QuickChase,
                    GameModeType.QuickGetaway,
                    GameModeType.TrailBlazer,
                    GameModeType.Survival,
                    GameModeType.CheckpointRace,
                    GameModeType.GateRace,
                };

                foreach (var gameMode in gameModes)
                {
                    IEnumerable<MissionDescriptor> missions = (gameMode != null) 
                        ? cityMissions.Where((m) => m.GameMode == gameMode)
                        : null;

                    if (missions == null)
                    {
                        menu.DropDownItems.Add(new ToolStripSeparator());
                        continue;
                    }

                    ToolStripMenuItem menuItem = null;

                    switch (gameMode)
                    {
                    case GameModeType.Undercover:
                        {
                            menuItem = new ToolStripMenuItem("Undercover");

                            foreach (var mission in missions)
                            {
                                var subItems = new List<ToolStripMenuItem>() {
                                    new ToolStripMenuItem("Intro") { Tag = mission.MissionIds[0] }
                                };

                                for (int i = 1; i < mission.MissionIds.Length; i++)
                                    subItems.Add(new ToolStripMenuItem($"Part {i}") { Tag = mission.MissionIds[i] });

                                var subMenuItem = BuildMenuItem(mission, subItems);
                                menuItem.DropDownItems.Add(subMenuItem);
                            }

                            menu.DropDownItems.Add(menuItem);
                        } break;
                    case GameModeType.TakeARide:
                        {
                            foreach (var mission in missions)
                            {
                                if (menuItem != null)
                                    throw new InvalidOperationException($"Too many Take a Ride missions defined for {city.ToString()}!");

                                var subItems = new List<ToolStripMenuItem>() {
                                    new ToolStripMenuItem("Default")    { Tag = mission.MissionIds[0] },
                                    new ToolStripMenuItem("Semi-truck") { Tag = mission.MissionIds[1] },
                                };

                                menuItem = BuildMenuItem(mission, subItems);
                                menu.DropDownItems.Add(menuItem);
                            }
                        } break;
                    case GameModeType.QuickChase:
                    case GameModeType.QuickGetaway:
                    case GameModeType.TrailBlazer:
                    case GameModeType.CheckpointRace:
                    case GameModeType.GateRace:
                        {
                            foreach (var mission in missions)
                            {
                                if (menuItem != null)
                                    throw new InvalidOperationException($"Too many {gameMode.ToString()} driving games defined for {city.ToString()}!");

                                var subItems = new List<ToolStripMenuItem>();

                                for (int i = 0; i < mission.MissionIds.Length; i++)
                                    subItems.Add(new ToolStripMenuItem($"Sub-game {i + 1}") { Tag = mission.MissionIds[i] });

                                menuItem = BuildMenuItem(mission, subItems);
                                menu.DropDownItems.Add(menuItem);
                            }
                        } break;
                    case GameModeType.Survival:
                        {
                            foreach (var mission in missions)
                            {
                                if (menuItem != null)
                                    throw new InvalidOperationException($"Too many Survival's defined for {city.ToString()}!");

                                menuItem = BuildMenuItem(mission);
                                menu.DropDownItems.Add(menuItem);
                            }
                        } break;
                    }
                }
            }
        }
        
        private void GenerateExportedMissionObjects()
        {
            var widget = new InspectorWidget();
            var nodes = widget.Nodes;

            Cursor = Cursors.WaitCursor;
            
            var objectsData = MissionPackage.MissionData.Objects;
            var numObjects = objectsData.Objects.Count;
            
            for (int i = 0; i < numObjects; i++)
            {
                var obj = objectsData[i];

                nodes.Nodes.Add(new TreeNode() {
                    Text = String.Format("[{0}]: {1}", i, ExportedMissionObjects.GetObjectNameById(obj.TypeId)),
                    Tag = obj
                });
            }
            
            SafeAddControl(widget);

            Cursor = Cursors.Default;
        }
        
        private void GenerateWireCollection()
        {
            // Get widget ready
            InspectorWidget Widget = new InspectorWidget();
            TreeView Nodes = Widget.Nodes;

            Cursor = Cursors.WaitCursor;

            List<NodeDefinition> nodeDefs = MissionPackage.MissionData.LogicData.Nodes.Definitions;
            var wireCollections = MissionPackage.MissionData.LogicData.WireCollection.WireCollections;
            int nWires = wireCollections.Count;
            
            for (int w = 0; w < nWires; w++)
            {
                var wires = wireCollections[w].Wires;
                var lNodeIdx = nodeDefs.FindIndex(0, (def) => (int)def.Properties[0].Value == w);

                var lNode = nodeDefs[lNodeIdx];
                var lNodeName = MissionPackage.MissionData.LogicData.StringCollection[lNode.StringId];
                
                var text = $"[{lNodeIdx}]: {NodeTypes.GetNodeType(lNode.TypeId)}";

                if (!String.IsNullOrEmpty(lNodeName))
                    text = $"{text} \"{lNodeName}\"";
                
                var wireGroupNode = new TreeNode() {
                    BackColor = lNode.Color,
                    Text = $"[{w}]: <{text}>",
                    Tag = lNode,
                };

                for (int n = 0; n < wires.Count; n++)
                {
                    var wire = wires[n];
                    
                    var node = nodeDefs[wire.NodeId];
                    var nodeName = MissionPackage.MissionData.LogicData.StringCollection[node.StringId];

                    var wireText = $"[{wire.NodeId}]: {NodeTypes.GetNodeType(wire.OpCode)}";

                    if (!String.IsNullOrEmpty(lNodeName))
                        wireText = $"{wireText} \"{nodeName}\"";

                    var wireNode = new TreeNode() {
                        BackColor = node.Color,
                        Text = $"[{n}]: {wire.GetWireNodeType()}: <{wireText}>",
                        Tag = wire,
                    };
                    
                    wireGroupNode.Nodes.Add(wireNode);
                }

                Nodes.Nodes.Add(wireGroupNode);
            }

            Nodes.ExpandAll();

            SafeAddControl(Widget);
            
            Cursor = Cursors.Default;
        }
        
        private void AddNodeProperty(TreeNode node, NodeProperty prop)
        {
            var propName = MissionPackage.MissionData.LogicData.StringCollection[prop.StringId];
            var propValue = prop.ToString();
            
            if (prop is IntegerProperty)
            {
                var value = (int)prop.Value;
                
                switch (prop.TypeId)
                {
                case 7:
                    if (value != -1)
                    {
                        var actor = MissionPackage.MissionData.LogicData.Actors[value];
                        var actorName = NodeTypes.GetActorType(actor.TypeId);
                        var actorText = MissionPackage.MissionData.LogicData.StringCollection[actor.StringId];

                        if (actorText != "Unknown" && actorText != "Unnamed")
                            actorName = String.Format("{0} \"{1}\"", actorName, MissionPackage.MissionData.LogicData.StringCollection[actor.StringId]);

                        propValue = String.Format("<[{0}]: {1}>", value, actorName);
                    }
                    break;
                case 9:
                    propValue = String.Format("0x{0:X8}", value);
                    break;
                case 20:
                    if (value != -1)
                    {
                        if (MissionPackage.HasLocaleString(value))
                            propValue = String.Format("\"{0}\"", MissionPackage.GetLocaleString(value));
                    }
                    break;
                }
            }
            else
            {
                switch (prop.TypeId)
                {
                case 2:
                    propValue = String.Format("{0:0.0###}", (float)prop.Value);
                    break;
                case 3:
                case 8:
                    {
                        var strId = (short)prop.Value;

                        // wut
                        if (strId < 0)
                            strId &= 0xFF;

                        propValue = String.Format("\"{0}\"", MissionPackage.MissionData.LogicData.StringCollection[strId]);

                        if (prop.TypeId == 8)
                            propValue = String.Format("{{ {0}, {1} }}", propValue, ((TextFileItemProperty)prop).Index);
                    } break;
                }
            }
            
            var propNode = new TreeNode() {
                Text = (prop.TypeId != 19) ? $"{propName}: {propValue}" : propName,
                Tag = prop
            };

            // Add property node to main node
            node.Nodes.Add(propNode);
        }

        private void StyleNode(TreeNode node, NodeDefinition def)
        {
            var text = (def is ActorDefinition) ? NodeTypes.GetActorType(def.TypeId) : NodeTypes.GetNodeType(def.TypeId);
            var name = MissionPackage.MissionData.LogicData.StringCollection[def.StringId];

            if (name != "Unknown" && name != "Unnamed")
                text = String.Format("{0} \"{1}\"", text, name);

            node.Text = text;
        }
        
        private void StyleWireNode(TreeNode node, TreeNode defNode, WireNode wire)
        {
            node.Text = String.Format("{0}: <{1}>", wire.GetWireNodeType(), defNode.Text);
        }

        private TreeNode CreateNode(NodeDefinition def)
        {
            var node = new TreeNode() {
                BackColor = def.Color,
                Tag = def
            };

            StyleNode(node, def);

            foreach (var prop in def.Properties)
                AddNodeProperty(node, prop);

            return node;
        }

        private void CreateNodes<T>(List<T> definitions)
            where T : NodeDefinition
        {
            // Get widget ready
            var inspector = new InspectorWidget();
            var nodes = inspector.Nodes;

            inspector.Nodes.NodeMouseDoubleClick += (o, e) => {
                var tag = e.Node.Tag;

                if (tag is ActorProperty)
                {
                    var prop = tag as ActorProperty;

                    if (prop.Value == -1)
                        return;

                    if (e.Node.Nodes.Count == 0)
                    {
                        var actor = MissionPackage.MissionData.LogicData.Actors[prop.Value] as ActorDefinition;
                        
                        foreach (var actorProp in actor.Properties)
                            AddNodeProperty(e.Node, actorProp);

                        e.Node.Expand();
                    }
                    else
                    {
                        e.Node.Nodes.Clear();
                    }
                }
            };

            inspector.Nodes.NodeMouseClick += (o, e) => {
                if (e.Button == MouseButtons.Right)
                {
                    var node = e.Node;
                    var tag = e.Node.Tag;
                    
                    if (tag is NodeDefinition)
                    {
                        var def = tag as NodeDefinition;

                        Form prompt = new Form() {
                            Width   = 500,
                            Height  = 150,

                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            StartPosition = FormStartPosition.CenterScreen,

                            Text = "Name"
                        };

                        Label textLabel = new Label() {
                            Left    = 50,
                            Top     = 20,

                            Text = "Enter a new name:"
                        };
                        
                        TextBox textBox = new TextBox() {
                            Left    = 50,
                            Top     = 50,

                            Width   = 400,

                            SelectedText = MissionPackage.MissionData.LogicData.StringCollection[def.StringId]
                        };

                        Button confirmation = new Button() {
                            Left    = 350,
                            Top     = 70,

                            Width   = 100,
                            DialogResult = DialogResult.OK,

                            Text = "Ok"
                        };

                        confirmation.Click += (sender, ee) => { prompt.Close(); };

                        prompt.Controls.Add(textBox);
                        prompt.Controls.Add(confirmation);
                        prompt.Controls.Add(textLabel);
                        prompt.AcceptButton = confirmation;

                        if (prompt.ShowDialog() == DialogResult.OK)
                        {
                            def.StringId = (short)MissionPackage.MissionData.LogicData.StringCollection.AppendString(textBox.Text);

                            StyleNode(node, def);
                            node.Text = String.Format("[{0}]: {1}", nodes.Nodes.IndexOf(node), node.Text);
                        }
                    }
                }
            };

            Cursor = Cursors.WaitCursor;
            
            var count = definitions.Count;
            
            // Build main nodes
            for (int i = 0; i < count; i++)
            {
                var def = definitions[i];
                var node = CreateNode(def);

                node.Text = String.Format("[{0}]: {1}", i, node.Text);
                
                // Add main node to master node list
                nodes.Nodes.Add(node);
            }

            // Load wires (logic nodes only)
            for (int i = 0; i < count; i++)
            {
                var def = definitions[i];

                // Skip actor definitions
                if (def is ActorDefinition)
                    break;

                var prop = def.Properties[0];
                var node = nodes.Nodes[i];

                if (node.Nodes.Count == 0)
                    continue;

                node = node.Nodes[0]; // pWireCollection
                
                var wireId = (int)prop.Value;

                foreach (var wire in MissionPackage.MissionData.LogicData.WireCollection.WireCollections[wireId].Wires)
                {
                    var defNode = nodes.Nodes[wire.NodeId];

                    var wireNode = new TreeNode() {
                        BackColor = defNode.BackColor,
                        Tag = wire
                    };

                    StyleWireNode(wireNode, defNode, wire);

                    node.Nodes.Add(wireNode);
                }
            }

            nodes.ExpandAll();

            SafeAddControl(inspector);
            
            Cursor = Cursors.Default;
        }

        private void GenerateLogicNodes()
        {
            CreateNodes(MissionPackage.MissionData.LogicData.Nodes.Definitions);
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
            CreateNodes(MissionPackage.MissionData.LogicData.Actors.Definitions);
        }

        public void GenerateDefinition(FlowgraphWidget flowgraph, NodeDefinition def, int x, int y)
        {
            IDictionary<int, string> opcodes =
                (def.Properties[0].TypeId == 19)
                ? NodeTypes.LogicNodeTypes
                : NodeTypes.ActorNodeTypes;

            string strName = MissionPackage.MissionData.LogicData.StringCollection[def.StringId];
            string nodeName = (strName == "Unknown" || strName == "Unnamed") ? String.Empty : String.Format("\"{0}\"", strName);
            string opcodeName = opcodes.ContainsKey(def.TypeId) ? opcodes[def.TypeId] : def.TypeId.ToString();

            NodeWidget node = new NodeWidget() {
                Flowgraph = flowgraph,
                //BackColor = Color.FromArgb(def.Byte4, def.Byte1, def.Byte2, def.Byte3),
                HeaderText = String.Format("{0}: {1} {2}", MissionPackage.MissionData.LogicData.Nodes.Definitions.IndexOf(def), opcodeName, nodeName),
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
                NodeProperty prop = def.Properties[p];

                string propName = MissionPackage.MissionData.LogicData.StringCollection[prop.StringId];

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

        public void CreateLogicNodesFlowgraph(IList<NodeDefinition> definition)
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
                (definition[0].Properties[0].TypeId == 19)
                ? NodeTypes.LogicNodeTypes
                : NodeTypes.ActorNodeTypes;

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

                for (int w = 0; w < MissionPackage.MissionData.LogicData.WireCollection[wireId].Wires.Count; w++)
                {
                    Flowgraph.Nodes[i].Left = x;
                    Flowgraph.Nodes[i].Top = y;

                    Flowgraph.Nodes[MissionPackage.MissionData.LogicData.WireCollection[wireId][w].NodeId].Left = x + Flowgraph.Nodes[i].Left + 235;
                    Flowgraph.Nodes[MissionPackage.MissionData.LogicData.WireCollection[wireId][w].NodeId].Top = y + Flowgraph.Nodes[i].Top;

                    Flowgraph.LinkNodes(Flowgraph.Nodes[i], Flowgraph.Nodes[MissionPackage.MissionData.LogicData.WireCollection[wireId][w].NodeId]);

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

            for (int i = 0; i < MissionPackage.MissionData.LogicData.StringCollection.Count; i++)
                DataGrid.Rows.Add(i, MissionPackage.MissionData.LogicData.StringCollection[i]);

            SafeAddControl(DataGridWidget);

            Cursor = Cursors.Default;
        }

        private void GenerateActorSetTable()
        {
            //DataGridWidget DataGridWidget = new DataGridWidget();
            //DataGridView DataGrid = DataGridWidget.DataGridView;
            //
            //Cursor = Cursors.WaitCursor;
            //
            //for (int i = 0; i < MissionPackage.ActorSetTable.Count; i++)
            //    DataGrid.Rows.Add(i, MissionPackage.ActorSetTable[i]);
            //
            //SafeAddControl(DataGridWidget);
            //
            //Cursor = Cursors.Default;
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
                    MessageBox.Show("Sorry, not implemented.", "Zartex", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //GenerateActorSetTable();
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
                    MessageBox.Show("Sorry, not implemented.", "Zartex", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                }

                Console.WriteLine("Couldn't find anything...");
            }
            else
                MessageBox.Show("No mission loaded!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
