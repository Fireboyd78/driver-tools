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

using Antilli.Models;

namespace Antilli
{
    public class ModelGroupListItem
    {
        public int ID { get; private set; }

        public Model3D Content { get; private set; }

        public override string ToString()
        {
            return String.Format("Model {0}", ID);
        }

        public ModelGroupListItem(int id, Model3D models)
        {
            ID = id;
            Content = models;
        }
    }
}
