using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GMC2Snooper
{
    public partial class BMPViewer : Form
    {
        public Dictionary<int, Bitmap> Images { get; set; }

        private int __i = 0;

        public BMPViewer(BitmapHelper bitmapHelper) : this(bitmapHelper.Bitmap) {}
        public BMPViewer(Bitmap bitmap) : this()
        {
            AddImage(bitmap);
            listBox1.SelectedIndex = 0;
        }

        public BMPViewer()
        {
            InitializeComponent();
            
            Images = new Dictionary<int, Bitmap>();
            pictureBox1.BackgroundImageLayout = ImageLayout.Center;

            listBox1.Focus();
            listBox1.KeyDown += (o, e) => {
                switch (e.KeyCode)
                {
                case Keys.F9:
                    {
                        var outDir = Path.Combine(Environment.CurrentDirectory, "textures");

                        if (!Directory.Exists(outDir))
                            Directory.CreateDirectory(outDir);

                        foreach (var image in Images)
                        {
                            var bmap = image.Value;
                            
                            var pixels = bmap.ToByteArray(PixelFormat.Format8bppIndexed);
                            var name = $"{Memory.GetCRC32(pixels):X8}.bmp";

                            bmap.Save(Path.Combine(outDir, name), ImageFormat.Bmp);
                        }

                        MessageBox.Show($"Textures dumped to {outDir}.", "GMC2Snooper", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    } break;
                }
            };
        }

        public void AddImageByName(BitmapHelper helper, string name)
        {
            var bmap = new Bitmap(helper.Bitmap);
            Images.Add(__i, bmap);

            listBox1.Items.Add(name);
            ++__i;
        }

        public void AddImage(BitmapHelper helper)
        {
            AddImage(helper.Bitmap);
        }

        public void AddImage(Bitmap bitmap)
        {
            var bmap = new Bitmap(bitmap);

            Images.Add(__i, bmap);

            listBox1.Items.Add(__i);

            ++__i;
        }

        public bool HasImages
        {
            get { return __i > 0; }
        }

        public void Init()
        {
            this.Show();

            if (listBox1.Items.Count > 0)
                listBox1.SelectedIndex = 0;
        }

        private void BMPViewer_Load(object sender, EventArgs e)
        {
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = Images[listBox1.SelectedIndex];
        }
    }
}
