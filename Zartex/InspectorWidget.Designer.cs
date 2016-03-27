namespace Zartex
{
    partial class InspectorWidget
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
            this.components = new System.ComponentModel.Container();
            this.Inspector = new System.Windows.Forms.PropertyGrid();
            this.Nodes = new System.Windows.Forms.TreeView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.expandAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.collapseAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SplitPanel = new System.Windows.Forms.SplitContainer();
            this.label1 = new System.Windows.Forms.Label();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SplitPanel)).BeginInit();
            this.SplitPanel.Panel1.SuspendLayout();
            this.SplitPanel.Panel2.SuspendLayout();
            this.SplitPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // Inspector
            // 
            this.Inspector.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Inspector.BackColor = System.Drawing.SystemColors.ControlDark;
            this.Inspector.CategoryForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.Inspector.Location = new System.Drawing.Point(0, 18);
            this.Inspector.Margin = new System.Windows.Forms.Padding(0);
            this.Inspector.Name = "Inspector";
            this.Inspector.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.Inspector.Size = new System.Drawing.Size(188, 339);
            this.Inspector.TabIndex = 0;
            // 
            // Nodes
            // 
            this.Nodes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Nodes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.Nodes.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.Nodes.ContextMenuStrip = this.contextMenuStrip1;
            this.Nodes.HideSelection = false;
            this.Nodes.Indent = 18;
            this.Nodes.ItemHeight = 19;
            this.Nodes.Location = new System.Drawing.Point(0, 0);
            this.Nodes.Margin = new System.Windows.Forms.Padding(0, 0, 1, 0);
            this.Nodes.Name = "Nodes";
            this.Nodes.Size = new System.Drawing.Size(217, 357);
            this.Nodes.TabIndex = 1;
            this.Nodes.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.LogicNodes_AfterSelect);
            this.Nodes.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.LogicNodes_NodeMouseDoubleClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.expandAllToolStripMenuItem,
            this.collapseAllToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(137, 48);
            // 
            // expandAllToolStripMenuItem
            // 
            this.expandAllToolStripMenuItem.Name = "expandAllToolStripMenuItem";
            this.expandAllToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
            this.expandAllToolStripMenuItem.Text = "Expand All";
            this.expandAllToolStripMenuItem.Click += new System.EventHandler(this.expandAllToolStripMenuItem_Click);
            // 
            // collapseAllToolStripMenuItem
            // 
            this.collapseAllToolStripMenuItem.Name = "collapseAllToolStripMenuItem";
            this.collapseAllToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
            this.collapseAllToolStripMenuItem.Text = "Collapse All";
            this.collapseAllToolStripMenuItem.Click += new System.EventHandler(this.collapseAllToolStripMenuItem_Click);
            // 
            // SplitPanel
            // 
            this.SplitPanel.BackColor = System.Drawing.SystemColors.Control;
            this.SplitPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitPanel.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.SplitPanel.Location = new System.Drawing.Point(0, 0);
            this.SplitPanel.Name = "SplitPanel";
            // 
            // SplitPanel.Panel1
            // 
            this.SplitPanel.Panel1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.SplitPanel.Panel1.Controls.Add(this.Nodes);
            // 
            // SplitPanel.Panel2
            // 
            this.SplitPanel.Panel2.BackColor = System.Drawing.Color.Transparent;
            this.SplitPanel.Panel2.Controls.Add(this.label1);
            this.SplitPanel.Panel2.Controls.Add(this.Inspector);
            this.SplitPanel.Size = new System.Drawing.Size(411, 357);
            this.SplitPanel.SplitterDistance = 218;
            this.SplitPanel.SplitterWidth = 1;
            this.SplitPanel.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(192, 18);
            this.label1.TabIndex = 1;
            this.label1.Text = "Property Inspector";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // InspectorWidget
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.SplitPanel);
            this.DoubleBuffered = true;
            this.Name = "InspectorWidget";
            this.Size = new System.Drawing.Size(411, 357);
            this.contextMenuStrip1.ResumeLayout(false);
            this.SplitPanel.Panel1.ResumeLayout(false);
            this.SplitPanel.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitPanel)).EndInit();
            this.SplitPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.PropertyGrid Inspector;
        public System.Windows.Forms.TreeView Nodes;
        public System.Windows.Forms.SplitContainer SplitPanel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem expandAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem collapseAllToolStripMenuItem;
    }
}
