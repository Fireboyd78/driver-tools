using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;

using DSCript;

namespace Antilli
{
    public sealed class ObjFile
    {
        public enum IlluminationModel : int
        {
            Color                       = 0,
            ColorAmbient                = 1,
            Highlight                   = 2,
            ReflectionRayTrace          = 3,
            GlassRayTrace               = 4,
            FresnelRayTrace             = 5,
            RefractionRayTrace          = 6,
            RefractionRayTraceFresnel   = 7,
            Reflection                  = 8,
            Glass                       = 9,
            InvisibleSurfaceShadows     = 10
        }

        public enum ComponentType
        {
            Ambient,
            Diffuse,
            Specular
        }

        public sealed class ObjMaterial
        {
            public string Name { get; set; }

            public Dictionary<string, string> Properties { get; private set; }

            public string this[string key]
            {
                get { return (Properties.ContainsKey(key)) ? Properties[key] : null; }
                set
                {
                    if (!Properties.ContainsKey(key))
                    {
                        Properties.Add(key, value);
                    }
                    else
                    {
                        Properties[key] = value;
                    }
                }
            }

            public string Compile()
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("newmtl {0}", Name).AppendLine();

                foreach (KeyValuePair<string, string> kv in Properties)
                {
                    if (kv.Value != null)
                        sb.AppendFormatEx("\t{0} {1}", kv.Key, kv.Value).AppendLine();
                }

                return sb.ToString();
            }

            private void SetColorAndMap(string colorKey, string mapKey, Vector3D color, string map)
            {
                Properties[colorKey] = String.Format("{0:F4} {1:F4} {2:F4}",
                    Math.Max(0.0, Math.Min(color.X, 1.0)),
                    Math.Max(0.0, Math.Min(color.Y, 1.0)),
                    Math.Max(0.0, Math.Min(color.Z, 1.0)));

                if (map != null)
                    Properties[mapKey] = map;
            }

            public void SetComponent(ComponentType componentType, Vector3D color, string map = null)
            {
                switch (componentType)
                {
                case ComponentType.Ambient: SetColorAndMap("Ka", "map_Ka", color, map); break;
                case ComponentType.Diffuse: SetColorAndMap("Kd", "map_Kd", color, map); break;
                case ComponentType.Specular: SetColorAndMap("Ks", "map_Ks", color, map); break;
                }
            }

            public void SetOpacity(double opacity, string map = null)
            {
                Properties["d"] = String.Format("{0:F1}", Math.Max(0.0, Math.Min(opacity, 1.0)));

                if (map != null)
                    Properties["map_d"] = map;
            }

            private string _spec
            {
                get { return Properties["Ns"]; }
                set { Properties["Ns"] = value; }
            }

            public double Specularity
            {
                get { return (_spec != null) ? double.Parse(_spec) : 0.0; }
                set { _spec = String.Format("{0:F4}", Math.Max(0.0, Math.Min(value, 1000.0))); }
            }

            public BitmapSource DiffuseMap
            {
                get
                {
                    if (Properties.ContainsKey("map_Kd"))
                        return BitmapSourceHelper.GetBitmapSource(Properties["map_Kd"], BitmapSourceLoadFlags.Default);

                    return null;
                }
            }

            public ObjMaterial(string name)
            {
                Name = name;
                Properties = new Dictionary<string, string>() {
                    { "Ns",         null                    },
                    { "d",          "1.0"                   },
                    { "illum",      "2"                     },
                    { "Ka",         "1.0000 1.0000 1.0000"  },
                    { "Kd",         "1.0000 1.0000 1.0000"  },
                    { "Ks",         "1.0000 1.0000 1.0000"  },
                    { "map_Ka",     null                    },
                    { "map_Kd",     null                    },
                    { "map_Ks",     null                    },
                    { "map_d",      null                    }
                };
            }
        }

        public sealed class ObjMesh
        {
            public string Name { get; set; }

            public ObjGroup Group { get; private set; }
            public ObjMaterial Material { get; set; }

            public int SmoothingGroup { get; set; }

            public List<int[,]> Faces { get; set; }

            public ObjMesh Split(ObjMaterial material)
            {
                return new ObjMesh(Group) { Material = material };
            }

            public ObjMesh Split(int smoothingGroup)
            {
                return new ObjMesh(Group) { SmoothingGroup = smoothingGroup };
            }

            public ObjMesh Split(ObjMaterial material, int smoothingGroup)
            {
                return new ObjMesh(Group) { Material = material, SmoothingGroup = smoothingGroup };
            }

            public ObjMesh(ObjGroup group)
            {
                Group = group;
                Faces = new List<int[,]>();

                SmoothingGroup = 0;
            }
        }

        public sealed class ObjGroup
        {
            public string Name { get; set; }

            public ObjFile Parent { get; private set; }

            public List<ObjMesh> Meshes { get; set; }

