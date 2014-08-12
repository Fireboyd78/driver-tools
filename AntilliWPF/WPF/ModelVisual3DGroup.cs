using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;

namespace Antilli
{
    public class ModelVisual3DGroup
    {
        public List<ModelVisual3D> Children { get; set; }

        public string Name { get; set; }

        public ModelVisual3DGroup()
        {
            Children = new List<ModelVisual3D>();
        }

        public ModelVisual3DGroup(string name)
            : this()
        {
            Name = name;
        }
    }
}
