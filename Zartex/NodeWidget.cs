using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Zartex.LogicData;
using Zartex.LogicExport;

namespace Zartex
{
    public partial class NodeWidget : UserControl
    {
        public NodeWidget()
        {
            InitializeComponent();

            _input = new List<PictureBox>();
            _output = new List<PictureBox>();

            Container.MouseDown += (o, e) => NodeWidget_MouseDown(o, e);
            Container.MouseDoubleClick += (o, e) => NodeWidget_DoubleClick(o, e);
            Container.MouseMove += (o, e) => NodeWidget_MouseMove(o, e);

            Header.MouseDown += (o, e) => NodeWidget_MouseDown(o, e);
            Header.MouseDoubleClick += (o, e) => NodeWidget_DoubleClick(o, e);
            Header.MouseMove += (o, e) => NodeWidget_MouseMove(o, e);
            
            Properties.MouseDown += (o, e) => NodeWidget_MouseDown(o, e);
            Properties.MouseDoubleClick += (o, e) => NodeWidget_DoubleClick(o, e);
            Properties.MouseMove += (o, e) => NodeWidget_MouseMove(o, e);
        }

        public Point MouseDownLocation;
        public Point outNodeClick;
        public Point mouseDrawLocation;

        public event PaintNodeEventHandler NodeAdded;
        public event PaintNodeEventHandler NodeUpdated;
        public event MouseEventHandler NodeClicked;
        public event PaintDrawingEventHandler NodeDrawing;

        bool nodeSelected = false;
        bool drawing = false;

        public bool IsSelected
        {
            get { return nodeSelected; }
        }

        public bool IsDrawing
        {
            get { return drawing; }
        }

        public PaintDrawingEventArgs _line;

        public int nodeId;

        private IList<PictureBox> _input;
        private IList<PictureBox> _output;

        public string HeaderText
        {
            get { return Header.Text; }
            set { Header.Text = value; }
        }

        public IList<PictureBox> Inputs
        {
            get { return _input; }
        }

        public IList<PictureBox> Outputs
        {
            get { return _output; }
        }

        public void AddInput(PictureBox input)
        {
            if (input.Parent != null && input.Parent.GetType() == typeof(NodeWidget))
            {
                _input.Add(input);
                OnNodeAdded(new PaintNodeEventArgs(nodeIn, input));
            }
        }

        public void AddOutput(PictureBox output)
        {
            if (output.Parent != null && output.Parent.GetType() == typeof(NodeWidget))
            {
                _output.Add(output);
                OnNodeAdded(new PaintNodeEventArgs(output, nodeOut));
            }
        }

        public void PaintNodes(PaintEventArgs e)
        {
            //Console.WriteLine("Refreshing {0} outputs...", Outputs.Count);

            foreach (PictureBox output in _output)
            {
                //Console.WriteLine("Refreshing {0}...", output.Parent.Name);
                OnNodeUpdated(new PaintNodeEventArgs(output, nodeOut, e));
            }

            //Console.WriteLine("Node painting done.");
        }

        public void DrawLines(PaintEventArgs e)
        {
            //OnNodeDraw(new PaintDrawingEventArgs(_line.BasePoint, _line.DragPoint, e));
            Flowgraph.OnLineDraw(this, e);
        }

        protected virtual void OnNodeAdded(PaintNodeEventArgs e)
        {
            if (NodeAdded != null)
                NodeAdded(this, e);
        }

        protected virtual void OnNodeUpdated(PaintNodeEventArgs e)
        {
            if (NodeUpdated != null)
                NodeUpdated(this, e);
        }

        protected virtual void OnNodeClicked(object sender, MouseEventArgs e)
        {
            if (NodeClicked != null)
                NodeClicked(sender, e);
        }

        protected virtual void OnNodeDraw(PaintDrawingEventArgs e)
        {
            if (NodeDrawing != null)
                NodeDrawing(this, e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_line != null)
                DrawLines(e);
        }

        private void NodeWidget_DoubleClick(object sender, EventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            while (stopwatch.ElapsedMilliseconds != 250)
            {
                this.Visible = false;
            }

            this.Visible = true;
            stopwatch.Stop();
        }

        private void NodeWidget_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                MouseDownLocation = PointToClient(PointToScreen(e.Location));

                OnNodeClicked(sender, e);

