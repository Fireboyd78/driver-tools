using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Zartex
{
    public partial class NodeWidget : UserControl
    {
        public class WireLink
        {
            public PictureBox Source { get; set; }
            public PictureBox Link { get; set; }

            public WireNodeType Type { get; set; }

            public WireLink(PictureBox source, PictureBox link, WireNodeType type)
            {
                Source = source;
                Link = link;
                Type = type;
            }
        }

        public NodeWidget()
        {
            InitializeComponent();
            
            _links = new List<WireLink>();

            Dialog.MouseDown += (o, e) => NodeWidget_MouseDown(o, e);
            Dialog.MouseDoubleClick += (o, e) => NodeWidget_DoubleClick(o, e);
            Dialog.MouseMove += (o, e) => NodeWidget_MouseMove(o, e);

            Header.MouseDown += (o, e) => NodeWidget_MouseDown(o, e);
            Header.MouseDoubleClick += (o, e) => NodeWidget_DoubleClick(o, e);
            Header.MouseMove += (o, e) => NodeWidget_MouseMove(o, e);
            
            Properties.MouseDown += (o, e) => NodeWidget_MouseDown(o, e);
            Properties.MouseDoubleClick += (o, e) => NodeWidget_DoubleClick(o, e);
            Properties.MouseMove += (o, e) => NodeWidget_MouseMove(o, e);

            SetupNodeEvents(nodeOutCondition);
            SetupNodeEvents(nodeOutSuccess);
            SetupNodeEvents(nodeOutFailure);
        }

        public void SetupNodeEvents(PictureBox node)
        {
            node.MouseDown += OnOutputNodeMouseDown;
            node.MouseUp += OnOutputNodeMouseUp;
            node.MouseMove += OnOutputNodeMouseMove;
        }

        public Point MouseDownLocation;
        public Point outNodeClick;
        public Point mouseDrawLocation;

        public event PaintNodeEventHandler NodeAdded;
        public event PaintNodeEventHandler NodeUpdated;
        public event MouseEventHandler NodeClicked;
        public event PaintDrawingEventHandler NodeDrawing;

        PictureBox outputNode = null;
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
        
        private IList<WireLink> _links;

        public string CommentText
        {
            get { return Comment.Text; }
            set { Comment.Text = value; }
        }

        public string HeaderText
        {
            get { return Header.Text; }
            set { Header.Text = value; }
        }

        public IList<WireLink> Links
        {
            get { return _links; }
        }

        PictureBox GetInputNode(WireNodeType wireType)
        {
            switch (wireType)
            {
            case WireNodeType.GroupEnable:
            case WireNodeType.OnConditionEnable:
            case WireNodeType.OnFailureEnable:
            case WireNodeType.OnSuccessEnable:
                return nodeInEnable;
            case WireNodeType.GroupDisable:
            case WireNodeType.OnConditionDisable:
            case WireNodeType.OnFailureDisable:
            case WireNodeType.OnSuccessDisable:
                return nodeInDisable;
            default:
                throw new Exception("Unknown wire type");
            }
        }

        PictureBox GetOutputNode(WireNodeType wireType)
        {
            switch (wireType)
            {
            case WireNodeType.OnSuccessEnable:
            case WireNodeType.OnSuccessDisable:
                return nodeOutSuccess;
            case WireNodeType.OnFailureEnable:
            case WireNodeType.OnFailureDisable:
                return nodeOutFailure;
            case WireNodeType.GroupEnable:
            case WireNodeType.GroupDisable:
            case WireNodeType.OnConditionEnable:
            case WireNodeType.OnConditionDisable:
                return nodeOutCondition;
            default:
                throw new Exception("Unknown wire type");
            }
        }

        public void AddWire(NodeWidget output, WireNodeType wireType)
        {
            var source = GetOutputNode(wireType);
            var link = output.GetInputNode(wireType);
            var wire = new WireLink(source, link, wireType);

            var offset = _links.Count;
            if (offset > 0)
                offset += 2;
            _links.Add(wire);

            OnNodeAdded(new PaintNodeEventArgs(link, source) {
                WireType = wireType,
                WireOffset = offset,
            });
        }

        public void PaintNodes(PaintEventArgs e)
        {
            //Console.WriteLine("Refreshing {0} outputs...", Outputs.Count);

            var count = _links.Count;
            var offset = (count == 1) ? 0 : -count;

            for (int i = 0; i < _links.Count; i++)
            {
                var output = _links[i];

                var source = output.Source;
                var link = output.Link;

                //Console.WriteLine("Refreshing {0}...", output.Parent.Name);
                OnNodeUpdated(new PaintNodeEventArgs(link, source, e) {
                    WireType = output.Type,
                    WireOffset = offset,
                });

                offset += 3;
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
            //Stopwatch stopwatch = new Stopwatch();
            //
            //stopwatch.Start();
            //
            //while (stopwatch.ElapsedMilliseconds != 250)
            //{
            //    this.Visible = false;
            //}
            //
            //this.Visible = true;
            //stopwatch.Stop();

            Flowgraph.LayoutNode(this);
        }

        private void NodeWidget_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MouseDownLocation = PointToClient(PointToScreen(e.Location));

                OnNodeClicked(sender, e);

                if (!Flowgraph.Parent.Focused)
                    Flowgraph.Parent.Focus();
            }
        }

        private void NodeWidget_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Flowgraph.MoveNode(this, e.Location, MouseDownLocation);

                if (ModifierKeys.HasFlag(Keys.Shift))
                {
                    foreach (var link in Links)
                    {
                        var child = link.Link.Parent as NodeWidget;

                        Flowgraph.MoveNode(child, e.Location, MouseDownLocation);
                    }
                }
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
            Point p = this.Parent.PointToClient(outputNode.Parent.PointToScreen(outputNode.Location));

            p.X += outputNode.Bounds.Width;
            p.Y += outputNode.Bounds.Height / 2;

            return p;
        }

        void ClearOutputNode()
        {
            outputNode = null;
            nodeSelected = false;
        }

        void SelectOutputNode(PictureBox node, MouseEventArgs e)
        {
            outputNode = node;
            nodeSelected = true;
        }

        private void OnOutputNodeMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                SelectOutputNode(sender as PictureBox, e);
            }
        }

        private void OnOutputNodeMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (nodeSelected)
                {
                    drawing = true;

                    outNodeClick = GetOutNodeLocation();

                    Point p = new Point(e.Location.X + outNodeClick.X - outputNode.Bounds.Width - 1, e.Location.Y + outNodeClick.Y - outputNode.Bounds.Height / 2);

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

        private void OnOutputNodeMouseUp(object sender, MouseEventArgs e)
        {
            if (nodeSelected && drawing)
            {
                drawing = false;

                // !!! BROKEN !!!
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

        private void Properties_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Console.WriteLine();
            }
        }

        private void NodeWidget_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!Flowgraph.Parent.Focused)
                    Flowgraph.Parent.Focus();

                ClearOutputNode();
            }
        }

        private void NodeWidget_Load(object sender, EventArgs e)
        {

        }
    }

    public delegate void PaintNodeEventHandler(object sender, PaintNodeEventArgs e);

    public class PaintNodeEventArgs
    {
        public PictureBox SenderNode { get; set; }
        public PictureBox ReceiverNode { get; set; }

        public PaintEventArgs PaintEventArgs { get; set; }

        public WireNodeType WireType { get; set; }
        
        public int WireOffset { get; set; }

        public Color WireColor
        {
            get
            {
                switch (WireType)
                {
#if OLD_COLORS
                case WireNodeType.GroupEnable:
                case WireNodeType.OnSuccessEnable:
                    return Color.FromArgb(128, 192, 128); // green
                case WireNodeType.GroupDisable:
                case WireNodeType.OnSuccessDisable:
                    return Color.FromArgb(192, 128, 128); // red

                case WireNodeType.OnFailureEnable:
                    return Color.FromArgb(128, 192, 192); // cyan
                case WireNodeType.OnFailureDisable:
                    return Color.FromArgb(192, 128, 192); // pink

                case WireNodeType.OnConditionEnable:
                    return Color.FromArgb(192, 192, 128); // yellow
                case WireNodeType.OnConditionDisable:
                    return Color.FromArgb(128, 128, 128); // gray
#else
                case WireNodeType.OnSuccessEnable:
                case WireNodeType.OnSuccessDisable:
                    return Color.FromArgb(128, 192, 128); // green

                case WireNodeType.OnFailureEnable:
                case WireNodeType.OnFailureDisable:
                    return Color.FromArgb(192, 128, 128); // red

                case WireNodeType.GroupEnable:
                case WireNodeType.GroupDisable:
                    return Color.FromArgb(128, 192, 192); // cyan

                case WireNodeType.OnConditionEnable:
                case WireNodeType.OnConditionDisable:
                    return Color.FromArgb(192, 192, 128); // yellow
#endif
                }

                // error!
                return Color.FromArgb(255, 0, 128);
            }
        }

        public Point ReceiverLocation
        {
            get
            {
                var location = ReceiverNode.Location;

                //location.X += WireOffset;
                location.Y -= WireOffset;

                return location;
            }
        }

        public Point SenderLocation
        {
            get
            {
                var location = SenderNode.Location;

                //location.X += WireOffset;
                location.Y += WireOffset;

                return location;
            }
        }

        public Point ReceiverScreenLocation
        {
            get { return ReceiverNode.Parent.PointToScreen(ReceiverLocation); }
        }

        public Point SenderScreenLocation
        {
            get { return SenderNode.Parent.PointToScreen(SenderLocation); }
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
