using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using DSCript;
using DSCript.IO;

namespace DVEdit
{
    using XCTK = Xceed.Wpf.Toolkit;

    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        public class NodeTag
        {
            public SubChunkBlock BaseChunk { get; set; }

            public NodeTag(SubChunkBlock chunk)
            {
                BaseChunk = chunk;
            }
        }

        public Microsoft.Win32.OpenFileDialog openFile = new Microsoft.Win32.OpenFileDialog
        {
            //Title = "Select a DRIV3R Vehicles package",
            //Filter = "DRIV3R Vehicles (*.vvs;*.vvv)|*.vvs;*.vvv"
            Title = "Select a CHUNK File",
            Filter = "Reflections CHUNK File|*.*"
        };

        ChunkReader ChunkFile;

        public Main()
        {
            InitializeComponent();

            OpenFileDialogClick();
        }

        public TreeViewItem GenerateNode(SubChunkBlock chunk)
        {
            string txt = ChunkFile.Magic2Str(chunk.Magic);

            return new TreeViewItem{
                Header = txt,
                Tag = new NodeTag(chunk)
            };
        }

        private int __i = 1; // HACK: num of chunks parsed to save performance!!

        public void RecurseHierarchy(int s, int n, TreeViewItem theNode)
        {
            for (int ss = __i; ss < ChunkFile.Chunk.Count; ss++)
            {
                //DSC.Log("{0} :: {1}", ss, __i);
                if (ChunkFile.Chunk[ss].Parent.Parent.ID == s && ChunkFile.Chunk[ss].Parent.ID == n)
                {
                    for (int nn = 0; nn < ChunkFile.Chunk[ss].Subs.Count; nn++)
                    {
                        TreeViewItem nd = GenerateNode(ChunkFile.Chunk[ss].Subs[nn]);
                        
                        theNode.Items.Add(nd);
            
                        RecurseHierarchy(ss, nn, nd);
                    }
                    ++__i;
                    break;
                }
            }
        }

        public void FillTreeView(TreeViewItem theTV)
        {
            for (int n = 0; n < ChunkFile.Chunk[0].SubCount; n++)
            {
                TreeViewItem nd = GenerateNode(ChunkFile.Chunk[0].Subs[n]);

                theTV.Items.Add(nd);

                RecurseHierarchy(0, n, nd);

                NodeTag bd = (NodeTag)nd.Tag;
            }
        }

        public void LoadFile(string filename)
        {
            if (filename.EndsWith("vvs") || filename.EndsWith("vvv"))
                ChunkFile = new ChunkReader(filename);
            else
                MessageBox.Show("You can't open this kind of file!"); 
        }

        private void OpenFileDialogClick()
        {
            Nullable<bool> Result = openFile.ShowDialog();

            if (Result == true)
            {
                // LoadFile(openFile.FileName);
                ChunkFile = new ChunkReader(openFile.FileName);

                if (ChunkFile.IsChunk)
                {
                    if (cNodes.HasItems)
                    {
                        DSC.Log("Clearing old nodes...");
                        cNodes.Items.Clear();
                        cNodes.UpdateLayout();
                    }

                    TreeViewItem ANode = new TreeViewItem {
                        Header = System.IO.Path.GetFileName(openFile.FileName)
                    };

                    DSC.Log("Adding root node...");
                    cNodes.Items.Add(ANode);

                    DSC.Log("Adding nodes...");
                    FillTreeView(ANode);
                    DSC.Log("Finished adding {0} nodes!", __i);

                    __i = 1; // reset chunks read

                    //ANode.ExpandSubtree();

                    cNodes.UpdateLayout();
                }
            }
        }

        private void open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialogClick();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void cNodes_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var node = cNodes.SelectedItem;

            if (node is TreeViewItem)
            {
                var n = ((TreeViewItem)node).Tag;

                if (n is NodeTag)
                {
                    PropGrid.SelectedObject = ((NodeTag)n).BaseChunk;
                    PropGrid.SelectedObjectName = (string)((TreeViewItem)cNodes.SelectedItem).Header;
                }
                else if (PropGrid.SelectedObject != null)
                {
                    PropGrid.SelectedObject = null;
                    PropGrid.SelectedObjectName = "";
                }
            }
        }
    }

    public class TreeViewLineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TreeViewItem item = (TreeViewItem)value;
            ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(item);
            return ic.ItemContainerGenerator.IndexFromContainer(item) == ic.Items.Count - 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public class UIntMagicConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var magic = (uint)value;

            return (magic > 255)
                ? new string(new char[]{
                    (char)(magic & 0x000000FF),
                    (char)((magic & 0x0000FF00) >> 8),
                    (char)((magic & 0x00FF0000) >> 16),
                    (char)((magic & 0xFF000000) >> 24)})
                    : (magic == 0)
                        ? "Unified Packager"
                        : magic.ToString("X");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new Exception("Not implemented yet!");
        }
    }

    public class UIntToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return String.Format("0x{0}", ((uint)value).ToString("X"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new Exception("Not implemented yet!");
        }
    }
}
