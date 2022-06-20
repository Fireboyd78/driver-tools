using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Diagnostics;
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
                node.NodeAdded += (o, e) => OnNodeAdded(o, e);
                node.NodeUpdated += (o, e) => OnNodeUpdated(o, e);
                node.NodeDrawing += (o, e) => OnLineDrawing(o, e);

                _nodes.Add(node);
            }
        }

        public void InsertNode(int index, NodeWidget node)
        {
            if (!_nodes.Contains(node))
            {
                node.NodeAdded += (o, e) => OnNodeAdded(o, e);
                node.NodeUpdated += (o, e) => OnNodeUpdated(o, e);
                node.NodeDrawing += (o, e) => OnLineDrawing(o, e);

                _nodes.Insert(index, node);
            }
        }

        public void AddNodes(params NodeWidget[] nodes)
        {
            foreach (NodeWidget node in nodes)
                AddNode(node);
        }

        public void LinkNodes(NodeWidget output, NodeWidget input, WireNodeType nodeType = WireNodeType.GroupEnable)
        {
            if (!_nodes.Contains(output))
                AddNode(output);
            if (!_nodes.Contains(input))
                AddNode(input);

            var idx = _nodes.IndexOf(output);
            var node = _nodes[idx];
            
            node.AddWire(input, nodeType);
        }

        public void OnNodeAdded(object sender, PaintNodeEventArgs e)
        {
            using (Graphics g = CreateGraphics())
            {
                e.PaintEventArgs = new PaintEventArgs(g, RectangleToScreen(ClientRectangle));
                OnNodeUpdated(sender, e);
            }
        }

        public void MoveNode(NodeWidget node, Point location, Point origin)
        {
            if (!Parent.Focused)
                Parent.Focus();

            var position = PointToClient(PointToScreen(location));

            node.Left = position.X + node.Left - origin.X;
            node.Top = position.Y + node.Top - origin.Y;
            node.BringToFront();

            Invalidate();
        }

        public void LayoutNode(NodeWidget node)
        {
            var nWires = node.Links.Count;

            var x = node.Left + (node.Width + 35);
            var y = node.Top - ((16 + 25) * nWires);

            if (y < 16)
                y = 16;

            foreach (var link in node.Links)
            {
                var child = link.Link.Parent as NodeWidget;

                child.Left = x;
                child.Top = y;

                y += (node.Height + 25);
            }

            Invalidate();
        }

        public void OnNodeUpdated(object sender, PaintNodeEventArgs e)
        {
            var args = e.PaintEventArgs;
            var gfx = args.Graphics;

            gfx.SmoothingMode = SmoothingMode.HighSpeed;
            
            using (var pen = new Pen(new SolidBrush(e.WireColor), 1.0f))
            {
                var nodeL = PointToClient(e.SenderScreenLocation);
                var nodeR = PointToClient(e.ReceiverScreenLocation);
                
                var boundL = e.SenderNode.Bounds;
                var boundR = e.ReceiverNode.Bounds;

                nodeL.X += boundL.Width - 1;
                nodeL.Y += boundL.Height / 2;
                
                nodeR.Y += boundR.Height / 2;
                
                var points = new List<Point>();

                points.Add(nodeL);

                nodeL.X += (10 + e.WireOffset);
                points.Add(nodeL);

                nodeR.X -= (10 + e.WireOffset);

                if (nodeR.X < nodeL.X)
                {
                    if (nodeR.Y < nodeL.Y)
                    {
                        nodeL.Y -= (e.SenderNode.Top + 15);
                        points.Add(nodeL);

                        var nodeM = new Point(nodeR.X, nodeR.Y + (e.ReceiverNode.Bottom + 15));

                        points.Add(nodeM);
                        points.Add(nodeR);
                    }
                    else
                    {
                        nodeL.Y += (e.SenderNode.Bottom + 15);
                        points.Add(nodeL);

                        var nodeM = new Point(nodeR.X, nodeR.Y - (e.ReceiverNode.Top + 15));

                        points.Add(nodeM);
                        points.Add(nodeR);
                    }
                }
                else
                {
                    points.Add(nodeR);
                }

                nodeR.X += (10 + e.WireOffset);
                points.Add(nodeR);

                for (int p = 1; p < points.Count; p++)
                    gfx.DrawLine(pen, points[p - 1], points[p]);
            }
        }

        public void OnLineDrawing(object sender, PaintDrawingEventArgs e)
        {
            using (var g = CreateGraphics())
            {
                e.PaintEventArgs = new PaintEventArgs(g, Parent.ClientRectangle);
        
                _nodeDraw = ((NodeWidget)sender);
        
                OnLineDraw(sender, e.PaintEventArgs);
            }
        }
        
        public void OnLineDraw(object sender, PaintEventArgs e)
        {
            if (_nodeDraw != null)
            {
                using (var pen = new Pen(new SolidBrush(Color.FromArgb(192, 192, 192)), 1.0f))
                {
                    Point basePoint = _nodeDraw._line.BasePoint;
                    Point dragPoint = _nodeDraw._line.DragPoint;
        
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
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

        ScrollableControl scrollPanel = null;

        bool mouseDragging = false;
        Point mouseOrigin = new Point(0, 0);
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (mouseDragging)
            {
                var origin = scrollPanel.AutoScrollPosition;
                var target = e.Location;
                
                var delta = new Point() {
                    X = (target.X - mouseOrigin.X),
                    Y = (target.Y - mouseOrigin.Y),
                };

                var position = new Point() {
                    X = -(origin.X + delta.X),
                    Y = -(origin.Y + delta.Y),
                };
                
                scrollPanel.AutoScrollPosition = position;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            switch (e.Button)
            {
            case MouseButtons.Left:
                if (mouseDragging)
                {
                    mouseDragging = false;
                    scrollPanel = null;
                }
                break;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            switch (e.Button)
            {
            case MouseButtons.Left:
                // hacks, cause I'm an idiot
                scrollPanel = Parent as ScrollableControl;

                if (scrollPanel != null)
                {
                    mouseDragging = true;
                    mouseOrigin = e.Location;
                }

                break;
            case MouseButtons.Right:
                addNodeLocation = PointToScreen(e.Location);
                contextMenuStrip1.Show(addNodeLocation);
                break;
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
