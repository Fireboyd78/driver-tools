using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.Linq;

namespace COLLADA
{
    public class COLLADAPolyList : COLLADABaseType
    {
        protected override string TagName => "polylist";

        private COLLADAMesh m_parent;

        public COLLADAMesh Parent
        {
            get
            {
                if (m_parent == null)
                    throw new InvalidOperationException("Parent source not set!");

                return m_parent;
            }
            set { m_parent = value; }
        }

        public string Material;

        public int Count;

        public List<COLLADASharedInput> Inputs;

        public List<int> VCount;
        public List<int> Indices;

        public override void LoadXml(XmlElement node)
        {
            Material = node.GetAttribute("material");
            Count = int.Parse(node.GetAttribute("count"), NumberStyles.Integer);

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "input":
                    var input = new COLLADASharedInput();
                    var inputSource = child.GetAttribute("source").Substring(1);

                    // HACK HACK HACK!!
                    if (Parent.Vertices.Info.Id == inputSource)
                        inputSource = Parent.Vertices.Input.Source.Info.Id;

                    foreach (var source in Parent.Sources)
                    {
                        if (source.Info.Id == inputSource)
                        {
                            input.Source = source;
                            break;
                        }
                    }

                    if (input.Source == null)
                        throw new InvalidOperationException($"Could not find source '{inputSource}' for input!");

                    input.LoadXml(child);
                    Inputs.Add(input);

                    break;
                case "vcount":
                    foreach (var vc in child.InnerText.Split(' '))
                    {
                        if (String.IsNullOrEmpty(vc))
                            break;
                        var val = int.Parse(vc, NumberStyles.Integer);
                        VCount.Add(val);
                    }
                    break;
                case "p":
                    foreach (var ind in child.InnerText.Split(' '))
                    {
                        var val = int.Parse(ind, NumberStyles.Integer);
                        Indices.Add(val);
                    }
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            var xmlDoc = node.OwnerDocument;

            node.SetAttribute("material", Material);
            node.SetAttribute("count", Count.ToString());

            foreach (var input in Inputs)
                input.WriteTo(node);

            var vcNode = xmlDoc.CreateElement("vcount");
            var pNode = xmlDoc.CreateElement("p");

            vcNode.InnerText = String.Join(" ", VCount.ToArray());
            pNode.InnerText = String.Join(" ", Indices.ToArray());

            node.AppendChild(vcNode);
            node.AppendChild(pNode);
        }

        public COLLADAPolyList()
        {
            Inputs = new List<COLLADASharedInput>();

            VCount = new List<int>();
            Indices = new List<int>();
        }
    }

    public class COLLADAVertices : COLLADABaseType
    {
        protected override string TagName => "vertices";

        private COLLADAMesh m_parent;

        public COLLADAMesh Parent
        {
            get
            {
                if (m_parent == null)
                    throw new InvalidOperationException("Parent source not set!");

                return m_parent;
            }
            set { m_parent = value; }
        }

        public COLLADANodeInformation Info;

        public COLLADAInput Input;

        public override void LoadXml(XmlElement node)
        {
            Info.LoadXml(node);

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "input":
                    var input = new COLLADAInput();
                    var inputSource = child.GetAttribute("source").Substring(1);

                    foreach (var source in Parent.Sources)
                    {
                        if (source.Info.Id == inputSource)
                        {
                            input.Source = source;
                            break;
                        }
                    }

                    if (input.Source == null)
                        throw new InvalidOperationException($"Could not find source '{inputSource}' for input!");

                    input.LoadXml(child);
                    Input = input;

                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            Info.SaveXml(node);
            Input.WriteTo(node);
        }

        public COLLADAVertices()
        {
            Input = new COLLADAInput();
        }
    }

    public class COLLADAMesh : COLLADABaseType
    {
        protected override string TagName => "mesh";

        public List<COLLADASource> Sources;
        public COLLADAVertices Vertices;
        public List<COLLADAPolyList> PolyLists;

        public override void LoadXml(XmlElement node)
        {
            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "source":
                    var source = new COLLADASource();

                    source.LoadXml(child);
                    Sources.Add(source);

                    break;
                case "vertices":
                    var vertices = new COLLADAVertices() {
                        Parent = this
                    };

                    vertices.LoadXml(child);
                    Vertices = vertices;

                    break;

                case "polylist":
                    var polyList = new COLLADAPolyList() {
                        Parent = this
                    };

                    polyList.LoadXml(child);
                    PolyLists.Add(polyList);

                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            foreach (var source in Sources)
                source.WriteTo(node);

            Vertices.WriteTo(node);

            foreach (var polyList in PolyLists)
                polyList.WriteTo(node);
        }

        public COLLADAMesh()
        {
            Sources = new List<COLLADASource>();
            PolyLists = new List<COLLADAPolyList>();
        }
    }

    public class COLLADAGeometry : COLLADABaseType
    {
        protected override string TagName => "geometry";

        public COLLADANodeInformation Info;
        public COLLADAMesh Mesh;

        public override void LoadXml(XmlElement node)
        {
            Info.LoadXml(node);

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "mesh":
                    var mesh = new COLLADAMesh();

                    mesh.LoadXml(child);
                    Mesh = mesh;

                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            Info.SaveXml(node);
            Mesh.WriteTo(node);
        }
    }

    public class COLLADAGeometryInstance : COLLADABaseType
    {
        protected override string TagName => "instance_geometry";

        public string SubId;
        public string Name;

        public string Url;

        public COLLADAMaterialBinding MaterialBinding;

        public override void LoadXml(XmlElement node)
        {
            SubId = node.GetAttribute("sid");
            Name = node.GetAttribute("name");
            Url = node.GetAttribute("url");

            if (node.HasChildNodes)
            {
                foreach (var child in node.ChildNodes.OfType<XmlElement>())
                {
                    switch (child.Name)
                    {
                    case "bind_material":
                        MaterialBinding = COLLADAFactory.CreateFromXml<COLLADAMaterialBinding>(child);
                        break;
                    }
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            node.SetAttribute("sid", SubId);
            node.SetAttribute("name", Name);
            node.SetAttribute("url", Url);

            if (MaterialBinding != null)
                MaterialBinding.WriteTo(node);
        }
    }

    public class COLLADAGeometryLibrary : COLLADABaseType
    {
        protected override string TagName => "library_geometry";

        public List<COLLADAGeometry> Geometries;

        public COLLADAGeometry this[string name]
        {
            get
            {
                if (name[0] == '#')
                    name = name.Substring(1);

                return Geometries.Find((g) => g.Info.Id == name);
            }
        }

        public override void LoadXml(XmlElement node)
        {
            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "geometry":
                    var geometry = new COLLADAGeometry();

                    geometry.LoadXml(child);
                    Geometries.Add(geometry);

                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            foreach (var geometry in Geometries)
                geometry.WriteTo(node);
        }

        public COLLADAGeometryLibrary()
        {
            Geometries = new List<COLLADAGeometry>();
        }
    }
}
