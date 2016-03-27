namespace Zartex
{
    partial class Main
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.MenuBar = new System.Windows.Forms.MenuStrip();
            this.mmFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mmFile_Open = new System.Windows.Forms.ToolStripMenuItem();
            this.mmFile_Save = new System.Windows.Forms.ToolStripMenuItem();
            this.mmFile_Sep1 = new System.Windows.Forms.ToolStripSeparator();
            this.mmFile_Exit = new System.Windows.Forms.ToolStripMenuItem();
            this.mmView = new System.Windows.Forms.ToolStripMenuItem();
            this.mmView_MiSummary = new System.Windows.Forms.ToolStripMenuItem();
            this.tabNodes = new System.Windows.Forms.TabPage();
            this.tabContent = new System.Windows.Forms.TabControl();
            this.tabActors = new System.Windows.Forms.TabPage();
            this.tabWires = new System.Windows.Forms.TabPage();
            this.mmFile_Close = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuBar.SuspendLayout();
            this.tabContent.SuspendLayout();
            this.SuspendLayout();
            // 
            // MenuBar
            // 
            this.MenuBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mmFile,
            this.mmView});
            this.MenuBar.Location = new System.Drawing.Point(0, 0);
            this.MenuBar.Name = "MenuBar";
            this.MenuBar.Size = new System.Drawing.Size(601, 24);
            this.MenuBar.TabIndex = 1;
            this.MenuBar.Text = "menuStrip1";
            // 
            // mmFile
            // 
            this.mmFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mmFile_Open,
            this.mmFile_Save,
            this.mmFile_Close,
            this.mmFile_Sep1,
            this.mmFile_Exit});
            this.mmFile.Name = "mmFile";
            this.mmFile.Size = new System.Drawing.Size(37, 20);
            this.mmFile.Text = "File";
            // 
            // mmFile_Open
            // 
            this.mmFile_Open.Name = "mmFile_Open";
            this.mmFile_Open.Size = new System.Drawing.Size(152, 22);
            this.mmFile_Open.Text = "Open...";
            // 
            // mmFile_Save
            // 
            this.mmFile_Save.Enabled = false;
            this.mmFile_Save.Name = "mmFile_Save";
            this.mmFile_Save.Size = new System.Drawing.Size(152, 22);
            this.mmFile_Save.Tag = "$CanSave";
            this.mmFile_Save.Text = "Save";
            // 
            // mmFile_Sep1
            // 
            this.mmFile_Sep1.Name = "mmFile_Sep1";
            this.mmFile_Sep1.Size = new System.Drawing.Size(149, 6);
            // 
            // mmFile_Exit
            // 
            this.mmFile_Exit.Name = "mmFile_Exit";
            this.mmFile_Exit.Size = new System.Drawing.Size(152, 22);
            this.mmFile_Exit.Text = "Exit";
            // 
            // mmView
            // 
            this.mmView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mmView_MiSummary});
            this.mmView.Enabled = false;
            this.mmView.Name = "mmView";
            this.mmView.Size = new System.Drawing.Size(44, 20);
            this.mmView.Tag = "$ScriptLoaded";
            this.mmView.Text = "View";
            // 
            // mmView_MiSummary
            // 
            this.mmView_MiSummary.Enabled = false;
            this.mmView_MiSummary.Name = "mmView_MiSummary";
            this.mmView_MiSummary.Size = new System.Drawing.Size(169, 22);
            this.mmView_MiSummary.Tag = "$HasSummary";
            this.mmView_MiSummary.Text = "Mission Summary";
            // 
            // tabNodes
            // 
            this.tabNodes.Location = new System.Drawing.Point(4, 22);
            this.tabNodes.Name = "tabNodes";
            this.tabNodes.Size = new System.Drawing.Size(593, 436);
            this.tabNodes.TabIndex = 2;
            this.tabNodes.Text = "Logic Nodes";
            this.tabNodes.UseVisualStyleBackColor = true;
            // 
            // tabContent
            // 
            this.tabContent.Controls.Add(this.tabActors);
            this.tabContent.Controls.Add(this.tabNodes);
            this.tabContent.Controls.Add(this.tabWires);
            this.tabContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabContent.Location = new System.Drawing.Point(0, 24);
            this.tabContent.Name = "tabContent";
            this.tabContent.SelectedIndex = 0;
            this.tabContent.Size = new System.Drawing.Size(601, 462);
            this.tabContent.TabIndex = 2;
            this.tabContent.TabStop = false;
            // 
            // tabActors
            // 
            this.tabActors.Location = new System.Drawing.Point(4, 22);
            this.tabActors.Name = "tabActors";
            this.tabActors.Size = new System.Drawing.Size(593, 436);
            this.tabActors.TabIndex = 4;
            this.tabActors.Text = "Actors";
            this.tabActors.UseVisualStyleBackColor = true;
            // 
            // tabWires
            // 
            this.tabWires.Location = new System.Drawing.Point(4, 22);
            this.tabWires.Name = "tabWires";
            this.tabWires.Padding = new System.Windows.Forms.Padding(3);
            this.tabWires.Size = new System.Drawing.Size(593, 436);
            this.tabWires.TabIndex = 3;
            this.tabWires.Text = "Wire Collection";
            this.tabWires.UseVisualStyleBackColor = true;
            // 
            // mmFile_Close
            // 
            this.mmFile_Close.Enabled = false;
            this.mmFile_Close.Name = "mmFile_Close";
            this.mmFile_Close.Size = new System.Drawing.Size(152, 22);
            this.mmFile_Close.Tag = "$ScriptLoaded";
            this.mmFile_Close.Text = "Close";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(601, 486);
            this.Controls.Add(this.tabContent);
            this.Controls.Add(this.MenuBar);
            this.MainMenuStrip = this.MenuBar;
            this.Name = "Main";
            this.Text = "Zartex Mission Editor";
            this.MenuBar.ResumeLayout(false);
            this.MenuBar.PerformLayout();
            this.tabContent.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip MenuBar;
        private System.Windows.Forms.ToolStripMenuItem mmFile;
        private System.Windows.Forms.ToolStripMenuItem mmFile_Open;
        private System.Windows.Forms.ToolStripMenuItem mmFile_Save;
        private System.Windows.Forms.ToolStripSeparator mmFile_Sep1;
        private System.Windows.Forms.ToolStripMenuItem mmFile_Exit;
        private System.Windows.Forms.TabPage tabNodes;
        private System.Windows.Forms.TabControl tabContent;
        private System.Windows.Forms.TabPage tabActors;
        private System.Windows.Forms.TabPage tabWires;
        private System.Windows.Forms.ToolStripMenuItem mmView;
        private System.Windows.Forms.ToolStripMenuItem mmView_MiSummary;
        private System.Windows.Forms.ToolStripMenuItem mmFile_Close;
    }
}

