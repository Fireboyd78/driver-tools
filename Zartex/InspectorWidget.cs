using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Zartex.LogicExport;
using Zartex.LogicData;

namespace Zartex
{
    public partial class InspectorWidget : UserControl
    {
        public InspectorWidget()
        {
            InitializeComponent();
        }

        private void LogicNodes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag != null)
            {
                if ((e.Node.Parent != null && e.Node.Parent.Tag != null)
                    && e.Node.Parent.Tag.GetType() == typeof(WireCollectionProperty)
                    && e.Node.Tag.GetType() == typeof(WireCollectionEntry))
                {
                    WireCollectionEntry wire = (WireCollectionEntry)e.Node.Tag;
                    Inspector.SelectedObject = Nodes.Nodes[wire.NodeId].Tag;
                }
                else
                {
                    Inspector.SelectedObject = e.Node.Tag;
                }
            }
        }

        public void AfterNodeWidgetSelected(object sender, MouseEventArgs e)
        {
            Inspector.SelectedObject = sender;
        }

        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Nodes.ExpandAll();
        }

        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Nodes.CollapseAll();
        }

        private void LogicNodes_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is WireCollectionEntry)
            {
                if (e.Node.Nodes.Count > 0)
                {
                    e.Node.Nodes.Clear();
                    return;
                }

                var wire = e.Node.Tag as WireCollectionEntry;
                var node = Nodes.Nodes[wire.NodeId];

                int nProps = node.Nodes.Count;

                if (nProps == 1 && node.Nodes[0].Tag is LogicProperty)
                {
                    var prop = node.Nodes[0].Tag as LogicProperty;

                    if (prop.Opcode == 19)
                    {
                        foreach (TreeNode propNode in node.Nodes[0].Nodes)
                            e.Node.Nodes.Add((TreeNode)propNode.Clone());

                        e.Node.Expand();

                        return;
                    }
                }

                // experimental node expansion
                foreach (TreeNode propNode in node.Nodes)
                    e.Node.Nodes.Add((TreeNode)propNode.Clone());

                e.Node.Expand();
            }
        }
    }
}
