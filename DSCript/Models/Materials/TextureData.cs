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
    public interface ITextureData
    {
        int UID { get; set; }
        
        int Type { get; set; }
        int Flags { get; set; }

        int Width { get; set; }
        int Height { get; set; }

        byte[] Buffer { get; set; }
    }

    public sealed class TextureDataPC : ITextureData
    {
        int ITextureData.UID
        {
            get { return CRC32; }
            set { CRC32 = value; }
        }

        int ITextureData.Flags
        {
            get { return Reserved; }
            set { Reserved = value; }
        }
        
        public int Reserved { get; set; }
        public int CRC32 { get; set; }

        public int Type { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public int Unknown { get; set; }

        public byte[] Buffer { get; set; }
    }
}
