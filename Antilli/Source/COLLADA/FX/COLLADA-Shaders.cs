using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.Linq;

using DSCript;

namespace COLLADA
{
    public class COLLADAColorOrTexture : COLLADABaseType
    {
        public string SubId;

        public Vector4 Color;

        public string Texture;

        protected bool IsColor
        {
            get { return String.IsNullOrEmpty(Texture); }
        }

        protected override string TagName
        {
            get { return (IsColor) ? "color" : "texture"; }
        }

        public override void LoadXml(XmlElement node)
        {
            switch (node.Name)
            {
            case "color":
                SubId = node.GetAttribute("sid");

                var s = node.InnerText.Split(' ');
                Color = new Vector4() {
                    X = float.Parse(s[0]),
                    Y = float.Parse(s[1]),
                    Z = float.Parse(s[2]),
                    W = float.Parse(s[3]),
                };
                break;
            case "texture":
                Texture = node.GetAttribute("texture");
                break;
            }
        }

        public override void SaveXml(XmlElement node)
        {
            if (IsColor)
            {
                node.SetAttribute("sid", SubId);
                node.InnerText = $"{Color.X} {Color.Y} {Color.Z} {Color.W}";
            }
            else
            {
                node.SetAttribute("texture", Texture);
            }
        }
    }

    public class COLLADAFloatOrParam : COLLADABaseType
    {
        public string SubId;
        public float Value;

        public string Param;

        public bool IsFloat
        {
            get { return String.IsNullOrEmpty(Param); }
        }

        protected override string TagName
        {
            get { return (IsFloat) ? "float" : "param"; }
        }

        public override void LoadXml(XmlElement node)
        {
            switch (node.Name)
            {
            case "float":
                SubId = node.GetAttribute("sid");
                Value = float.Parse(node.InnerText);
                break;
            case "param":
                Param = node.GetAttribute("ref");
                break;
            }
        }

        public override void SaveXml(XmlElement node)
        {
            if (IsFloat)
            {
                node.SetAttribute("sid", SubId);
                node.InnerText = Value.ToString();
            }
            else
            {
                node.SetAttribute("ref", Param);
            }
        }
    }

    public class COLLADAShader : COLLADABaseType
    {
        private string m_tagName;
        protected override string TagName => m_tagName;

        public COLLADAColorOrTexture Emission;
        public COLLADAColorOrTexture Ambient;
        public COLLADAColorOrTexture Diffuse;
        public COLLADAColorOrTexture Specular;

        public COLLADAFloatOrParam Shininess;
        public COLLADAFloatOrParam IndexOfRefraction;

        protected T LoadChildTag<T>(XmlElement node)
            where T : COLLADABaseType, new()
        {
            var child = node.ChildNodes[0] as XmlElement;
            return COLLADAFactory.CreateFromXml<T>(child);
        }

        protected void SaveChildTag<T>(XmlElement node, string name, T child)
            where T : COLLADABaseType
        {
            var xmlDoc = node.OwnerDocument;

            var elem = xmlDoc.CreateElement(name);
            child.WriteTo(elem);

            node.AppendChild(elem);
        }
        
        public override void LoadXml(XmlElement node)
        {
            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "emission":
                    Emission = LoadChildTag<COLLADAColorOrTexture>(child);
                    break;
                case "ambient":
                    Ambient = LoadChildTag<COLLADAColorOrTexture>(child);
                    break;
                case "diffuse":
                    Diffuse = LoadChildTag<COLLADAColorOrTexture>(child);
                    break;
                case "specular":
                    Specular = LoadChildTag<COLLADAColorOrTexture>(child);
                    break;
                case "shininess":
                    Shininess = LoadChildTag<COLLADAFloatOrParam>(child);
                    break;
                /*
                    TODO: transparent?
                */
                case "index_of_refraction":
                    IndexOfRefraction = LoadChildTag<COLLADAFloatOrParam>(child);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            SaveChildTag(node, "emission", Emission);
            SaveChildTag(node, "ambient", Ambient);
            SaveChildTag(node, "diffuse", Diffuse);
            SaveChildTag(node, "specular", Specular);
            SaveChildTag(node, "shininess", Shininess);
            // TODO: transparent?
            SaveChildTag(node, "index_of_refraction", IndexOfRefraction);
        }

        public COLLADAShader(string tagName)
        {
            m_tagName = tagName;
        }

        public COLLADAShader(XmlElement node)
        {
            m_tagName = node.Name;
            LoadXml(node);
        }
    }
}
