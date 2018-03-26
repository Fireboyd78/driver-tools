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
    public class COLLADASceneNode : COLLADABaseType
    {
        protected override string TagName => "node";

        public COLLADANodeInformation Info;

        public string Type;

        public COLLADAMatrix Matrix;

        public List<COLLADAGeometryInstance> GeometryInstances;

        public List<COLLADASceneNode> Children;

        public override void LoadXml(XmlElement node)
        {
            Info.LoadXml(node);

            Type = node.GetAttribute("type");

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "matrix":
                    var matrix = new COLLADAMatrix();

                    matrix.LoadXml(child);
                    Matrix = matrix;

                    break;
                case "instance_geometry":
                    var geomInstance = new COLLADAGeometryInstance();

                    geomInstance.LoadXml(child);
                    GeometryInstances.Add(geomInstance);

                    break;
                case "node":
                    var childNode = new COLLADASceneNode();

                    childNode.LoadXml(child);
                    Children.Add(childNode);

                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            Info.SaveXml(node);

            node.SetAttribute("type", Type);

            if (Matrix != null)
                Matrix.WriteTo(node);

            foreach (var geomInstance in GeometryInstances)
                geomInstance.WriteTo(node);

            foreach (var child in Children)
                child.WriteTo(node);
        }

        public COLLADASceneNode()
        {
            GeometryInstances = new List<COLLADAGeometryInstance>();
            Children = new List<COLLADASceneNode>();
        }
    }

    public class COLLADAVisualScene : COLLADABaseType
    {
        protected override string TagName => "visual_scene";

        public COLLADANodeInformation Info;

        public List<COLLADASceneNode> Nodes;

        public override void LoadXml(XmlElement node)
        {
            Info.LoadXml(node);

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "node":
                    var sceneNode = new COLLADASceneNode();

                    sceneNode.LoadXml(child);
                    Nodes.Add(sceneNode);

                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            Info.SaveXml(node);

            foreach (var child in Nodes)
                child.WriteTo(node);
        }

        public COLLADAVisualScene()
        {
            Nodes = new List<COLLADASceneNode>();
        }
    }

    public class COLLADAVisualSceneInstance : COLLADABaseType
    {
        protected override string TagName => "instance_visual_scene";

        public string Name;
        public string SubId;
        public string URL;

        public override void LoadXml(XmlElement node)
        {
            SubId = node.GetAttribute("sid");
            Name = node.GetAttribute("name");
            URL = node.GetAttribute("url");
        }

        public override void SaveXml(XmlElement node)
        {
            node.SetAttribute("sid", SubId);
            node.SetAttribute("name", Name);
            node.SetAttribute("url", URL);
        }
    }

    public class COLLADAVisualSceneLibrary : COLLADABaseType
    {
        protected override string TagName => "library_visual_scenes";

        public COLLADANodeInformation Info;

        public List<COLLADAVisualScene> Scenes;

        public override void LoadXml(XmlElement node)
        {
            Info.LoadXml(node);

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "visual_scene":
                    var scene = new COLLADAVisualScene();

                    scene.LoadXml(child);
                    Scenes.Add(scene);

                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            Info.SaveXml(node);

            foreach (var scene in Scenes)
                scene.WriteTo(node);
        }

        public COLLADAVisualSceneLibrary()
        {
            Scenes = new List<COLLADAVisualScene>();
        }
    }

    public class COLLADAScene : COLLADABaseType
    {
        protected override string TagName => "scene";

        public COLLADAVisualSceneInstance SceneInstance;

        public override void LoadXml(XmlElement node)
        {
            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "instance_visual_scene":
                    var sceneInstance = new COLLADAVisualSceneInstance();

                    sceneInstance.LoadXml(child);
                    SceneInstance = sceneInstance;

                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            SceneInstance.WriteTo(node);
        }
    }
}
