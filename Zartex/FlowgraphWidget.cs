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
    public partial class FlowgraphWidget : UserControl
    {
        public FlowgraphWidget()
        {
            InitializeComponent();

            _nodes = new List<NodeWidget>();
        }

        private IList<NodeWidget> _nodes;

        public IList<NodeWidget> Nodes
        {
            get { return _nodes; }
        }

        private NodeWidget _nodeDraw;

        private Point addNodeLocation;

        public void AddNode(NodeWidget node)
        {
            if (!_nodes.Contains(node))
            {
                node.NodeAdded += (o, e) => OnNodesAdded(o, e);
                node.NodeUpdated += (o, e) => OnNodesUpdated(o, e);
                node.NodeDrawing += (o, e) => OnLineDrawing(o, e);

                _nodes.Add(node);
            }
        }

        public void InsertNode(int index, NodeWidget node)
        {
            if (!_nodes.Contains(node))
            {
                node.NodeAdded += (o, e) => OnNodesAdded(o, e);
                node.NodeUpdated += (o, e) => OnNodesUpdated(o, e);
                node.NodeDrawing += (o, e) => OnLineDrawing(o, e);

                _nodes.Insert(index, node);
            }
        }

        public void AddNodes(params NodeWidget[] nodes)
        {
            foreach (NodeWidget node in nodes)
                AddNode(node);
        }

        public void LinkNodes(NodeWidget output, NodeWidget input)
        {
            if (!_nodes.Contains(output))
                AddNode(output);
            if (!_nodes.Contains(input))
                AddNode(input);

            int i = _nodes.IndexOf(output);
            
            if (!_nodes[i].Outputs.Contains(input.nodeIn))
                _nodes[i].AddOutput(input.nodeIn);
        }

        public void OnNodesAdded(object sender, PaintNodeEventArgs e)
        {
            using (Graphics g = CreateGraphics())
            {
                e.PaintEventArgs = new PaintEventArgs(g, RectangleToScreen(ClientRectangle));
                OnNodesUpdated(sender, e);
            }
        }

        public void OnNodesUpdated(object sender, PaintNodeEventArgs e)
        {
            using (Pen pen = new Pen(new SolidBrush(Color.FromArgb(128, 128, 255)), 1F))
            {
                e.PaintEventArgs.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                Point receiverNode = PointToClient(e.ReceiverScreenLocation);
                Point senderNode = PointToClient(e.SenderScreenLocation);

                senderNode.X += e.SenderNode.Bounds.Width - 1;
                senderNode.Y += e.SenderNode.Bounds.Height / 2;

                receiverNode.Y += e.ReceiverNode.Bounds.Height / 2;

                //Console.WriteLine("Drawing a line from {0} to {1}.", e.SenderNode.Parent.Name, e.ReceiverNode.Parent.Name);

                e.PaintEventArgs.Graphics.DrawLine(pen, senderNode, receiverNode);
            }
        }

        public void OnLineDrawing(object sender, PaintDrawingEventArgs e)
        {
            using (Graphics g = CreateGraphics())
            {
                e.PaintEventArgs = new PaintEventArgs(g, this.Parent.ClientRectangle);

                _nodeDraw = ((NodeWidget)sender);

                OnLineDraw(sender, e.PaintEventArgs);
            }
        }

        public void OnLineDraw(object sender, PaintEventArgs e)
        {
            if (_nodeDraw != null)
            {
                using (Pen pen = new Pen(new SolidBrush(Color.FromArgb(128, 128, 255)), 1F))
                {
                    Point basePoint = _nodeDraw._line.BasePoint;
                    Point dragPoint = _nodeDraw._line.DragPoint;

                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    e.Graphics.DrawLine(pen, basePoint, dragPoint);
                }
            }
        }

        public void OnLineDrawingFinished()
        {
            _nodeDraw = null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            foreach (NodeWidget node in _nodes)
                node.PaintNodes(e);

            if (_nodeDraw != null)
                _nodeDraw.DrawLines(e);
        }

        private void FlowgraphWidget_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                addNodeLocation = PointToScreen(e.Location);
                contextMenuStrip1.Show(addNodeLocation);
            }

            if (Focused)
                Parent.Focus();
        }

        private void newNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NodeWidget node = new NodeWidget() {
                Flowgraph = this,
                HeaderText = "NewNode1",
                Name = "newnode01",
                Left = PointToClient(addNodeLocation).X,
                Top = PointToClient(addNodeLocation).Y
            };

            AddNode(node);
        }
    }

    
}
