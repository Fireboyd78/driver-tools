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

namespace Antilli
{
    /// <summary>
    /// Interaction logic for Importer.xaml
    /// </summary>
    public partial class Importer : ObservableWindow
    {
        private ObjFile m_objFile;
        private ObjFile.ObjGroup m_model;

        public List<ObjFile.ObjGroup> Models
        {
            get
            {
                if (m_objFile != null)
                {
                    return m_objFile.Groups;
                }

                return null;
            }
        }

        public object ModelProperties
        {
            get
            {
                if (m_model != null)
                {
                    return m_model.Name;
                }

                return null;
            }
        }

        public bool CanSave
        {
            get { return false; }
        }

        private void ModelSelected(object sender, RoutedEventArgs e)
        {
            m_model = e.Source as ObjFile.ObjGroup;
        }

        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            //m_objFile = new ObjFile(@"C:\Users\Mark\Desktop\chally.obj");
            //OnPropertyChanged("Models");
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
