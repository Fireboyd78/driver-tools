using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Xml
{
    public static class XmlExtensions
    {
        public static XmlElement AddAttribute(this XmlElement @this, string name, string value)
        {
            @this.SetAttributeNode(name, "").Value = value;
            return @this;
        }

        public static XmlElement AddAttributeIf(this XmlElement @this, string name, string value, bool condition)
        {
            return (condition) ? @this.AddAttribute(name, value) : @this;
        }

        public static XmlElement AddAttribute(this XmlElement @this, string name, object value)
        {
            return @this.AddAttribute(name, value.ToString());
        }

        public static XmlElement AddAttributeIf(this XmlElement @this, string name, object value, bool condition)
        {
            return (condition) ? @this.AddAttribute(name, value) : @this;
        }

        public static XmlElement AddElement(this XmlDocument @this, string name)
        {
            var node = @this.CreateElement(name);
            @this.AppendChild(node);

            return node;
        }

        public static XmlElement AddElement(this XmlElement @this, string name)
        {
            var node = @this.OwnerDocument.CreateElement(name);
            @this.AppendChild(node);

            return node;
        }

        public static XmlElement AddElementIf(this XmlElement @this, string name, bool condition)
        {
            return (condition) ? @this.AddElement(name) : @this;
        }
    }
}
