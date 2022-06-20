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
            this.Dialog = new System.Windows.Forms.SplitContainer();
            this.Header = new System.Windows.Forms.Label();
            this.Properties = new System.Windows.Forms.FlowLayoutPanel();
            this.nodeInDisable = new System.Windows.Forms.PictureBox();
            this.nodeInEnable = new System.Windows.Forms.PictureBox();
            this.nodeOutSuccess = new System.Windows.Forms.PictureBox();
            this.nodeOutFailure = new System.Windows.Forms.PictureBox();
            this.nodeOutCondition = new System.Windows.Forms.PictureBox();
            this.Comment = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.Dialog)).BeginInit();
            this.Dialog.Panel1.SuspendLayout();
            this.Dialog.Panel2.SuspendLayout();
            this.Dialog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nodeInDisable)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeInEnable)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeOutSuccess)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeOutFailure)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeOutCondition)).BeginInit();
            this.SuspendLayout();
            // 
            // Dialog
            // 
            this.Dialog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Dialog.BackColor = System.Drawing.Color.Black;
            this.Dialog.Location = new System.Drawing.Point(12, 15);
            this.Dialog.Margin = new System.Windows.Forms.Padding(0);
            this.Dialog.Name = "Dialog";
            this.Dialog.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // Dialog.Panel1
            // 
            this.Dialog.Panel1.BackColor = System.Drawing.Color.Transparent;
            this.Dialog.Panel1.Controls.Add(this.Header);
            this.Dialog.Panel1.Padding = new System.Windows.Forms.Padding(1, 1, 1, 0);
            // 
            // Dialog.Panel2
            // 
            this.Dialog.Panel2.BackColor = System.Drawing.Color.Transparent;
            this.Dialog.Panel2.Controls.Add(this.Properties);
            this.Dialog.Panel2.Padding = new System.Windows.Forms.Padding(1);
            this.Dialog.Size = new System.Drawing.Size(170, 150);
            this.Dialog.SplitterDistance = 25;
            this.Dialog.SplitterWidth = 1;
            this.Dialog.TabIndex = 0;
            // 
            // Header
            // 
            this.Header.BackColor = System.Drawing.SystemColors.ControlDark;
            this.Header.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Header.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.75F);
            this.Header.Location = new System.Drawing.Point(1, 1);
            this.Header.Margin = new System.Windows.Forms.Padding(0);
            this.Header.Name = "Header";
            this.Header.Size = new System.Drawing.Size(168, 24);
            this.Header.TabIndex = 0;
            this.Header.Text = "Logic Node";
            this.Header.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Properties
            // 
            this.Properties.BackColor = System.Drawing.SystemColors.ControlLight;
            this.Properties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Properties.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.25F);
            this.Properties.Location = new System.Drawing.Point(1, 1);
            this.Properties.Name = "Properties";
            this.Properties.Padding = new System.Windows.Forms.Padding(2);
            this.Properties.Size = new System.Drawing.Size(168, 122);
            this.Properties.TabIndex = 0;
            // 
            // nodeInDisable
            // 
            this.nodeInDisable.BackColor = System.Drawing.Color.Thistle;
            this.nodeInDisable.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.nodeInDisable.Location = new System.Drawing.Point(0, 30);
            this.nodeInDisable.Margin = new System.Windows.Forms.Padding(0);
            this.nodeInDisable.Name = "nodeInDisable";
            this.nodeInDisable.Size = new System.Drawing.Size(14, 14);
            this.nodeInDisable.TabIndex = 2;
            this.nodeInDisable.TabStop = false;
            // 
            // nodeInEnable
            // 
            this.nodeInEnable.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.nodeInEnable.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.nodeInEnable.Location = new System.Drawing.Point(0, 16);
            this.nodeInEnable.Margin = new System.Windows.Forms.Padding(0);
            this.nodeInEnable.Name = "nodeInEnable";
            this.nodeInEnable.Size = new System.Drawing.Size(14, 14);
            this.nodeInEnable.TabIndex = 3;
            this.nodeInEnable.TabStop = false;
            // 
            // nodeOutSuccess
            // 
            this.nodeOutSuccess.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nodeOutSuccess.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.nodeOutSuccess.Location = new System.Drawing.Point(182, 48);
            this.nodeOutSuccess.Margin = new System.Windows.Forms.Padding(0);
            this.nodeOutSuccess.Name = "nodeOutSuccess";
            this.nodeOutSuccess.Size = new System.Drawing.Size(18, 18);
            this.nodeOutSuccess.TabIndex = 4;
            this.nodeOutSuccess.TabStop = false;
            // 
            // nodeOutFailure
            // 
            this.nodeOutFailure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nodeOutFailure.BackColor = System.Drawing.Color.Thistle;
            this.nodeOutFailure.Location = new System.Drawing.Point(182, 96);
            this.nodeOutFailure.Margin = new System.Windows.Forms.Padding(0);
            this.nodeOutFailure.Name = "nodeOutFailure";
            this.nodeOutFailure.Size = new System.Drawing.Size(18, 18);
            this.nodeOutFailure.TabIndex = 5;
            this.nodeOutFailure.TabStop = false;
            // 
            // nodeOutCondition
            // 
            this.nodeOutCondition.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nodeOutCondition.BackColor = System.Drawing.Color.CornflowerBlue;
            this.nodeOutCondition.Location = new System.Drawing.Point(182, 72);
            this.nodeOutCondition.Margin = new System.Windows.Forms.Padding(0);
            this.nodeOutCondition.Name = "nodeOutCondition";
            this.nodeOutCondition.Size = new System.Drawing.Size(18, 18);
            this.nodeOutCondition.TabIndex = 1;
            this.nodeOutCondition.TabStop = false;
            // 
            // Comment
            // 
            this.Comment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Comment.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Comment.ForeColor = System.Drawing.SystemColors.Window;
            this.Comment.Location = new System.Drawing.Point(12, 0);
            this.Comment.Margin = new System.Windows.Forms.Padding(0);
            this.Comment.Name = "Comment";
            this.Comment.Size = new System.Drawing.Size(170, 15);
            this.Comment.TabIndex = 6;
            this.Comment.Text = "label1";
            this.Comment.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // NodeWidget
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.Comment);
            this.Controls.Add(this.nodeOutFailure);
            this.Controls.Add(this.nodeOutSuccess);
            this.Controls.Add(this.nodeInEnable);
            this.Controls.Add(this.nodeInDisable);
            this.Controls.Add(this.nodeOutCondition);
            this.Controls.Add(this.Dialog);
            this.DoubleBuffered = true;
            this.Name = "NodeWidget";
            this.Size = new System.Drawing.Size(200, 165);
            this.Load += new System.EventHandler(this.NodeWidget_Load);
            this.DoubleClick += new System.EventHandler(this.NodeWidget_DoubleClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.NodeWidget_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.NodeWidget_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.NodeWidget_MouseUp);
            this.Dialog.Panel1.ResumeLayout(false);
            this.Dialog.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Dialog)).EndInit();
            this.Dialog.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nodeInDisable)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeInEnable)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeOutSuccess)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeOutFailure)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nodeOutCondition)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer Dialog;
        public System.Windows.Forms.PictureBox nodeInDisable;
        public System.Windows.Forms.FlowLayoutPanel Properties;
        public System.Windows.Forms.PictureBox nodeInEnable;
        public System.Windows.Forms.PictureBox nodeOutSuccess;
        public System.Windows.Forms.PictureBox nodeOutFailure;
        public System.Windows.Forms.Label Header;
        public System.Windows.Forms.PictureBox nodeOutCondition;
        private System.Windows.Forms.Label Comment;
    }
}
