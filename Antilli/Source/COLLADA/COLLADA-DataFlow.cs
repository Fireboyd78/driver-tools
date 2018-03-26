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
    public abstract class COLLADAArray<T> : COLLADABaseType
        where T : struct, IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
    {
        public COLLADANodeInformation Info;

        public List<T> Data;

        public int Count
        {
            get { return Data.Count; }
            set
            {
                Data = new List<T>();
            }
        }

        public COLLADAArray()
        {
            Data = new List<T>();
        }

        public COLLADAArray(int count)
        {
            Data = new List<T>(count);
        }
    }

    public class COLLADAFloatArray : COLLADAArray<float>
    {
        protected override string TagName => "float_array";

        public override void LoadXml(XmlElement node)
        {
            Info.LoadXml(node);

            Count = int.Parse(node.GetAttribute("count"), NumberStyles.Integer);

            foreach (var aryVal in node.InnerText.Split(' '))
            {
                var val = float.Parse(aryVal);
                Data.Add(val);
            }
        }

        public override void SaveXml(XmlElement node)
        {
            Info.SaveXml(node);
            node.InnerText = String.Join(" ", Data.ToArray());
        }
    }

    public class COLLADAParam : COLLADABaseType
    {
        protected override string TagName => "param";

        public string Name;
        public string SubId;
        public string Type;
        public string Semantic;

        public override void LoadXml(XmlElement node)
        {
            Name = node.GetAttribute("name");
            SubId = node.GetAttribute("sid");
            Type = node.GetAttribute("type");
            Semantic = node.GetAttribute("semantic");
        }

        public override void SaveXml(XmlElement node)
        {
            node.SetAttribute("name", Name);

            if (!String.IsNullOrEmpty(SubId))
                node.SetAttribute("sid", SubId);

            node.SetAttribute("type", Type);
            node.SetAttribute("semantic", Semantic);
        }
    }

    public class COLLADAAccessor : COLLADABaseType
    {
        protected override string TagName => "accessor";

        private COLLADAFloatArray m_source;

        public COLLADAFloatArray Source
        {
            get
            {
                if (m_source == null)
                    throw new InvalidOperationException("Source was never set!");

                return m_source;
            }
            set { m_source = value; }
        }

        public int Count;
        public int Offset;
        public int Stride;

        public List<COLLADAParam> Params;

        public override void LoadXml(XmlElement node)
        {
            Count = int.Parse(node.GetAttribute("count"), NumberStyles.Integer);

            if (node.HasAttribute("offset"))
                Offset = int.Parse(node.GetAttribute("offset"), NumberStyles.Integer);

            Stride = int.Parse(node.GetAttribute("stride"), NumberStyles.Integer);

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "param":
                    var param = new COLLADAParam();
                    param.LoadXml(child);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            var xmlDoc = node.OwnerDocument;

            node.SetAttribute("source", $"#{Source.Info.Id}");
            node.SetAttribute("count", Count.ToString());

            if (Offset != 0)
                node.SetAttribute("offset", Offset.ToString());

            node.SetAttribute("stride", Stride.ToString());

            foreach (var param in Params)
                param.WriteTo(node);
        }

        public COLLADAAccessor()
        {
            Params = new List<COLLADAParam>();
        }
    }

    public class COLLADASource : COLLADABaseType
    {
        protected override string TagName => "source";
    
        public COLLADANodeInformation Info;

        public COLLADAFloatArray Array;
        public COLLADATechniqueCommon Technique;

        public override void LoadXml(XmlElement node)
        {
            Info.LoadXml(node);

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "float_array":
                    Array.LoadXml(child);
                    break;
                case "technique_common":
                    Technique.LoadXml(child);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            Info.SaveXml(node);

            Array.WriteTo(node);
            Technique.WriteTo(node);
        }

        public COLLADASource()
        {
            Array = new COLLADAFloatArray();

            Technique = new COLLADATechniqueCommon() {
                Parent = this
            };
        }
    }

    public class COLLADAInput : COLLADABaseType
    {
        protected override string TagName => "input";

        private COLLADASource m_source;

        public string Semantic;

        public COLLADASource Source
        {
            get
            {
                if (m_source == null)
                    throw new InvalidOperationException("Source was never set!");

                return m_source;
            }
            set { m_source = value; }
        }

        public override void LoadXml(XmlElement node)
        {
            Semantic = node.GetAttribute("semantic");
        }

        public override void SaveXml(XmlElement node)
        {
            node.SetAttribute("semantic", Semantic.ToString());
            node.SetAttribute("source", $"#{Source.Info.Id}");
        }
    }

    public class COLLADASharedInput : COLLADAInput
    {
        public int Offset;
        public int Set;

        public override void LoadXml(XmlElement node)
        {
            base.LoadXml(node);

            Offset = int.Parse(node.GetAttribute("offset"));

            var set = node.GetAttribute("set");

            Set = (String.IsNullOrEmpty(set)) ? -1 : int.Parse(set);
        }

        public override void SaveXml(XmlElement node)
        {
            base.SaveXml(node);

            node.SetAttribute("offset", Offset.ToString());

            if (Set != -1)
                node.SetAttribute("set", Set.ToString());
        }
    }
}
