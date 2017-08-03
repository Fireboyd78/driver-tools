using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
            //var wire = e.Node.Tag as WireCollectionEntry;
            //
            //if (wire != null)
            //{
            //    var prop = e.Node.Parent;
            //
            //    if (prop != null && prop.Tag is WireCollectionProperty)
            //        Inspector.SelectedObject = Nodes.Nodes[wire.NodeId].Tag;
            //
            //}

            Inspector.SelectedObject = e.Node.Tag;
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
            var tag = e.Node.Tag;

            if (tag is WireNode)
            {
                if (e.Node.Nodes.Count == 0)
                {
                    var wire = tag as WireNode;

                    var node = Nodes.Nodes[wire.NodeId];
                    var nProps = node.Nodes.Count;

                    /*
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
                    }*/

                    if (nProps > 0)
                    {
                        // experimental node expansion
                        foreach (TreeNode propNode in node.Nodes)
                        {
                            var newNode = (TreeNode)propNode.Clone();

                            e.Node.Nodes.Add(newNode);
                        }
                    }
                    else
                    {
                        var newNode = (TreeNode)node.Clone();
                        e.Node.Nodes.Add(newNode);
                    }

                    e.Node.Expand();
                }
                else
                {
                    e.Node.Nodes.Clear();
                }
            }
        }
    }
}