                if (!Flowgraph.Parent.Focused)
                    Flowgraph.Parent.Focus();
            }
        }

        private void NodeWidget_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (!Flowgraph.Parent.Focused)
                    Flowgraph.Parent.Focus();

                this.Left = PointToClient(PointToScreen(e.Location)).X + this.Left - MouseDownLocation.X;
                this.Top = PointToClient(PointToScreen(e.Location)).Y + this.Top - MouseDownLocation.Y;

                this.Flowgraph.Invalidate();
            }
        }

        public FlowgraphWidget Flowgraph
        {
            get
            {
                if (Parent.GetType() != typeof(FlowgraphWidget))
                    throw new Exception("Something went horribly wrong with a logic node's parent property! Please fix!");
    
                return (FlowgraphWidget)Parent;
            }
            set
            {
                this.Parent = value;
                ((FlowgraphWidget)Parent).AddNode(this);

                Parent.GotFocus += (o, e) => { Parent.Parent.Focus(); };
            }
        }

        private Point GetOutNodeLocation()
        {
            Point p = this.Parent.PointToClient(nodeOut.Parent.PointToScreen(nodeOut.Location));

            p.X += nodeOut.Bounds.Width;
            p.Y += nodeOut.Bounds.Height / 2;

            return p;
        }

        #region nodeOut methods (FIX ME!)
        private void nodeOut_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                nodeSelected = true;
            }
        }

        private void nodeOut_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (nodeSelected)
                {
                    drawing = true;

                    outNodeClick = GetOutNodeLocation();

                    Point p = new Point(e.Location.X + outNodeClick.X - nodeOut.Bounds.Width - 1, e.Location.Y + outNodeClick.Y - nodeOut.Bounds.Height / 2);

                    mouseDrawLocation = p;

                    _line = new PaintDrawingEventArgs(outNodeClick, mouseDrawLocation);

                    OnNodeDraw(_line);

                    Flowgraph.Invalidate();

                    //OnNodeDraw(_line);

                    //OnNodeDraw(new PaintDrawingEventArgs(outNodeClick, mouseDrawLocation));

                    // Flowgraph.Refresh();
                    // 
                    // using (Pen pen = new Pen(new SolidBrush(Color.FromArgb(25, 25, 25)), 1F))
                    // using (Graphics g = this.Parent.CreateGraphics())
                    // {
                    //     g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    //     g.DrawLine(pen, outNodeClick, mouseDrawLocation);
                    // }
                }
            }
        }

        private void nodeOut_MouseUp(object sender, MouseEventArgs e)
        {
            if (nodeSelected && drawing)
            {
                drawing = false;

                NodeWidget node = (NodeWidget)Flowgraph.GetChildAtPoint(mouseDrawLocation);

                if (node != null)
                    Flowgraph.LinkNodes(this, node);

                Flowgraph.OnLineDrawingFinished();
                Flowgraph.Invalidate();

                //Console.WriteLine((node != null) ? node.Location.ToString() : String.Empty);

                // Flowgraph.Refresh();
                // 
                // using (Pen pen = new Pen(new SolidBrush(Color.FromArgb(25, 25, 25)), 1F))
                // using (Graphics g = this.Parent.CreateGraphics())
                // {
                //     g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                //     g.DrawLine(pen, outNodeClick, mouseDrawLocation);
                // }
            }
        }
        #endregion

        private void Properties_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                Console.WriteLine();
            }
        }

        private void NodeWidget_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (!Flowgraph.Parent.Focused)
                    Flowgraph.Parent.Focus();

                nodeSelected = false;
            }
        }

    }

    public delegate void PaintNodeEventHandler(object sender, PaintNodeEventArgs e);

    public class PaintNodeEventArgs
    {
        public PictureBox SenderNode { get; set; }
        public PictureBox ReceiverNode { get; set; }

        public PaintEventArgs PaintEventArgs { get; set; }

        public Point ReceiverScreenLocation
        {
            get { return ReceiverNode.Parent.PointToScreen(ReceiverNode.Location); }
        }

        public Point SenderScreenLocation
        {
            get { return SenderNode.Parent.PointToScreen(SenderNode.Location); }
        }

        public PaintNodeEventArgs(PictureBox receiverNode, PictureBox senderNode)
        {
            ReceiverNode = receiverNode;
            SenderNode = senderNode;
        }

        public PaintNodeEventArgs(PictureBox receiverNode, PictureBox senderNode, PaintEventArgs e)
            : this(receiverNode, senderNode)
        {
            PaintEventArgs = e;
        }
    }

    public delegate void PaintDrawingEventHandler(object sender, PaintDrawingEventArgs e);

    public class PaintDrawingEventArgs
    {
        public Point BasePoint { get; set; }
        public Point DragPoint { get; set; }

        public PaintEventArgs PaintEventArgs { get; set; }

        public PaintDrawingEventArgs(Point basePoint, Point dragPoint)
        {
            BasePoint = basePoint;
            DragPoint = dragPoint;
        }

        public PaintDrawingEventArgs(Point basePoint, Point dragPoint, PaintEventArgs e) : this(basePoint, dragPoint)
        {
            PaintEventArgs = e;
        }
    }
}
