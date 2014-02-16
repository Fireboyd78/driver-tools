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
    public class PCMPSubMaterial
    {
        internal uint BaseOffset { get; set; }

        public uint Unk1 { get; set; }

        public ushort Unk2 { get; set; }
        public ushort Unk3 { get; set; }

        public List<PCMPTexture> Textures { get; set; }

        public PCMPSubMaterial()
        {
            Textures = new List<PCMPTexture>();
        }
    }
}
