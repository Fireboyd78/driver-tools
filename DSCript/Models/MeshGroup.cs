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

        public Vector4[] Transform { get; set; }

        public bool UseTransform { get; set; }

        // likely unused, but I'm tired of chasing after bugs
        public int Reserved { get; set; }

        public MeshGroup()
        {
            Meshes = new List<MeshDefinition>();

            Transform = new Vector4[4];
        }
    }
}
