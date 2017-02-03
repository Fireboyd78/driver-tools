using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GMC2Snooper
{
    public partial class BMPViewer : Form
    {
        public Dictionary<int, Image> Images { get; set; }

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
            
            Images = new Dictionary<int, Image>();
            pictureBox1.BackgroundImageLayout = ImageLayout.Center;
        }

        public void AddImage(BitmapHelper helper)
        {
            AddImage(helper.Bitmap);
        }

        public void AddImage(Bitmap bitmap)
        {
            Bitmap bmap = new Bitmap(bitmap);

            Images.Add(__i, bmap);

            listBox1.Items.Add(__i);

            ++__i;
        }

        public void Init()
        {
            this.Show();

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
