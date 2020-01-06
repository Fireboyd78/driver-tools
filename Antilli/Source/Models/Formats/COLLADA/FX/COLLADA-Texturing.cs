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
    public class COLLADAGenericTag : COLLADABaseType
    {
        private string m_tagName;

        protected override string TagName => m_tagName;

        public string Value;

        public override void LoadXml(XmlElement node)
        {
            Value = node.InnerText;
        }

        public override void SaveXml(XmlElement node)
        {
            node.InnerText = Value;
        }

        public COLLADAGenericTag(string tagName)
        {
            m_tagName = tagName;
        }

        public COLLADAGenericTag(string tagName, string value)
        {
            m_tagName = tagName;
            Value = value;
        }
    }
    
    public class COLLADAImage : COLLADABaseType
    {
        protected override string TagName => "image";

        public string Id;
        public string Name;

        public string InitFrom;

        public override void LoadXml(XmlElement node)
        {
            Id = node.GetAttribute("id");
            Name = node.GetAttribute("name");

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "init_from":
                    InitFrom = child.InnerText;
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            var xmlDoc = node.OwnerDocument;

            node.SetAttribute("id", Id);
            node.SetAttribute("name", Name);

            var initFrom = new COLLADAGenericTag("init_from", InitFrom);
            initFrom.WriteTo(node);
        }
    }
    public class COLLADASurface : COLLADABaseType
    {
        protected override string TagName => "surface";

        public string Type;
        public string InitFrom;

        public override void LoadXml(XmlElement node)
        {
            Type = node.GetAttribute("type");

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "init_from":
                    InitFrom = child.InnerText;
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            node.SetAttribute("type", Type);

            var initFrom = new COLLADAGenericTag("init_from", InitFrom);
            initFrom.WriteTo(node);
        }
    }

    public class COLLADASampler2D : COLLADABaseType
    {
        protected override string TagName => "sampler2D";

        public string Source;

        public override void LoadXml(XmlElement node)
        {
            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "source":
                    Source = child.InnerText;
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            var source = new COLLADAGenericTag("source", Source);
            source.WriteTo(node);
        }
    }

    public class COLLADAImageLibrary : COLLADABaseType
    {
        protected override string TagName => "library_images";

        public List<COLLADAImage> Images;

        public override void LoadXml(XmlElement node)
        {
            Images = new List<COLLADAImage>();

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "image":
                    var image = COLLADAFactory.CreateFromXml<COLLADAImage>(child);
                    Images.Add(image);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            foreach (var image in Images)
                image.WriteTo(node);
        }
    }
}
