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
    public class PCMPMaterial
    {
        public List<PCMPSubMaterial> SubMaterials { get; set; }

        public PCMPMaterial()
        {
            SubMaterials = new List<PCMPSubMaterial>();
        }
    }
}
