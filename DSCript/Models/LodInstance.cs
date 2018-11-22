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
    public class LodInstance
    {
        public Lod Parent { get; set; }

        public List<SubModel> SubModels { get; set; }

        public Vector4[] Transform { get; set; }

        public bool UseTransform { get; set; }

        // likely unused, but I'm tired of chasing after bugs
        public int Reserved { get; set; }

        public LodInstance()
        {
            SubModels = new List<SubModel>();
            Transform = new Vector4[4];
        }
    }
}
