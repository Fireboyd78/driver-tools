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
    public class PCMPData
    {
        public const uint Magic = 0x504D4350; // 'PCMP'

        public List<PCMPMaterial> Materials { get; set; }
        public List<PCMPSubMaterial> SubMaterials { get; set; }
        public List<PCMPTexture> Textures { get; set; }

        public PCMPData()
        {
            Materials    = new List<PCMPMaterial>();
            SubMaterials = new List<PCMPSubMaterial>();
            Textures     = new List<PCMPTexture>();
        }
    }
}
