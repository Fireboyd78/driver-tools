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
    public class COLLADATechniqueCommon : COLLADABaseType
    {
        protected override string TagName
        {
            get { return "techique_common"; }
        }

        private COLLADASource m_parent;

        public COLLADAAccessor Accessor;

        public COLLADASource Parent
        {
            get
            {
                if (m_parent == null)
                    throw new InvalidOperationException("Parent source not set!");

                return m_parent;
            }
            set { m_parent = value; }
        }

        public override void LoadXml(XmlElement node)
        {
            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "accessor":
                    var source = child.GetAttribute("source").Substring(1); // remove '#'

                    if (Parent.Array.Info.Id != source)
                        throw new InvalidOperationException("Can't find accessor source!");

                    Accessor = new COLLADAAccessor() {
                        Source = Parent.Array
                    };

                    Accessor.LoadXml(child);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            Accessor.WriteTo(node);
        }

        public COLLADATechniqueCommon()
        {
            Accessor = new COLLADAAccessor();
        }
    }
}
