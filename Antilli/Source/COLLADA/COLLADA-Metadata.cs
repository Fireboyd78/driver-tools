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
    public class COLLADADocument : COLLADABaseType
    {
        protected override string TagName => "COLLADA";

        public string XmlNs;
        public string Version;
        public string Base;

        public COLLADAImageLibrary ImageLibrary;
        public COLLADAEffectLibrary EffectLibrary;
        public COLLADAMaterialLibrary MaterialLibrary;

        public COLLADAGeometryLibrary GeometryLibrary;
        public COLLADAVisualSceneLibrary SceneLibrary;

        public COLLADAScene Scene;

        public override void LoadXml(XmlElement node)
        {
            XmlNs = node.GetAttribute("xmlns");
            Version = node.GetAttribute("version");
            Base = node.GetAttribute("base");

            bool supported = false;

            switch (Version)
            {
            case "1.4.0":
            case "1.4.1":
                supported = true;
                break;
            }

            if (!supported)
                throw new COLLADABadVersionException(Version);

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "library_images":
                    ImageLibrary = COLLADAFactory.CreateFromXml<COLLADAImageLibrary>(child);
                    break;
                case "library_effects":
                    EffectLibrary = COLLADAFactory.CreateFromXml<COLLADAEffectLibrary>(child);
                    break;
                case "library_materials":
                    MaterialLibrary = COLLADAFactory.CreateFromXml<COLLADAMaterialLibrary>(child);
                    break;
                case "library_geometries":
                    GeometryLibrary = COLLADAFactory.CreateFromXml<COLLADAGeometryLibrary>(child);
                    break;
                case "library_visual_scenes":
                    SceneLibrary = COLLADAFactory.CreateFromXml<COLLADAVisualSceneLibrary>(child);
                    break;
                case "scene":
                    Scene = COLLADAFactory.CreateFromXml<COLLADAScene>(child);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            node.SetAttribute("xmlns", XmlNs);
            node.SetAttribute("version", Version);

            if (!String.IsNullOrEmpty(Base))
                node.SetAttribute("base", Base);

            ImageLibrary.WriteTo(node);
            EffectLibrary.WriteTo(node);
            MaterialLibrary.WriteTo(node);
            GeometryLibrary.WriteTo(node);
            SceneLibrary.WriteTo(node);
            Scene.WriteTo(node);
        }

        public COLLADADocument()
        {
            XmlNs = "http://www.collada.org/2005/11/COLLADASchema";
            Version = "1.4.1";
        }

        public COLLADADocument(string filename)
        {
            var xmlDoc = new XmlDocument();

            xmlDoc.Load(filename);

            var root = xmlDoc.DocumentElement;

            if (root.Name != "COLLADA")
                throw new COLLADAInvalidFileTypeException();

            LoadXml(root);
        }
    }
}
