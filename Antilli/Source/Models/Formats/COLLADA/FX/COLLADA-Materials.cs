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
    public class COLLADAMateral : COLLADABaseType
    {
        protected override string TagName => "material";

        public COLLADANodeInformation Info;

        public COLLADAEffectInstance EffectInstance;

        public override void LoadXml(XmlElement node)
        {
            Info.LoadXml(node);

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "instance_effect":
                    EffectInstance = COLLADAFactory.CreateFromXml<COLLADAEffectInstance>(child);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            Info.SaveXml(node);
            EffectInstance.WriteTo(node);
        }
    }

    public class COLLADAMaterialBinding : COLLADABaseType
    {
        public class CommonTechnique : COLLADABaseType
        {
            protected override string TagName => "technique_common";

            public List<COLLADAMaterialInstance> Materials;

            public override void LoadXml(XmlElement node)
            {
                Materials = new List<COLLADAMaterialInstance>();

                foreach (var child in node.ChildNodes.OfType<XmlElement>())
                {
                    switch (child.Name)
                    {
                    case "instance_material":
                        var material = COLLADAFactory.CreateFromXml<COLLADAMaterialInstance>(child);
                        Materials.Add(material);
                        break;
                    }
                }
            }

            public override void SaveXml(XmlElement node)
            {
                foreach (var material in Materials)
                    material.WriteTo(node);
            }
        }

        protected override string TagName => "bind_material";

        public CommonTechnique Technique;

        public override void LoadXml(XmlElement node)
        {
            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "technique_common":
                    Technique = COLLADAFactory.CreateFromXml<CommonTechnique>(child);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            Technique.WriteTo(node);
        }
    }

    public class COLLADAMaterialInstance : COLLADABaseType
    {
        protected override string TagName => "instance_material";

        public string SubId;
        public string Name;
        public string Target;
        public string Symbol;

        public override void LoadXml(XmlElement node)
        {
            SubId = node.GetAttribute("sid");
            Name = node.GetAttribute("name");

            Target = node.GetAttribute("target");
            Symbol = node.GetAttribute("symbol");
        }

        public override void SaveXml(XmlElement node)
        {
            if (!String.IsNullOrEmpty(SubId))
                node.SetAttribute("sid", SubId);
            if (!String.IsNullOrEmpty(Name))
                node.SetAttribute("name", Name);

            node.SetAttribute("target", Target);
            node.SetAttribute("symbol", Symbol);
        }
    }

    public class COLLADAMaterialLibrary : COLLADABaseType
    {
        protected override string TagName => "library_materials";

        public List<COLLADAMateral> Materials;

        public override void LoadXml(XmlElement node)
        {
            Materials = new List<COLLADAMateral>();

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "material":
                    var material = COLLADAFactory.CreateFromXml<COLLADAMateral>(child);
                    Materials.Add(material);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            foreach (var material in Materials)
                material.WriteTo(node);
        }
    }
}
