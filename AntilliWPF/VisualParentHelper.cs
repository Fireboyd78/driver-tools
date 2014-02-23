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

using HelixToolkit.Wpf;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    public class VisualParentHelper
    {
        public ModelVisual3D Visual { get; private set; }

        public ModelVisual3D Parent { get; private set; }

        public int Index { get; private set; }

        public void RestoreParent()
        {
            ModelVisual3D oldParent = (ModelVisual3D)VisualTreeHelper.GetParent(Visual);

            bool insert = Index <= Parent.Children.Count;

            oldParent.Children.Remove(Visual);

            if (insert)
                Parent.Children.Insert(Index, Visual);
            else
                Parent.Children.Add(Visual);
        }

        public VisualParentHelper(ModelVisual3D visual, ModelVisual3D newParent)
        {
            Visual = visual;

            Parent = (ModelVisual3D)VisualTreeHelper.GetParent(Visual);

            Index = Parent.Children.IndexOf(Visual);

            Parent.Children.Remove(Visual);
            newParent.Children.Add(Visual);
        }
    }
}
