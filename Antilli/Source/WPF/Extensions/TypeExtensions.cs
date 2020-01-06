using System;
using System.Reflection;

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
using System.Windows.Threading;

namespace Antilli
{
    public static class TypeExtensions
    {
        public static object GetValue(this PropertyInfo @this, object obj, BindingFlags flags)
        {
            return @this.GetValue(obj, flags, null, null, null);
        }

        public static T FindTemplatedParent<T>(this FrameworkElement @this)
            where T : FrameworkElement
        {
            var parent = @this.TemplatedParent as FrameworkElement;

            while(parent != null)
            {
                if (parent is T)
                    return parent as T;
                
                parent = parent.TemplatedParent as FrameworkElement;
            }

            return null;
        }
    }
}
