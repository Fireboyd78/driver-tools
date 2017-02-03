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
using System.Windows.Shapes;

using Microsoft.Win32;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for Importer.xaml
    /// </summary>
    public partial class Importer : ObservableWindow
    {
        static readonly OpenFileDialog OpenObj = new OpenFileDialog() {
            CheckFileExists = true,
            CheckPathExists = true,
            Filter          = "Wavefront OBJ|*.obj",
            ValidateNames   = true,
        };

        protected ObjFile ObjFile { get; set; }

        protected ObjFile.ObjGroup SelectedModel
        {
            get { return TModels.SelectedItem as ObjFile.ObjGroup; }
        }

        public List<ObjFile.ObjGroup> Models
        {
            get
            {
                if (ObjFile != null)
                    return ObjFile.Groups;

                return null;
            }
        }

        public object ModelProperties
        {
            get
            {
                if (SelectedModel != null)
                    return SelectedModel.Name;

                return null;
            }
        }

        public bool CanSave
        {
            get { return false; }
        }

        private void ModelSelected(object sender, RoutedEventArgs e)
        {
            OnPropertyChanged("ModelProperties");
        }

        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            if (OpenObj.ShowDialog(Owner) ?? false)
            {
                ObjFile = new ObjFile(OpenObj.FileName);
                OnPropertyChanged("Models");
            }
        }

        private void SaveFileClick(object sender, RoutedEventArgs e)
        {

        }

        public Importer()
        {
            InitializeComponent();
        }

    }
}
