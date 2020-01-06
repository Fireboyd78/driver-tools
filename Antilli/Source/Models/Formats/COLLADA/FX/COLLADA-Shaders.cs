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
    public interface ICOLLADAParamValue
    {
        string TagName { get; }

        bool Required { get; }
        
        void LoadXml(XmlElement node);
        void SaveXml(XmlElement node);
    }

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

    public struct COLLADAParamValue<T> : ICOLLADAParamValue
        where T : COLLADABaseType, new()
    {
        public string Name;
        public T Value;

        public bool Required;

        string ICOLLADAParamValue.TagName => Name;
        bool ICOLLADAParamValue.Required => Required;
        
        public void LoadXml(XmlElement node)
        {
            var child = node.ChildNodes[0] as XmlElement;

            Value = COLLADAFactory.CreateFromXml<T>(child);
        }

        public void SaveXml(XmlElement node)
        {
            if (Required || (Value != null))
            {
                if (Value == null)
                    throw new Exception($"Required param '{Name}' is null.");

                Value.WriteTo(node);
            }
        }
        
        public COLLADAParamValue(string name, bool required = false)
        {
            Name = name;
            Required = required;

            Value = null;
        }
    }

    public enum COLLADAOpaqueType
    {
        None,

        AlphaOne,
        RGBZero,
    }

    public struct COLLADATransparent : ICOLLADAParamValue
    {
        string ICOLLADAParamValue.TagName => "transparent";
        bool ICOLLADAParamValue.Required => false;

        public COLLADAOpaqueType Opaque;

        public COLLADAParamValue<COLLADAColorOrTexture> Value;
        
        public void LoadXml(XmlElement node)
        {
            var opaqueType = node.GetAttribute("opaque");

            switch (opaqueType)
            {
            case "A_ONE":
                Opaque = COLLADAOpaqueType.AlphaOne;
                break;
            case "RGB_ZERO":
                Opaque = COLLADAOpaqueType.RGBZero;
                break;
            }

            Value.LoadXml(node);
        }

        public void SaveXml(XmlElement node)
        {
            if (Opaque != COLLADAOpaqueType.None)
            {
                var opaqueType = String.Empty;

                switch (Opaque)
                {
                case COLLADAOpaqueType.AlphaOne:
                    opaqueType = "A_ONE";
                    break;
                case COLLADAOpaqueType.RGBZero:
                    opaqueType = "RGB_ZERO";
                    break;
                }

                node.SetAttribute("opaque", opaqueType);
            }

            Value.SaveXml(node);
        }
    }
    
    public class COLLADAShader : COLLADABaseType
    {
        private string m_tagName;
        protected override string TagName => m_tagName;

        public COLLADAParamValue<COLLADAColorOrTexture> Emission;
        public COLLADAParamValue<COLLADAColorOrTexture> Ambient;
        public COLLADAParamValue<COLLADAColorOrTexture> Diffuse;
        public COLLADAParamValue<COLLADAColorOrTexture> Specular;

        public COLLADAParamValue<COLLADAFloatOrParam> Shininess;

        public COLLADATransparent Transparent;
        public COLLADAParamValue<COLLADAFloatOrParam> Transparency;

        public COLLADAParamValue<COLLADAFloatOrParam> IndexOfRefraction;

        protected void InitParam<T>(ref COLLADAParamValue<T> value, string name, bool required = false)
            where T : COLLADABaseType, new()
        {
            value = new COLLADAParamValue<T>(name, required);
        }
        
        protected void LoadParam<T>(XmlElement node, ref T value)
            where T : ICOLLADAParamValue
        {
            value.LoadXml(node);
        }

        protected void LoadParam<T>(IEnumerable<XmlElement> nodes, ref T value)
            where T : ICOLLADAParamValue
        {
            try
            {
                var name = value.TagName;
                var node = nodes.First((n) => n.Name == name);

                LoadParam(node, ref value);
            }
            catch (InvalidOperationException e)
            {
                if (value.Required)
                    throw new InvalidOperationException($"Required param '{value.TagName}' not found.", e);
            }
        }

        protected void SaveParam<T>(XmlElement node, T value)
            where T : ICOLLADAParamValue
        {
            var xmlDoc = node.OwnerDocument;
            var child = xmlDoc.CreateElement(value.TagName);

            value.SaveXml(node);

            node.AppendChild(node);
        }
        
        public override void LoadXml(XmlElement node)
        {
            var children = node.ChildNodes.OfType<XmlElement>();

            LoadParam(children, ref Emission);
            LoadParam(children, ref Ambient);
            LoadParam(children, ref Diffuse);
            LoadParam(children, ref Specular);
            LoadParam(children, ref Shininess);
            LoadParam(children, ref Transparent);
            LoadParam(children, ref Transparency);
            LoadParam(children, ref IndexOfRefraction);
        }

        public override void SaveXml(XmlElement node)
        {
            SaveParam(node, Emission);
            SaveParam(node, Ambient);
            SaveParam(node, Diffuse);
            SaveParam(node, Specular);
            SaveParam(node, Shininess);
            SaveParam(node, Transparent);
            SaveParam(node, Transparency);
            SaveParam(node, IndexOfRefraction);
        }

        protected COLLADAShader()
        {
            InitParam(ref Emission, "emission");
            InitParam(ref Ambient, "ambient");
            InitParam(ref Diffuse, "diffuse");
            InitParam(ref Specular, "specular");
            InitParam(ref Shininess, "shininess");
            InitParam(ref Transparency, "trasparency");
            InitParam(ref IndexOfRefraction, "index_of_refraction");
        }

        public COLLADAShader(string tagName)
            : this()
        {
            m_tagName = tagName;
        }

        public COLLADAShader(XmlElement node)
            : this()
        {
            m_tagName = node.Name;
            LoadXml(node);
        }
    }
}
