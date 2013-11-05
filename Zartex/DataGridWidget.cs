using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Zartex
{
    public partial class DataGridWidget : UserControl
    {
        public DataGridWidget()
        {
            InitializeComponent();

            DoubleBuffered = true;
        }
    }
}