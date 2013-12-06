namespace Antilli
{
    partial class ExportOBJ
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
            this.WPFExportHost = new System.Windows.Forms.Integration.ElementHost();
            this.ExportGUI = new Antilli.Assets.WPFExport();
            this.SuspendLayout();
            // 
            // WPFExportHost
            // 
            this.WPFExportHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WPFExportHost.Location = new System.Drawing.Point(0, 0);
            this.WPFExportHost.Margin = new System.Windows.Forms.Padding(0);
            this.WPFExportHost.Name = "WPFExportHost";
            this.WPFExportHost.Size = new System.Drawing.Size(350, 90);
            this.WPFExportHost.TabIndex = 0;
            this.WPFExportHost.Text = "elementHost1";
            this.WPFExportHost.Child = this.ExportGUI;
            // 
            // ExportOBJ
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(350, 90);
            this.Controls.Add(this.WPFExportHost);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ExportOBJ";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "ExportOBJ";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost WPFExportHost;
        private Assets.WPFExport ExportGUI;
    }
}