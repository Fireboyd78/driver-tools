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

namespace DSCript.Models
{
    public interface ITextureData
    {
        int UID { get; set; }
        int Handle { get; set; }
        
        int Type { get; set; }
        int Flags { get; set; }

        int Width { get; set; }
        int Height { get; set; }

        byte[] Buffer { get; set; }
    }

    public sealed class TextureDataPC : ITextureData
    {
        public int UID { get; set; }
        public int Handle { get; set; }

        public int Type { get; set; }
        
        public int Width { get; set; }
        public int Height { get; set; }
        
        public int Flags { get; set; }
        
        public byte[] Buffer { get; set; }
    }
}
