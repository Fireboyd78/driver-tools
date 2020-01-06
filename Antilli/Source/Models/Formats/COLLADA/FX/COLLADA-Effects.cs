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
    public class COLLADAEffect : COLLADABaseType
    {
        protected override string TagName => "effect";

        public COLLADANodeInformation Info;

        public COLLADACommonProfile Profile;

        public override void LoadXml(XmlElement node)
        {
            Info.LoadXml(node);

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "profile_CG":
                case "profile_GLES":
                case "profile_GLSL":
                    throw new COLLADAXmlException(child, "Unsupported profile type.");
                case "profile_COMMON":
                    Profile = COLLADAFactory.CreateFromXml<COLLADACommonProfile>(child);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            throw new NotImplementedException();
        }
    }

    public class COLLADAEffectTechnique : COLLADABaseType
    {
        protected override string TagName => "technique";

        public string Id;
        public string SubId;

        public override void LoadXml(XmlElement node)
        {
            Id = node.GetAttribute("id");
            SubId = node.GetAttribute("sid");
        }

        public override void SaveXml(XmlElement node)
        {
            node.SetAttribute("id", Id);

            if (!String.IsNullOrEmpty(SubId))
                node.SetAttribute("sid", SubId);
        }
    }

    public class COLLADAEffectInstance : COLLADABaseType
    {
        protected override string TagName => "instance_effect";

        public string SubId;
        public string Name;
        public string Url;

        public override void LoadXml(XmlElement node)
        {
            SubId = node.GetAttribute("sid");
            Name = node.GetAttribute("name");
            Url = node.GetAttribute("url");
        }

        public override void SaveXml(XmlElement node)
        {
            if (!String.IsNullOrEmpty(SubId))
                node.SetAttribute("sid", SubId);
            if (!String.IsNullOrEmpty(Name))
                node.SetAttribute("name", Name);

            node.SetAttribute("url", Url);
        }
    }

    public class COLLADAEffectLibrary : COLLADABaseType
    {
        protected override string TagName => "library_effects";

        public List<COLLADAEffect> Effects;

        public override void LoadXml(XmlElement node)
        {
            Effects = new List<COLLADAEffect>();

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "effect":
                    var effect = COLLADAFactory.CreateFromXml<COLLADAEffect>(child);
                    Effects.Add(effect);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            foreach (var effect in Effects)
                effect.WriteTo(node);
        }
    }
}
