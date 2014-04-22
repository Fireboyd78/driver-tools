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
    public sealed class VisualParentHelper
    {
        private static List<VisualParentHelper> VisualParents;

        static VisualParentHelper()
        {
            VisualParents = new List<VisualParentHelper>();
        }

        public static bool ResetAllParents()
        {
            if (VisualParents.Count > 0)
            {
                foreach (VisualParentHelper k in VisualParents)
                    k.RestoreParent();

                VisualParents.Clear();
                
                return VisualParents.Count == 0;
            }
            return false;
        }

        public static void SetParent(ModelVisual3D visual, ModelVisual3D newParent)
        {
            if (VisualParents.Find((m) => m.Visual == visual) != null)
                ResetParent(visual);

            VisualParents.Add(new VisualParentHelper(visual, newParent));
        }

        public static bool ResetParent(ModelVisual3D visual)
        {
            VisualParentHelper helper = VisualParents.Find((k) => k.Visual == visual);

            if (helper != null)
            {
                helper.RestoreParent();
                return true;
            }
            else
            {
                return false;
            }
        }

        public int Index { get; private set; }
        
        public ModelVisual3D Parent { get; private set; }
        public ModelVisual3D Visual { get; private set; }

        public bool RestoreParent()
        {
            ModelVisual3D oldParent = (ModelVisual3D)VisualTreeHelper.GetParent(Visual);

            if (oldParent == null)
                return false;

            oldParent.Children.Remove(Visual);

            if (Index < Parent.Children.Count)
                Parent.Children.Insert(Index, Visual);
            else
                Parent.Children.Add(Visual);

            return true;
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
