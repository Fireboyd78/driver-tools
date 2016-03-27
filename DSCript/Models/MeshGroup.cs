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
using System.Windows.Media.Media3D;

namespace DSCript.Models
{
    public class MeshGroup
    {
        public PartDefinition Parent { get; set; }

        public List<MeshDefinition> Meshes { get; set; }

        public Point4D[] Transform { get; set; }
        public Point4D Unknown { get; set; }

        public MeshGroup()
        {
            Meshes = new List<MeshDefinition>();

            Transform = new Point4D[3];
            Unknown = new Point4D();
        }
    }
}
