using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.Xml;
using System.Xml.Linq;

using Microsoft.Win32;

using DSCript;
using DSCript.Models;
using DSCript.Spooling;

using COLLADA;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for Importer.xaml
    /// </summary>
    public partial class Importer : ObservableWindow
    {
        static readonly OpenFileDialog OpenDialog;

        static Importer()
        {
            var filters = new[] {
                "All supported files|*.dae;*.obj",
                "COLLADA|*.dae",
                "Wavefront OBJ|*.obj",
            };
            
            OpenDialog = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = String.Join("|", filters),
                ValidateNames = true,
            };
        }

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

        // need to come up with a better system...
        private ModelConverter m_converter;
        private ModelPackage m_modelPackage;

        public bool CanSave
        {
            get
            {
                return (m_converter != null);
            }
        }

        private void ModelSelected(object sender, RoutedEventArgs e)
        {
            OnPropertyChanged("ModelProperties");
        }
        
        private void LoadCOLLADA(string filename)
        {
            var colladaDoc = new COLLADADocument(filename);

            m_converter = new ModelConverter();
            m_converter.LoadCOLLADA(colladaDoc);

            // HACK HACK HACK
            m_converter.EffectType = EffectType.Vehicle;

            // compile model package
            m_modelPackage = m_converter.ToModelPackage();
        }

        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            if (OpenDialog.ShowDialog(Owner) ?? false)
            {
                var fileName = OpenDialog.FileName;

                var ext = Path.GetExtension(OpenDialog.FileName).ToLower();

                switch (ext)
                {
                case ".obj":
                    ObjFile = new ObjFile(OpenDialog.FileName);
                    OnPropertyChanged("Models");
                    break;
                case ".dae":
                    LoadCOLLADA(fileName);
                    OnPropertyChanged("CanSave");
                    break;
                }
            }
        }

        private void SaveFileClick(object sender, RoutedEventArgs e)
        {
            var saveModel = new SaveFileDialog() {
                Title               = "Save model as:",
                DefaultExt          = ".rimodel",
                InitialDirectory    = Settings.ExportDirectory,
                AddExtension        = true,
            };

            if (saveModel.ShowDialog(Owner) ?? false)
            {
#if USE_OLD_SAVE_CODE
                var spooler = SpoolableResourceFactory.Create<ModelPackagePC>();
                
                var nVertices = ObjFile.Positions.Count;
                var vertices = new List<Vertex>(nVertices);

                for (int v = 0; v < ObjFile.Positions.Count; v++)
                {
                    var vp = ObjFile.Positions[v];

                    vertices[v].Position = new Vector3(
                        (float)vp.X,
                        (float)vp.Y,
                        (float)vp.Z);
                }

                for (int t = 0; t < ObjFile.TextureCoordinates.Count; t++)
                {
                    var vt = ObjFile.TextureCoordinates[t];

                    vertices[t].UV = new Vector2(
                        (float)vt.X,
                        (float)vt.Y);
                }

                for (int n = 0; n < ObjFile.Normals.Count; n++)
                {
                    var vt = ObjFile.Normals[n];

                    vertices[n].Normal = new Vector3(
                        (float)vt.X,
                        (float)vt.Y,
                        (float)vt.Z);
                }

                //spooler.VertexBuffers = new List<VertexData>();

                /*
                    This is where I realized this won't work!
                    The OBJ format just isn't made for this kind of stuff :(
                */
#else
                var chunker = new FileChunker();

                // if we sent it to the viewport, the default package gets nuked
                ISpoolableResource resource = m_modelPackage ?? AT.CurrentState.SelectedModelPackage;

                resource.Save();

                // definitely flawed
                chunker.Content.Children.Add(resource.Spooler);
                
                var outDir = Path.GetDirectoryName(saveModel.FileName);

                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                chunker.Save(saveModel.FileName);
#endif
            }
        }

        public Importer()
        {
            InitializeComponent();
        }

        private void BTConvert_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BTLoad3D_Click(object sender, RoutedEventArgs e)
        {
            AT.CurrentState.SelectedModelPackage = m_modelPackage;
            m_modelPackage = null; // use model in viewer instead
        }
    }
}
