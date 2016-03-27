using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

using FreeImageAPI;

namespace DSCript.Models
{
    public class MaterialData
    {
        public List<SubstanceData> Substances { get; set; }

        public bool Animated            { get; set; } = false;
        public double AnimationSpeed    { get; set; } = 25.0;
        
        public MaterialData()
        {
            Substances = new List<SubstanceData>();
        }
    }
}
