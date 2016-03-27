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
    public class TextureData : CacheableTexture
    {
        public int Reserved { get; set; }
        public int CRC32 { get; set; }
        public int Type { get; set; }

        public int Unknown { get; set; }

        public void ExportFile(string filename)
        {
            string dir = Path.GetDirectoryName(filename);

            using (MemoryStream f = new MemoryStream(Buffer))
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                if (!File.Exists(filename))
                    f.WriteTo(File.Create(filename, (int)f.Length));
            }
        }
    }
}