            public Model3DGroup GetModel()
            {
                var model = new Model3DGroup();

                // indice lookup stored as long
                // vx = (hash & 0xFFFF)
                // vt = ((hash >> 16) & 0xFFFF)
                // vn = ((hash >> 32) & 0xFFFF)

                var hashes = new Dictionary<long, int>();

                foreach (ObjMesh mesh in Meshes)
                {
                    var positions = new Point3DCollection();
                    var normals = new Vector3DCollection();
                    var coords = new PointCollection();

                    var indices = new Int32Collection();

                    int minIndex = 0;

                    for (int t = 0; t < mesh.Faces.Count; t++)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            int vx = mesh.Faces[t][i, 0];
                            int vt = mesh.Faces[t][i, 1];
                            int vn = mesh.Faces[t][i, 2];

                            positions.Add(Parent.Positions[vx]);

                            if (vt != -1)
                                coords.Add(Parent.TextureCoordinates[vt]);
                            if (vn != -1)
                                normals.Add(Parent.Normals[vn]);

                            //long hash = ((vx | ((vt != -1) ? (vt << 16) : 0)) | ((vn != -1) ? (vn << 32) : 0));
                            //
                            //if (!hashes.ContainsKey(hash))
                            //{
                            //    hashes.Add(hash, minIndex++);
                            //
                            //    positions.Add(Parent.Positions[vx]);
                            //
                            //    if (vt != -1)
                            //        coords.Add(Parent.TextureCoordinates[vt]);
                            //    if (vn != -1)
                            //        normals.Add(Parent.Normals[vn]);
                            //}

                            //int indice = hashes[hash];

                            indices.Add(minIndex++);
                        }
                    }

                    BitmapSource bmap = (mesh.Material != null) ? mesh.Material.DiffuseMap : null;

                    DiffuseMaterial mat = new DiffuseMaterial();

                    if (bmap != null)
                    {
                        mat.Brush = new ImageBrush() {
                            ImageSource = bmap,
                            TileMode = TileMode.Tile,
                            Stretch = Stretch.Fill,
                            ViewportUnits = BrushMappingMode.Absolute
                        };
                    }
                    else
                    {
                        mat.Brush = new SolidColorBrush(Colors.White);
                    }

                    model.Children.Add(new GeometryModel3D() {
                        Geometry = new MeshGeometry3D() {
                            Positions = positions,
                            Normals = normals,
                            TextureCoordinates = coords,
                            TriangleIndices = indices
                        },
                        Material = mat,
                        BackMaterial = mat
                    });
                }

