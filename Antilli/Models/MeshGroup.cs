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

namespace Antilli.Models
{
    public class MeshGroup
    {
        public uint Offset { get; private set; }

        public List<IndexedPrimitive> Meshes { get; set; }

        public MeshGroup(uint meshesOffset, int count)
        {
            Offset = meshesOffset;
            Meshes = new List<IndexedPrimitive>(count);
        }
    }
}
