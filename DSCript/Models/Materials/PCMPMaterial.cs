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
        uint? unk1, unk2, unk3, unk4;

        public uint Reserved1
        {
            get { return unk1 ?? 0; }
            set { unk1 = value; }
        }

        public uint Reserved2
        {
            get { return unk2 ?? 0x41C80000; }
            set { unk2 = value; }
        }

        public uint Reserved3
        {
            get { return unk3 ?? 0; }
            set { unk3 = value; }
        }

        public uint Reserved4
        {
            get { return unk4 ?? 0; }
            set { unk4 = value; }
        }

        public List<PCMPSubMaterial> SubMaterials { get; set; }

        public PCMPMaterial()
        {
            SubMaterials = new List<PCMPSubMaterial>();
        }
    }
}
