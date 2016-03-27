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
            this.MenuPanel = new System.Windows.Forms.Panel();
            this.MenuBar = new System.Windows.Forms.MenuStrip();
            this.mnFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnFile_Open = new System.Windows.Forms.ToolStripMenuItem();
            this.mnLoadFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.mnMiami = new System.Windows.Forms.ToolStripMenuItem();
            this.mnNice = new System.Windows.Forms.ToolStripMenuItem();
            this.mnIstanbul = new System.Windows.Forms.ToolStripMenuItem();
            this.mnFile_Save = new System.Windows.Forms.ToolStripMenuItem();
            this.mnSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnFile_Exit = new System.Windows.Forms.ToolStripMenuItem();
            this.mnTools = new System.Windows.Forms.ToolStripMenuItem();
            this.mnTools_LoadLocale = new System.Windows.Forms.ToolStripMenuItem();
            this.Content = new System.Windows.Forms.Panel();
            this.LeftMenu = new System.Windows.Forms.Panel();
            this.lbl__EM = new System.Windows.Forms.Label();
            this.lblLELD = new System.Windows.Forms.Label();
            this.btnLECO = new System.Windows.Forms.Button();
            this.btnLEWC = new System.Windows.Forms.Button();
            this.btnLEAS = new System.Windows.Forms.Button();
            this.btnLENC = new System.Windows.Forms.Button();
            this.btnLEAC = new System.Windows.Forms.Button();
            this.btnLESB = new System.Windows.Forms.Button();
            this.btnEMMS = new System.Windows.Forms.Button();
            this.btnLESC = new System.Windows.Forms.Button();
            this.btnEMPR = new System.Windows.Forms.Button();
            this.btnEMOB = new System.Windows.Forms.Button();
            this.MenuPanel.SuspendLayout();
            this.MenuBar.SuspendLayout();
            this.LeftMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // MenuPanel
            // 
            this.MenuPanel.BackColor = System.Drawing.SystemColors.ControlDark;
            this.MenuPanel.Controls.Add(this.MenuBar);
            this.MenuPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.MenuPanel.Location = new System.Drawing.Point(0, 0);
            this.MenuPanel.Name = "MenuPanel";
            this.MenuPanel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.MenuPanel.Size = new System.Drawing.Size(944, 24);
            this.MenuPanel.TabIndex = 0;
            // 
            // MenuBar
            // 
            this.MenuBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MenuBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnFile,
            this.mnTools});
            this.MenuBar.Location = new System.Drawing.Point(0, 0);
            this.MenuBar.Name = "MenuBar";
            this.MenuBar.Size = new System.Drawing.Size(944, 23);
            this.MenuBar.TabIndex = 0;
            this.MenuBar.Text = "menuStrip1";
            // 
            // mnFile
            // 
            this.mnFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnFile_Open,
            this.mnFile_Save,
            this.mnSep1,
            this.mnFile_Exit});
            this.mnFile.Name = "mnFile";
            this.mnFile.Size = new System.Drawing.Size(37, 19);
            this.mnFile.Text = "File";
            // 
            // mnFile_Open
            // 
            this.mnFile_Open.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnLoadFile,
            this.mnSep2,
            this.mnMiami,
            this.mnNice,
            this.mnIstanbul});
            this.mnFile_Open.Name = "mnFile_Open";
            this.mnFile_Open.Size = new System.Drawing.Size(152, 22);
            this.mnFile_Open.Text = "Open";
            // 
            // mnLoadFile
            // 
            this.mnLoadFile.Name = "mnLoadFile";
            this.mnLoadFile.Size = new System.Drawing.Size(128, 22);
            this.mnLoadFile.Text = "Load file...";
            this.mnLoadFile.Click += new System.EventHandler(this.MenuLoadFile);
            // 
            // mnSep2
            // 
            this.mnSep2.Name = "mnSep2";
            this.mnSep2.Size = new System.Drawing.Size(125, 6);
            // 
            // mnMiami
            // 
            this.mnMiami.Name = "mnMiami";
            this.mnMiami.Size = new System.Drawing.Size(128, 22);
            this.mnMiami.Text = "Miami";
            // 
            // mnNice
            // 
            this.mnNice.Name = "mnNice";
            this.mnNice.Size = new System.Drawing.Size(128, 22);
            this.mnNice.Text = "Nice";
            // 
            // mnIstanbul
            // 
            this.mnIstanbul.Name = "mnIstanbul";
            this.mnIstanbul.Size = new System.Drawing.Size(128, 22);
            this.mnIstanbul.Text = "Istanbul";
            // 
            // mnFile_Save
            // 
            this.mnFile_Save.Enabled = false;
            this.mnFile_Save.Name = "mnFile_Save";
            this.mnFile_Save.Size = new System.Drawing.Size(152, 22);
            this.mnFile_Save.Text = "Save";
            this.mnFile_Save.Click += new System.EventHandler(this.MenuSaveFile);
            // 
            // mnSep1
            // 
            this.mnSep1.Name = "mnSep1";
            this.mnSep1.Size = new System.Drawing.Size(149, 6);
            // 
            // mnFile_Exit
            // 
            this.mnFile_Exit.Name = "mnFile_Exit";
            this.mnFile_Exit.Size = new System.Drawing.Size(152, 22);
            this.mnFile_Exit.Text = "Exit";
            // 
            // mnTools
            // 
            this.mnTools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnTools_LoadLocale});
            this.mnTools.Enabled = false;
            this.mnTools.Name = "mnTools";
            this.mnTools.Size = new System.Drawing.Size(47, 19);
            this.mnTools.Text = "Tools";
            // 
            // mnTools_LoadLocale
            // 
            this.mnTools_LoadLocale.Name = "mnTools_LoadLocale";
            this.mnTools_LoadLocale.Size = new System.Drawing.Size(143, 22);
            this.mnTools_LoadLocale.Text = "Load locale...";
            this.mnTools_LoadLocale.Click += new System.EventHandler(this.LoadLocaleTool);
            // 
            // Content
            // 
            this.Content.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Content.AutoSize = true;
            this.Content.BackColor = System.Drawing.Color.Gray;
            this.Content.Location = new System.Drawing.Point(157, 24);
            this.Content.Margin = new System.Windows.Forms.Padding(0);
            this.Content.Name = "Content";
            this.Content.Size = new System.Drawing.Size(787, 688);
            this.Content.TabIndex = 1;
            // 
            // LeftMenu
            // 
            this.LeftMenu.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.LeftMenu.Controls.Add(this.lbl__EM);
            this.LeftMenu.Controls.Add(this.lblLELD);
            this.LeftMenu.Controls.Add(this.btnLECO);
            this.LeftMenu.Controls.Add(this.btnLEWC);
            this.LeftMenu.Controls.Add(this.btnLEAS);
            this.LeftMenu.Controls.Add(this.btnLENC);
            this.LeftMenu.Controls.Add(this.btnLEAC);
            this.LeftMenu.Controls.Add(this.btnLESB);
            this.LeftMenu.Controls.Add(this.btnEMMS);
            this.LeftMenu.Controls.Add(this.btnLESC);
            this.LeftMenu.Controls.Add(this.btnEMPR);
            this.LeftMenu.Controls.Add(this.btnEMOB);
            this.LeftMenu.Dock = System.Windows.Forms.DockStyle.Left;
            this.LeftMenu.Location = new System.Drawing.Point(0, 24);
            this.LeftMenu.Margin = new System.Windows.Forms.Padding(0, 0, 1, 0);
            this.LeftMenu.Name = "LeftMenu";
            this.LeftMenu.Size = new System.Drawing.Size(156, 688);
            this.LeftMenu.TabIndex = 2;
            // 
            // lbl__EM
            // 
            this.lbl__EM.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl__EM.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.lbl__EM.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.25F);
            this.lbl__EM.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lbl__EM.Location = new System.Drawing.Point(3, 3);
            this.lbl__EM.Margin = new System.Windows.Forms.Padding(3);
            this.lbl__EM.Name = "lbl__EM";
            this.lbl__EM.Size = new System.Drawing.Size(150, 20);
            this.lbl__EM.TabIndex = 12;
            this.lbl__EM.Text = "Mission Data";
            this.lbl__EM.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLELD
            // 
            this.lblLELD.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLELD.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.lblLELD.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.25F);
            this.lblLELD.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lblLELD.Location = new System.Drawing.Point(3, 64);
            this.lblLELD.Margin = new System.Windows.Forms.Padding(3);
            this.lblLELD.Name = "lblLELD";
            this.lblLELD.Size = new System.Drawing.Size(150, 20);
            this.lblLELD.TabIndex = 11;
            this.lblLELD.Text = "Mission Logic";
            this.lblLELD.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnLECO
            // 
            this.btnLECO.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLECO.Enabled = false;
            this.btnLECO.Location = new System.Drawing.Point(3, 249);
            this.btnLECO.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.btnLECO.Name = "btnLECO";
            this.btnLECO.Size = new System.Drawing.Size(150, 25);
            this.btnLECO.TabIndex = 10;
            this.btnLECO.Text = "Script Counters";
            this.btnLECO.UseVisualStyleBackColor = true;
            // 
            // btnLEWC
            // 
            this.btnLEWC.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLEWC.Location = new System.Drawing.Point(3, 222);
            this.btnLEWC.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.btnLEWC.Name = "btnLEWC";
            this.btnLEWC.Size = new System.Drawing.Size(150, 25);
            this.btnLEWC.TabIndex = 9;
            this.btnLEWC.Text = "Wire Collection";
            this.btnLEWC.UseVisualStyleBackColor = true;
            // 
            // btnLEAS
            // 
            this.btnLEAS.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLEAS.Location = new System.Drawing.Point(3, 195);
            this.btnLEAS.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.btnLEAS.Name = "btnLEAS";
            this.btnLEAS.Size = new System.Drawing.Size(150, 25);
            this.btnLEAS.TabIndex = 8;
            this.btnLEAS.Text = "Actor Set Table";
            this.btnLEAS.UseVisualStyleBackColor = true;
            // 
            // btnLENC
            // 
            this.btnLENC.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLENC.Location = new System.Drawing.Point(3, 168);
            this.btnLENC.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.btnLENC.Name = "btnLENC";
            this.btnLENC.Size = new System.Drawing.Size(150, 25);
            this.btnLENC.TabIndex = 7;
            this.btnLENC.Text = "Logic Nodes";
            this.btnLENC.UseVisualStyleBackColor = true;
            // 
            // btnLEAC
            // 
            this.btnLEAC.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLEAC.Location = new System.Drawing.Point(3, 141);
            this.btnLEAC.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.btnLEAC.Name = "btnLEAC";
            this.btnLEAC.Size = new System.Drawing.Size(150, 25);
            this.btnLEAC.TabIndex = 6;
            this.btnLEAC.Text = "Actors";
            this.btnLEAC.UseVisualStyleBackColor = true;
            // 
            // btnLESB
            // 
            this.btnLESB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLESB.Enabled = false;
            this.btnLESB.Location = new System.Drawing.Point(3, 114);
            this.btnLESB.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.btnLESB.Name = "btnLESB";
            this.btnLESB.Size = new System.Drawing.Size(150, 25);
            this.btnLESB.TabIndex = 5;
            this.btnLESB.Text = "Sound Bank Table";
            this.btnLESB.UseVisualStyleBackColor = true;
            // 
            // btnEMMS
            // 
            this.btnEMMS.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEMMS.Enabled = false;
            this.btnEMMS.Location = new System.Drawing.Point(3, 645);
            this.btnEMMS.Name = "btnEMMS";
            this.btnEMMS.Size = new System.Drawing.Size(150, 40);
            this.btnEMMS.TabIndex = 4;
            this.btnEMMS.Text = "Mission Summary";
            this.btnEMMS.UseVisualStyleBackColor = true;
            // 
            // btnLESC
            // 
            this.btnLESC.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLESC.Location = new System.Drawing.Point(3, 87);
            this.btnLESC.Margin = new System.Windows.Forms.Padding(3, 3, 3, 1);
            this.btnLESC.Name = "btnLESC";
            this.btnLESC.Size = new System.Drawing.Size(150, 25);
            this.btnLESC.TabIndex = 3;
            this.btnLESC.Text = "String Collection";
            this.btnLESC.UseVisualStyleBackColor = true;
            // 
            // btnEMPR
            // 
            this.btnEMPR.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEMPR.Enabled = false;
            this.btnEMPR.Location = new System.Drawing.Point(78, 26);
            this.btnEMPR.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.btnEMPR.Name = "btnEMPR";
            this.btnEMPR.Size = new System.Drawing.Size(75, 35);
            this.btnEMPR.TabIndex = 2;
            this.btnEMPR.Text = "Prop Handles";
            this.btnEMPR.UseVisualStyleBackColor = true;
            // 
            // btnEMOB
            // 
            this.btnEMOB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEMOB.Location = new System.Drawing.Point(3, 26);
            this.btnEMOB.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.btnEMOB.Name = "btnEMOB";
            this.btnEMOB.Size = new System.Drawing.Size(75, 35);
            this.btnEMOB.TabIndex = 1;
            this.btnEMOB.Text = "Objects";
            this.btnEMOB.UseVisualStyleBackColor = true;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(944, 712);
            this.Controls.Add(this.LeftMenu);
            this.Controls.Add(this.Content);
            this.Controls.Add(this.MenuPanel);
            this.DoubleBuffered = true;
            this.MainMenuStrip = this.MenuBar;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(600, 405);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Zartex Mission Editor";
            this.MenuPanel.ResumeLayout(false);
            this.MenuPanel.PerformLayout();
            this.MenuBar.ResumeLayout(false);
            this.MenuBar.PerformLayout();
            this.LeftMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel MenuPanel;
        private System.Windows.Forms.MenuStrip MenuBar;
        private System.Windows.Forms.ToolStripMenuItem mnFile;
        private System.Windows.Forms.ToolStripMenuItem mnFile_Open;
        private System.Windows.Forms.ToolStripMenuItem mnFile_Save;
        private System.Windows.Forms.ToolStripSeparator mnSep1;
        private System.Windows.Forms.ToolStripMenuItem mnFile_Exit;
        private System.Windows.Forms.Panel LeftMenu;
        private System.Windows.Forms.Button btnEMOB;
        private System.Windows.Forms.Label lblLELD;
        private System.Windows.Forms.Button btnLECO;
        private System.Windows.Forms.Button btnLEWC;
        private System.Windows.Forms.Button btnLEAS;
        private System.Windows.Forms.Button btnLENC;
        private System.Windows.Forms.Button btnLEAC;
        private System.Windows.Forms.Button btnLESB;
        private System.Windows.Forms.Button btnEMMS;
        private System.Windows.Forms.Button btnLESC;
        private System.Windows.Forms.Button btnEMPR;
        public System.Windows.Forms.Panel Content;
        private System.Windows.Forms.Label lbl__EM;
        private System.Windows.Forms.ToolStripMenuItem mnLoadFile;
        private System.Windows.Forms.ToolStripSeparator mnSep2;
        private System.Windows.Forms.ToolStripMenuItem mnMiami;
        private System.Windows.Forms.ToolStripMenuItem mnNice;
        private System.Windows.Forms.ToolStripMenuItem mnIstanbul;
        private System.Windows.Forms.ToolStripMenuItem mnTools;
        private System.Windows.Forms.ToolStripMenuItem mnTools_LoadLocale;

    }
}

