using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

using DSCript;

namespace DSCript.Models
{
    public class ObjFile
    {
        public class Element
        {
            protected ObjFile Parent { get; set; }

            protected Element(ObjFile parent)
            {
                Parent = parent;
            }
        }

        public class Group
        {
            internal static int Index;

            static Group()
            {
                Index = 0;
            }

            public List<Mesh> Meshes { get; set; }

            public string Name { get; set; }

            public Group() : this(String.Format("Model {0}", ++Index)) { }

            public Group(string name)
            {
                Name = name;
                Meshes = new List<Mesh>();
            }

            public Group(string name, int nMeshes)
            {
                Name = name;
                Meshes = new List<Mesh>(nMeshes);
            }
        }

        public class Mesh : Element
        {
            public Point3DCollection Positions { get; set; }
            public Vector3DCollection Normals { get; set; }
            public PointCollection TextureCoordinates { get; set; }

            public Int32Collection TriangleIndices { get; set; }

            public ObjMaterial Material { get; set; }

            public Mesh(ObjFile parent)
                : base(parent)
            {
                Parent = parent;

                Positions = new Point3DCollection();
                Normals = new Vector3DCollection();
                TextureCoordinates = new PointCollection();

                TriangleIndices = new Int32Collection();
            }
        }

        public class ObjMaterial : Element
        {
            public Dictionary<string, string> Properties { get; private set; }

            public string Name
            {
                get { return Parent.Materials.First((o) => o.Value == this).Key; }
            }

            public string this[string name]
            {
                get { return (Properties.ContainsKey(name)) ? Properties[name] : null; }
                set
                {
                    if (Properties.ContainsKey(name))
                        Properties[name] = value;
                    else
                        Properties.Add(name, value);
                }
            }

            public FileStream Map_Kd
            {
                get
                {
                    string path = this["map_Kd"];

                    if (!File.Exists(path))
                        path = String.Format("{0}\\{1}", Path.GetDirectoryName(Parent.MtlPath), path);

                    return (path != null && File.Exists(path)) ? File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
                }
            }

            public ObjMaterial(ObjFile parent, string name)
                : base(parent)
            {
                Properties = new Dictionary<string, string>();

                if (!Parent.Materials.ContainsKey(name))
                    Parent.Materials.Add(name, this);
            }
        }

        public Dictionary<string, ObjMaterial> Materials { get; private set; }

        public Point3DCollection Positions { get; private set; }
        public Vector3DCollection Normals { get; private set; }
        public PointCollection TextureCoordinates { get; private set; }

        public List<Group> Groups { get; private set; }
        public List<Mesh> Meshes { get; private set; }

        public bool FlipUVs { get; set; }

        public string Name
        {
            get { return Path.GetFileNameWithoutExtension(ObjPath); }
        }

        private string MtlPath { get; set; }
        private string ObjPath { get; set; }

