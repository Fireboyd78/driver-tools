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
        public int UID { get; set; }
        public int Handle { get; set; }

        public Vector4 Unknown { get; set; }

        /// <summary>
        /// Gets or sets the Vertex Buffer to use when accessing vertices
        /// </summary>
        public VertexBuffer VertexBuffer { get; set; }
        
        // type of vertices in the buffer
        // resolves to a vertex declaration
        public short VertexType { get; set; }

        public int Unknown2 { get; set; }

        // something shadow related?
        public int Unknown3 { get; set; }

        public Vector4[] Transform { get; set; }    = new Vector4[8];
        public PartDefinition[] Parts { get; set; } = new PartDefinition[7];
    }
}
