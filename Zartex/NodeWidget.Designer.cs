namespace Zartex
{
    partial class NodeWidget
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Container = new System.Windows.Forms.SplitContainer();
            this.Header = new System.Windows.Forms.Label();
            this.nodeOut = new System.Windows.Forms.PictureBox();
            this.nodeIn = new System.Windows.Forms.PictureBox();
            this.Properties = new System.Windows.Forms.FlowLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.Container)).BeginInit();
            this.Container.Panel1.SuspendLayout();
            this.Container.Panel2.SuspendLayout();
            this.Container.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nodeOut)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeIn)).BeginInit();
            this.SuspendLayout();
            // 
            // Container
            // 
            this.Container.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Container.BackColor = System.Drawing.Color.Black;
            this.Container.Location = new System.Drawing.Point(18, 0);
            this.Container.Margin = new System.Windows.Forms.Padding(0);
            this.Container.Name = "Container";
            this.Container.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // Container.Panel1
            // 
            this.Container.Panel1.BackColor = System.Drawing.Color.Transparent;
            this.Container.Panel1.Controls.Add(this.Header);
            this.Container.Panel1.Padding = new System.Windows.Forms.Padding(1, 1, 1, 0);
            // 
            // Container.Panel2
            // 
            this.Container.Panel2.BackColor = System.Drawing.Color.Transparent;
            this.Container.Panel2.Controls.Add(this.Properties);
            this.Container.Panel2.Padding = new System.Windows.Forms.Padding(1);
            this.Container.Size = new System.Drawing.Size(164, 125);
            this.Container.SplitterDistance = 25;
            this.Container.SplitterWidth = 1;
            this.Container.TabIndex = 0;
            // 
            // Header
            // 
            this.Header.BackColor = System.Drawing.SystemColors.ControlDark;
            this.Header.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Header.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.75F);
            this.Header.Location = new System.Drawing.Point(1, 1);
            this.Header.Margin = new System.Windows.Forms.Padding(0);
            this.Header.Name = "Header";
            this.Header.Size = new System.Drawing.Size(162, 24);
            this.Header.TabIndex = 0;
            this.Header.Text = "Logic Node";
            this.Header.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // nodeOut
            // 
            this.nodeOut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nodeOut.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.nodeOut.Location = new System.Drawing.Point(182, 56);
            this.nodeOut.Margin = new System.Windows.Forms.Padding(0);
            this.nodeOut.Name = "nodeOut";
            this.nodeOut.Size = new System.Drawing.Size(18, 18);
            this.nodeOut.TabIndex = 1;
            this.nodeOut.TabStop = false;
            this.nodeOut.MouseDown += new System.Windows.Forms.MouseEventHandler(this.nodeOut_MouseDown);
            this.nodeOut.MouseMove += new System.Windows.Forms.MouseEventHandler(this.nodeOut_MouseMove);
            this.nodeOut.MouseUp += new System.Windows.Forms.MouseEventHandler(this.nodeOut_MouseUp);
            // 
            // nodeIn
            // 
            this.nodeIn.BackColor = System.Drawing.Color.Thistle;
            this.nodeIn.Location = new System.Drawing.Point(0, 56);
            this.nodeIn.Margin = new System.Windows.Forms.Padding(0);
            this.nodeIn.Name = "nodeIn";
            this.nodeIn.Size = new System.Drawing.Size(18, 18);
            this.nodeIn.TabIndex = 2;
            this.nodeIn.TabStop = false;
            // 
            // Properties
            // 
            this.Properties.BackColor = System.Drawing.SystemColors.ControlLight;
            this.Properties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Properties.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.25F);
            this.Properties.Location = new System.Drawing.Point(1, 1);
            this.Properties.Name = "Properties";
            this.Properties.Padding = new System.Windows.Forms.Padding(2);
            this.Properties.Size = new System.Drawing.Size(162, 97);
            this.Properties.TabIndex = 0;
            // 
            // NodeWidget
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.nodeIn);
            this.Controls.Add(this.nodeOut);
            this.Controls.Add(this.Container);
            this.DoubleBuffered = true;
            this.Name = "NodeWidget";
            this.Size = new System.Drawing.Size(200, 125);
            this.DoubleClick += new System.EventHandler(this.NodeWidget_DoubleClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.NodeWidget_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.NodeWidget_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.NodeWidget_MouseUp);
            this.Container.Panel1.ResumeLayout(false);
            this.Container.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Container)).EndInit();
            this.Container.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nodeOut)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeIn)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer Container;
        public System.Windows.Forms.Label Header;
        public System.Windows.Forms.PictureBox nodeOut;
        public System.Windows.Forms.PictureBox nodeIn;
        public System.Windows.Forms.FlowLayoutPanel Properties;
    }
}
