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
using DSCript.Methods;
using DSCript.Object;

using Microsoft.Win32;

namespace DVEdit
{
    using XCTK = Xceed.Wpf.Toolkit;

    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        public Main()
        {
            InitializeComponent();

            // This is here for debugging purposes
            ChooseFile();

            // Assign event handlers
            open.Click += (o, e) => ChooseFile();
            close.Click += (o, e) => Environment.Exit(0);
        }

        public ChunkReader ChunkFile { get; set; }

        // Not being used at the moment
        public void LoadFile(string filename)
        {
            if (filename.EndsWith("vvs") || filename.EndsWith("vvv"))
                ChunkFile = new ChunkReader(filename);
            else
                MessageBox.Show("You can't open this kind of file!"); 
        }

        private void ChooseFile()
        {
            OpenFileDialog OpenFile =
                new OpenFileDialog
                {
                    //Title = "Select a DRIV3R Vehicles package",
                    //Filter = "DRIV3R Vehicles (*.vvs;*.vvv)|*.vvs;*.vvv"

                    Title = "Select a CHUNK File",
                    Filter = "Reflections CHUNK File|*.*",

                    CheckFileExists = true,
                    CheckPathExists = true
                };

            if ((bool?)OpenFile.ShowDialog() == true)
            {
                ChunkFile = new ChunkReader(OpenFile.FileName);

                if (ChunkFile.IsChunk)
                    Nodes.CreateTreeView(ChunkFile, cNodes);
            }
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
            return Chunks.Magic2Str((uint)value);
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
