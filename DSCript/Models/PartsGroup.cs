using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DSCript.Models
{
    public class PartsGroup
    {
        public uint UID { get; set; }
        public uint Handle { get; set; }

        /// <summary>
        /// Gets or sets the Vertex Buffer to use when accessing vertices
        /// </summary>
        public VertexData VertexBuffer { get; set; }
        
        public short Unknown1 { get; set; }
        public int Unknown2 { get; set; }

        public List<Point4D> Transform { get; set; }
        public List<PartDefinition> Parts { get; set; }

        public PartsGroup()
        {
            Parts = new List<PartDefinition>(7);
            Transform = new List<Point4D>(8);
        }
    }
}
