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
    public class MeshGroup
    {
        public PartDefinition Parent { get; set; }

        public List<IndexedMesh> Meshes { get; set; }

        public MeshGroup(int nMeshes)
        {
            Meshes = new List<IndexedMesh>(nMeshes);
        }
    }
}
