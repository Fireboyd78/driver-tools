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
    public class PartDefinition
    {
        public int ID { get; set; }

        public uint Unknown { get; set; }
        public uint Reserved { get; set; }

        public PartsGroup Parent { get; set; }
        public MeshGroup Group { get; set; }

        public PartDefinition(int id)
        {
            ID = id;
        }
    }
}
