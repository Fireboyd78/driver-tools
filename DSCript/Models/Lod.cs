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

namespace DSCript.Models
{
    // TODO: Rename to 'Lod'
    public class Lod
    {
        public int ID { get; set; }

        public Model Parent { get; set; }
        public List<LodInstance> Instances { get; set; }

        public int Flags { get; set; }

        public int Mask { get; set; }
        
        public Lod(int id)
        {
            ID = id;
            Instances = new List<LodInstance>();
        }
    }
}
