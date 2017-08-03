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
        
        protected object CurrentSelection
        {
            get { return TModels.SelectedItem; }
        }

        private int GetObjDataType(object obj)
        {
            return (obj is ObjFile.ObjGroup) ? 0
                : (obj is ObjFile.ObjMesh) ? 1
                : (obj is ObjFile.ObjMaterial) ? 2
                : -1;
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

        public List<FrameworkElement> ModelProperties
        {
            get
            {
                var items = new List<FrameworkElement>();
                
                if (CurrentSelection != null)
                {
                    var objId = GetObjDataType(CurrentSelection);

                    switch (objId)
                    {
                    case 1:
                        {
                            var mesh = CurrentSelection as ObjFile.ObjMesh;
                            var group = mesh.Group;
                            var mat = mesh.Material;

                            var meshIdx = group.Meshes.IndexOf(mesh) + 1;
                            
                            if (mat != null)   
                                items.Add(new TextBlock() { Text = $"Material: \"{mat.Name}\"" });
                        } break;
                    }
                }

                return items;
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