                return model;
            }
            
            public ObjGroup(ObjFile parent)
            {
                Parent = parent;
                Meshes = new List<ObjMesh>();
            }
        }

        public Point3DCollection Positions { get; set; }
        public Vector3DCollection Normals { get; set; }
        public PointCollection TextureCoordinates { get; set; }

        public List<ObjGroup> Groups { get; private set; }
        public Dictionary<string, ObjMaterial> Materials { get; private set; }

        private ObjGroup currentGroup;
        private ObjMesh currentMesh;

        private ObjMaterial currentMaterial;

        private ObjGroup CurrentGroup
        {
            get { return currentGroup; }
            set
            {
                currentGroup = value;

                if (currentGroup != null)
                    Groups.Add(currentGroup);
            }
        }

        private ObjMesh CurrentMesh
        {
            get { return currentMesh; }
            set
            {
                currentMesh = value;

                if (currentMesh != null && CurrentGroup != null)
                    CurrentGroup.Meshes.Add(currentMesh);
            }
        }

        private ObjMaterial CurrentMaterial
        {
            get { return currentMaterial; }
            set
            {
                currentMaterial = value;

                if (currentMaterial != null)
                    Materials.Add(currentMaterial.Name, currentMaterial);
            }
        }

        public string Name { get; set; }

        public string FilePath { get; private set; }

        #region Import helper methods
        private void GetKeyVal(string line, out string key, out string val)
        {
            key = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0];
            val = line.Split(new[] { key }, StringSplitOptions.None).Merge();

            while (val.StartsWith(" ") || val.StartsWith("\t"))
                val = val.Substring(1);
            while (val.EndsWith(" "))
                val = val.Substring(0, val.Length - 1);
        }

        private int GetValIndices(string val, out int[,] indices)
        {
            indices =
                new int[3, 3]{
                    { -1, -1, -1 },
                    { -1, -1, -1 },
                    { -1, -1, -1 }
                };

            string[] vals = val.Split(' ');

            // invalid indice
            if (vals.Length < 1)
                return -1;
            // quads not supported
            else if (vals.Length > 3)
                return -2;

            for (int t = 0; t < vals.Length; t++)
            {
                string[] ind = vals[t].Split(new[] { '/' }, StringSplitOptions.None);

                // weird error occured
                if (ind.Length > 3)
                    return -3;

                int i0, i1, i2;

                indices[t, 0] = (int.TryParse(ind[0], out i0)) ? i0 - 1 : -1;
                indices[t, 1] = (int.TryParse(ind[1], out i1)) ? i1 - 1 : -1;

                if (ind.Length == 3)
                    indices[t, 2] = (int.TryParse(ind[2], out i2)) ? i2 - 1 : -1;
            }

            return 0;
        }

        private int GetValPoints2D(string val, out double x, out double y)
        {
            x = y = 0.0;

            string[] vals = val.Split(' ');

            if (vals.Length < 2)
                return -1;

            x = (double)float.Parse(vals[0], DSC.CurrentCulture);
            y = (double)float.Parse(vals[1], DSC.CurrentCulture);

            return 0;
        }

        private int GetValPoints3D(string val, out double x, out double y, out double z)
        {
            x = y = z = 0.0;

            string[] vals = val.Split(' ');

            if (vals.Length < 3)
                return -1;

            x = (double)float.Parse(vals[0], DSC.CurrentCulture);
            y = (double)float.Parse(vals[1], DSC.CurrentCulture);
            z = (double)float.Parse(vals[2], DSC.CurrentCulture);

            return 0;
        }
        #endregion Importer helper methods

        private void ImportMTL(string name)
        {
            using (TextReader f = new StreamReader(Path.Combine(Path.GetDirectoryName(FilePath), name)))
            {
                int line = 0;

                string str = "", key = "", val = "", lastKey = "";

                while ((str = f.ReadLine()) != null)
                {
                    ++line;

                    if (str == "" || str.StartsWith("#"))
                        continue;

                    GetKeyVal(str, out key, out val);

                    switch (key)
                    {
                    case "newmtl":
                        {
                            CurrentMaterial = new ObjMaterial(val);
                        } break;
                    default:
                        {
                            if (CurrentMaterial != null)
                                CurrentMaterial[key] = val;
                        } break;
                    }

                    lastKey = key;
                }

                CurrentMaterial = null;

                DSC.Log("Done reading material file!");
            }
        }

        private void Import(Stream stream)
        {
            using (TextReader f = new StreamReader(stream))
            {
                int line = 0;

                string str = "", key = "", val = "", lastKey = "", lastVal = "";

                Positions = new Point3DCollection();
                Normals = new Vector3DCollection();
                TextureCoordinates = new PointCollection();

                while ((str = f.ReadLine()) != null)
                {
                    ++line;

                    if (str == "" || str.StartsWith("#"))
                        continue;

                    GetKeyVal(str, out key, out val);

                    switch (key)
                    {
                    case "v":
                        {
                            // check for stupid exporters that append 'g' before vertices -.-
                            if (lastKey == "g" || lastKey == "o")
                            {
                                // remove null mesh that was added
                                if (CurrentGroup != null)
                                    CurrentGroup.Meshes.Remove(CurrentMesh);

                                CurrentGroup = new ObjGroup(this) { Name = lastVal };
                                CurrentMesh = null;
                            }
                            else if (lastKey == "f" || CurrentGroup == null)
                                CurrentGroup = new ObjGroup(this);

                            double x, y, z;

                            if (GetValPoints3D(val, out x, out y, out z) == -1)
                                throw new Exception(String.Format("Bad vertex definition on line {0}!", line));

                            Positions.Add(new Point3D(x, y, z));
                        } break;
                    case "vn":
                        {
                            double x, y, z;

                            if (GetValPoints3D(val, out x, out y, out z) == -1)
                                throw new Exception(String.Format("Bad vertex normal definition on line {0}!", line));

                            Normals.Add(new Vector3D(x, y, z));
                        } break;
                    case "vt":
                        {
                            double x, y;

                            if (GetValPoints2D(val, out x, out y) == -1)
                                throw new Exception(String.Format("Bad vertex texture-coord definition on line {0}!", line));

                            TextureCoordinates.Add(new Point(x, -y));
                        } break;
                    case "g":
                        {
                            CurrentMesh = new ObjMesh(CurrentGroup) { Name = val };
                        } break;
                    case "s":
                        {
                            if (CurrentMesh == null)
                                CurrentMesh = new ObjMesh(CurrentGroup) { SmoothingGroup = int.Parse(val) };
                            else if (lastKey == "f")
                                CurrentMesh = CurrentMesh.Split(int.Parse(val));
                        } break;
                    case "usemtl":
                        {
                            if (lastKey == "f")
                                CurrentMesh = CurrentMesh.Split(Materials[val]);
                            else if (lastKey == "g" || lastKey == "s")
                            {
                                CurrentMesh.Material = Materials[val];
                            } else
                                throw new Exception(String.Format("Bad 'usemtl' definition on line {0}!", line));
                        } break;
                    case "f":
                        {
                            int[,] indices;

                            if (CurrentMesh == null || GetValIndices(val, out indices) == -1)
                                throw new Exception(String.Format("Bad face definition on line {0}!", line));

                            CurrentMesh.Faces.Add(indices);
                        } break;
                    case "mtllib":
                        {
                            ImportMTL(val);
                        } break;
                    default:
                        break;
                    }

                    lastKey = key;
                    lastVal = val;
                }

                DSC.Log("Reached end of file.");
            }
        }

        public ObjFile()
        {
            Groups = new List<ObjGroup>();
            Materials = new Dictionary<string, ObjMaterial>();
        }

        public ObjFile(string path)
        {
            Groups = new List<ObjGroup>();
            Materials = new Dictionary<string, ObjMaterial>();

            FilePath = path;

            Import(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
    }
}