        private FileStream GetObjStream()
        {
            return (File.Exists(ObjPath)) ? File.Open(ObjPath, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
        }

        private FileStream GetMtlStream()
        {
            return (File.Exists(MtlPath)) ? File.Open(MtlPath, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
        }

        public void Load()
        {
            string log = "";

            switch (LoadObj())
            {
            case 0: log = "Successfully loaded '{0}'!"; break;
            case 1: log = "The file '{0}' failed to load properly!"; break;
            case 2: log = "The file '{0}' does not exist!"; break;
            }

            DSC.Log(log, ObjPath);
        }

        private void LoadMtl()
        {
            string log = "";

            switch (_LoadMtl())
            {
            case 0: log = "Successfully loaded material file '{0}'!"; break;
            case 1: log = "The material file '{0}' failed to load properly!"; break;
            case 2: log = "The material file '{0}' does not exist!"; break;
            }

            DSC.Log(log, MtlPath);
        }

        private int _LoadMtl()
        {
            using (FileStream fs = GetMtlStream())
            {
                if (fs == null)
                    return 2;

                using (TextReader reader = new StreamReader(fs))
                {
                    string str = "";

                    string key = "";
                    string val = "";

                    string lastKey = "";

                    int curLine = 0;

                    ObjMaterial mtl = null;

                    while ((str = reader.ReadLine()) != null)
                    {
                        ++curLine;

                        string[] strs = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (strs.Length < 1)
                            continue;

                        key = strs[0];
                        val = str.Split(new[] { key }, StringSplitOptions.None).Merge();

                        while (val.StartsWith(" "))
                            val = val.Substring(1);
                        while (val.EndsWith(" "))
                            val = val.Substring(0, val.Length - 1);

                        while (key.StartsWith("\t"))
                            key = key.Substring(1);

                        string[] vals = val.Split(' ');

                        switch (key.ToLower())
                        {
                        // Skip comments
                        case "#":
                            break;
                        case "newmtl":
                            {
                                if (vals.Length < 1 || lastKey == "newmtl")
                                    throw new Exception(String.Format("LoadMtl::Bad material definition on line {0}!", curLine));

                                mtl = new ObjMaterial(this, val);

                            } goto setKey;
                        case "map_ka":
                        case "map_kd":
                            {
                                //str.Replace('\\', '\\\\');
                                DSC.Log("{0}: {1}", key, str);

                            } goto default;
                        setKey:
                            {
                                // set the last key
                                lastKey = key;
                            } break;
                        default:
                            {
                                if (vals.Length < 1)
                                    throw new Exception(String.Format("LoadMtl::Invalid operand on line {0}!", curLine));

                                mtl[key] = val;

                            } goto setKey;
                        }
                    }

                    if (Materials.Count > 0)
                        return 0;
                }
            }
            return 1;
        }

        private int LoadObj()
        {
            using (FileStream fs = GetObjStream())
            {
                if (fs == null)
                    return 2;

                using (TextReader reader = new StreamReader(fs))
                {
                    string str = "";

                    bool hasMtl = false;

                    Positions = new Point3DCollection();
                    Normals = new Vector3DCollection();
                    TextureCoordinates = new PointCollection();

                    string key = "";
                    string val = "";

                    string lastKey = "";

                    int curLine = 0;
                    int nFaces = 0;
                    
                    Group group = null;
                    Mesh mesh = null;

                    ObjMaterial curMtl = null;

                    while ((str = reader.ReadLine()) != null)
                    {
                        ++curLine;

                        string[] strs = str.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries);

                        if (strs.Length <= 1)
                            continue;

                        key = strs[0];
                        val = str.Split(new[] { key }, StringSplitOptions.None).Merge();

                        while (val.StartsWith(" "))
                            val = val.Substring(1);
                        while (val.EndsWith(" "))
                            val = val.Substring(0, val.Length - 1);

                        string[] vals = val.Split(' ');
                        
                        switch (key.ToLower())
                        {
                        case "v":
                            {
                                if (vals.Length < 3)
                                    throw new Exception(String.Format("Bad vertex definition on line {0}!", curLine));

                                Positions.Add(new Point3D() {
                                    X = StringHelper.ToDouble(vals[0]),
                                    Y = StringHelper.ToDouble(vals[1]),
                                    Z = StringHelper.ToDouble(vals[2])
                                });

                            } goto setKey;
                        case "vn":
                            {
                                if (vals.Length < 3)
                                    throw new Exception(String.Format("Bad vertex normal definition on line {0}!", curLine));

                                Normals.Add(new Vector3D() {
                                    X = StringHelper.ToDouble(vals[0]),
                                    Y = StringHelper.ToDouble(vals[1]),
                                    Z = StringHelper.ToDouble(vals[2])
                                });

                            } goto setKey;
                        case "vt":
                            {
                                if (vals.Length < 2)
                                    throw new Exception(String.Format("Bad vertex uv definition on line {0}!", curLine));

                                double u = StringHelper.ToDouble(vals[0]);
                                double v = StringHelper.ToDouble(vals[1]);

                                TextureCoordinates.Add(new Point() {
                                    X = u,
                                    Y = (FlipUVs) ? -v : v
                                });

                            } goto setKey;
                        case "g":
                            {
                                if (vals.Length < 1)
                                    throw new Exception(String.Format("Bad group definition on line {0}!", curLine));

                                // only add new groups after vertices have been defined
                                // stupid blender
                                if (lastKey.StartsWith("v"))
                                {
                                    group = new Group(vals[0]);
                                    Groups.Add(group);
                                }

                            } goto setKey;
                        case "f":
                            {
                                if (vals.Length < 3)
                                    throw new Exception(String.Format("Bad face definition on line {0}!", curLine));

                                if (lastKey != "f")
                                {
                                    if (group == null)
                                    {
                                        group = new Group();
                                        Groups.Add(group);
                                    }

                                    mesh = new Mesh(this) {
                                        Material = curMtl
                                    };

                                    group.Meshes.Add(mesh);
                                    Meshes.Add(mesh);
                                }

                                foreach (string face in vals)
                                {
                                    string[] indice = face.Split('/');

                                    int type = indice.Length;

                                    if (type > 2 && indice[1] == "")
                                    {
                                        indice = face.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                                        type = 0;
                                    }

                                    if (indice.Length == 0)
                                        throw new Exception(String.Format("Invalid face on line {0}!", curLine));

                                    int vx = 0;
                                    int vn = 0;
                                    int vt = 0;

                                    bool hasVt = (type >= 2);
                                    bool hasVn = (type == 3 || type == 0);

                                    vx = StringHelper.ToInt32(indice[0]) - 1;
                                    
                                    if (hasVt)
                                        vt = StringHelper.ToInt32(indice[1]) - 1;

                                    if (hasVn)
                                        vn = StringHelper.ToInt32(indice[(hasVt) ? 2 : 1]) - 1;

                                    bool addPos = !mesh.Positions.Contains(Positions[vx]);
                                    bool addNor = (hasVn) ? !mesh.Normals.Contains(Normals[vn]) : false;
                                    bool addUv = !mesh.TextureCoordinates.Contains(TextureCoordinates[vt]);

                                    if (addPos || addUv || addNor)
                                    {
                                        mesh.Positions.Add(Positions[vx]);
                                        mesh.Normals.Add((!addNor) ? Normals[vn] : new Vector3D(0,0,0));
                                        mesh.TextureCoordinates.Add(TextureCoordinates[vt]);
                                    }

                                    int vIdx = (!addPos) ? mesh.Positions.Count - 1 : mesh.Positions.IndexOf(Positions[vx]);

                                    mesh.TriangleIndices.Add(vIdx);                                    
                                }

                                ++nFaces;

                            } goto setKey;
                        case "mtllib":
                            {
                                if (vals.Length < 1)
                                    throw new Exception(String.Format("Bad material library assignment on line {0}!", curLine));

                                string mtlFile = val;
                                string mtlPath = String.Format("{0}{1}", ObjPath.Substring(0, ObjPath.Length - (Path.GetFileName(ObjPath)).Length), mtlFile);

                                hasMtl = File.Exists(mtlPath);

                                DSC.Log("Material library: {0}", mtlPath);
                                DSC.Log("Status: {0}", (hasMtl) ? "found" : "missing");

                                if (hasMtl)
                                {
                                    MtlPath = mtlPath;
                                    LoadMtl();
                                }

                            } goto setKey;
                        case "usemtl":
                            {
                                if (vals.Length < 1)
                                    throw new Exception(String.Format("Bad mesh material assignment on line {0}!", curLine));

                                string mtl = val;
                                curMtl = (Materials.ContainsKey(mtl)) ? Materials[mtl] : null;

                                if (curMtl == null)
                                    DSC.Log("WARNING: Assigning null material '{0}' on line {1}!", mtl, curLine);

                            } goto setKey;
                        setKey:
                            {
                                // set the last key
                                lastKey = key;
                            } break;
                        default:
                            break;
                        }
                    }

                    if (Positions != null && nFaces > 0)
                        return 0;
                }
            }
            return 1;
        }

        public ObjFile(string filename)
            : this(filename, false)
        {
            
        }

        public ObjFile(string filename, bool flipUVs)
        {
            ObjPath = filename;

            Groups = new List<Group>();
            Meshes = new List<Mesh>();

            Materials = new Dictionary<string, ObjMaterial>();

            FlipUVs = flipUVs;

            // Reset index
            Group.Index = 0;
        }
    }
}
