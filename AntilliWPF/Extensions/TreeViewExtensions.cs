using System;
using System.Windows.Controls;
using System.Reflection;

namespace Antilli
{
    
    public static class TreeViewExtensions
    {
        public static TreeViewItem GetSelectedContainer(this TreeView @this)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var propInfo = typeof(TreeView).GetProperty("SelectedContainer", flags);

            if (propInfo == null)
                return null;

            return propInfo.GetValue(@this, flags) as TreeViewItem;
        }

        public static Panel GetItemsHost(this ItemsControl @this)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var propInfo = typeof(ItemsControl).GetProperty("ItemsHost", flags);

            if (propInfo == null)
                return null;

            return propInfo.GetValue(@this, flags) as Panel;
        }

        // ###################################################################################################
        // Based on original code by gauthampj
        // Source: http://www.experts-exchange.com/Programming/Microsoft_Development/Q_26529451.html#a34161941
        public static TreeViewItem ContainerFromItem(this TreeView treeView, object item)
        {
            TreeViewItem container;

            var generator = treeView.ItemContainerGenerator;

            return ((container = (TreeViewItem)generator.ContainerFromItem(item)) != null) ? container : ContainerFromItem(generator, treeView.Items, item);
        }

        private static TreeViewItem ContainerFromItem(ItemContainerGenerator parentItemContainerGenerator, ItemCollection itemCollection, object item)
        {
            TreeViewItem childContainer, containerFromItem, recursiveContainer;

            foreach (object childItem in itemCollection)
            {
                if ((childContainer = (TreeViewItem)parentItemContainerGenerator.ContainerFromItem(childItem)) != null)
                {
                    var childGenerator = childContainer.ItemContainerGenerator;

                    if ((containerFromItem = (TreeViewItem)childGenerator.ContainerFromItem(item)) != null)
                        return containerFromItem;
                    else if ((recursiveContainer = ContainerFromItem(childGenerator, childContainer.Items, item)) != null)
                        return recursiveContainer;
                }
            }
            return null;
        }
        // ###################################################################################################
    }
}
