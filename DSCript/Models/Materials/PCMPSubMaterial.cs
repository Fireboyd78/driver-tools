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
        public uint Flags { get; set; }

        public ushort Mode { get; set; }
        public ushort Type { get; set; }

        public List<PCMPTexture> Textures { get; set; }

        public bool AlphaMask
        {
            get { return (Type == 0x400 || Type == 0x1000); }
        }

        public bool Damage
        {
            get { return (Type == 0x800 || Type == 0x1000); }
        }

        public bool Specular
        {
            get { return (Mode == 0x201 || Mode == 0x102); }
        }

        public bool Emissive
        {
            get { return ((Flags & 0x18000) == 0x18000 || (Flags & 0x1E) == 0x1E); }
        }

        public bool Transparency
        {
            get { return (((Flags & 0x1) == 0x1 || Flags == 0x4) && !Specular); }
        }

        public PCMPSubMaterial()
        {
            Textures = new List<PCMPTexture>();
        }
    }
}
