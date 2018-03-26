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
    public struct COLLADANodeInformation
    {
        public string Id;
        public string Name;

        public void LoadXml(XmlElement node)
        {
            Id = node.GetAttribute("id");
            Name = node.GetAttribute("name");
        }

        public void SaveXml(XmlElement node)
        {
            node.SetAttribute("id", Id);
            node.SetAttribute("name", Name);
        }
    }

    public abstract class COLLADABaseType
    {
        protected abstract string TagName { get; }

        public abstract void LoadXml(XmlElement node);
        public abstract void SaveXml(XmlElement node);

        public virtual void WriteTo(XmlElement parent)
        {
            var xmlDoc = parent.OwnerDocument;
            var node = xmlDoc.CreateElement(TagName);

            SaveXml(node);

            parent.AppendChild(node);
        }
    }

    public static class COLLADAFactory
    {
        public static T CreateFromXml<T>(XmlElement node)
            where T : COLLADABaseType, new()
        {
            var result = new T();

            result.LoadXml(node);

            return result;
        }
    }

    public class COLLADAException : Exception { }
    
    public class COLLADABadVersionException : COLLADAException
    {
        public string Version { get; }

        public override string Message
        {
            get { return $"COLLADA version {Version} is currently not supported."; }
        }

        public COLLADABadVersionException(string version)
        {
            Version = version;
        }
    }

    public class COLLADAInvalidFileTypeException : COLLADAException
    {
        public override string Message
        {
            get { return "Document is not a COLLADA file!"; }
        }
    }

    public class COLLADAXmlException : COLLADAException
    {
        public XmlElement Element { get; }

        public string Reason { get; }

        public override string Message
        {
            get { return $"Unable to load XML element '{Element.Name}' -- {Reason}"; }
        }
        
        public COLLADAXmlException(XmlElement element, string reason)
        {
            Element = element;
            Reason = reason;
        }
    }
}
