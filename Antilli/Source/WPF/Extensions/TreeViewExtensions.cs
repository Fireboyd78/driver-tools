using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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

        public static TreeView GetParentTreeView(this TreeViewItem @this)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var propInfo = typeof(TreeViewItem).GetProperty("ParentTreeView", flags);

            if (propInfo == null)
                return null;

            return propInfo.GetValue(@this, flags) as TreeView;
        }

        // ###################################################################################################
        // Based on original code by gauthampj
        // Source: http://www.experts-exchange.com/Programming/Microsoft_Development/Q_26529451.html#a34161941
        public static TreeViewItem ContainerFromItem(this TreeView treeView, object item)
        {
            var generator = treeView.ItemContainerGenerator;
            var container = (TreeViewItem)generator.ContainerFromItem(item) ?? ContainerFromItem(generator, treeView.Items, item);

            return container;
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
        
        public static TItemsControl ContainerFromItem<TItemsControl>(this ItemContainerGenerator itemGenerator, object item)
            where TItemsControl : ItemsControl
        {
            return itemGenerator.ContainerFromItem(item) as TItemsControl;
        }

        public static TItemsControl ContainerFromItem<TItemsControl>(this ItemContainerGenerator itemGenerator, ItemsControl itemsHost, object item)
            where TItemsControl : ItemsControl
        {
            var container = itemGenerator.ContainerFromItem<TItemsControl>(item) 
                ?? itemGenerator.ContainerFromItem<TItemsControl>(itemsHost.Items, item);

            return container;
        }

        public static TItemsControl ContainerFromItem<TItemsControl>(this ItemContainerGenerator itemGenerator, ItemCollection itemCollection, object item)
            where TItemsControl : ItemsControl
        {
            foreach (var childItem in itemCollection)
            {
                var childContainer = itemGenerator.ContainerFromItem<TItemsControl>(childItem);

                if (childContainer != null)
                {
                    var container = childContainer.ContainerFromItem<TItemsControl>(item);

                    if (container != null)
                        return container;
                }
            }

            // damn
            return null;
        }

        public static TItemsControl ContainerFromItem<TItemsControl>(this ItemsControl itemsHost, object item)
            where TItemsControl : ItemsControl
        {
            var generator = itemsHost.ItemContainerGenerator;

            return generator.ContainerFromItem<TItemsControl>(itemsHost, item);
        }

        public static IEnumerable<TItemsControl> GetItemContainers<TItemsControl>(this ItemsControl itemsHost, ItemContainerGenerator itemGenerator)
            where TItemsControl : ItemsControl
        {
            foreach (var item in itemsHost.Items)
            {
                var container = itemGenerator.ContainerFromItem<TItemsControl>(item);

                if (container != null)
                    yield return container;
            }
        }

        public static IEnumerable<TItemsControl> GetItemContainers<TItemsControl>(this ItemsControl itemsHost)
            where TItemsControl : ItemsControl
        {
            return itemsHost.GetItemContainers<TItemsControl>(itemsHost.ItemContainerGenerator);
        }

        private static MethodInfo _BringIndexIntoView = null;

        public static void BringIndexIntoView(this VirtualizingPanel vPanel, int index)
        {
            if (_BringIndexIntoView == null)
                _BringIndexIntoView = typeof(VirtualizingPanel).GetMethod(
                    "BringIndexIntoView",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(int) },
                    null);

            _BringIndexIntoView.Invoke(vPanel, new object[] { index });
        }

        public static void CollapseSubtree(this TreeViewItem treeItem)
        {
            if (treeItem.IsExpanded)
            {
                treeItem.IsExpanded = false;

                var generator = treeItem.ItemContainerGenerator;
                var numItems = treeItem.Items.Count;

                for (int i = 0; i < numItems; i++)
                {
                    var container = generator.ContainerFromIndex(i) as TreeViewItem;

                    if (container.IsExpanded)
                        container.CollapseSubtree();
                }
            }
        }
        
        public static void ExpandAll(this TreeView treeView, bool expand = true)
        {
            var generator = treeView.ItemContainerGenerator;
            var numItems = treeView.Items.Count;

            for (int i = 0; i < numItems; i++)
            {
                var treeItem = generator.ContainerFromIndex(i) as TreeViewItem;

                if (expand)
                {
                    treeItem.ExpandSubtree();
                }
                else
                {
                    treeItem.CollapseSubtree();
                }
            }
            
            treeView.UpdateLayout();
        }
    }
}
