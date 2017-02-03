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
    public class SubstanceData
    {
        public int Flags { get; set; }

        public int Mode { get; set; }
        public int Type { get; set; }

        public List<TextureData> Textures { get; set; }

        public virtual bool AlphaMask
        {
            get { return (Type == 0x400 || Type == 0x1000); }
        }

        public virtual bool Damage
        {
            get { return (Type == 0x800 || Type == 0x1000); }
        }

        public virtual bool Specular
        {
            get { return (Mode == 0x201 || Mode == 0x102); }
        }

        public virtual bool Emissive
        {
            get { return ((Flags & 0x18000) == 0x18000 || (Flags & 0x1E) == 0x1E); }
        }

        public virtual bool Transparency
        {
            get { return (((Flags & 0x1) == 0x1 || Flags == 0x4) && !Specular); }
        }

        public SubstanceData()
        {
            Textures = new List<TextureData>();
        }
    }
}
