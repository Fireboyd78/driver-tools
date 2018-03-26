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
    public enum COLLADAParamType
    {
        Float,
        Float2,
        Float3,
        Float4,
        Surface,
        Sampler2D,
    }

    public abstract class COLLADAParamValue : COLLADABaseType
    {
        public abstract Type ObjectType { get; }

        public abstract COLLADAParamType ValueType { get; }

        public abstract object GetValue();
        public abstract bool SetValue(object value);

        public bool TryGetValue<T>(out T result)
            where T : struct
        {
            if (typeof(T) == ObjectType)
            {
                result = (T)GetValue();
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }
    }

    public abstract class COLLADAParamValueBinder<T> : COLLADAParamValue
    {
        protected T m_value;

        public override Type ObjectType => typeof(T);

        public override object GetValue()
        {
            return m_value;
        }

        public override bool SetValue(object value)
        {
            if (value.GetType() == ObjectType)
            {
                m_value = (T)value;
                return true;
            }
            return false;
        }

        public T Value
        {
            get { return (T)m_value; }
            set { m_value = value; }
        }
    }

    public class COLLADAFloatParamValue : COLLADAParamValueBinder<float>
    {
        protected override string TagName => "float";

        public override COLLADAParamType ValueType => COLLADAParamType.Float;

        public override void LoadXml(XmlElement node)
        {
            Value = float.Parse(node.InnerText);
        }

        public override void SaveXml(XmlElement node)
        {
            node.InnerText = Value.ToString();
        }
    }

    public class COLLADAFloat2ParamValue : COLLADAParamValueBinder<Vector2>
    {
        protected override string TagName => "float2";

        public override COLLADAParamType ValueType => COLLADAParamType.Float2;

        public override void LoadXml(XmlElement node)
        {
            var s = node.InnerText.Split(' ');

            Value = new Vector2() {
                X = float.Parse(s[0]),
                Y = float.Parse(s[1]),
            };
        }

        public override void SaveXml(XmlElement node)
        {
            node.InnerText = $"{Value.X} {Value.Y}";
        }
    }

    public class COLLADAFloat3ParamValue : COLLADAParamValueBinder<Vector3>
    {
        protected override string TagName => "float3";

        public override COLLADAParamType ValueType => COLLADAParamType.Float3;

        public override void LoadXml(XmlElement node)
        {
            var s = node.InnerText.Split(' ');

            Value = new Vector3() {
                X = float.Parse(s[0]),
                Y = float.Parse(s[1]),
                Z = float.Parse(s[2]),
            };
        }

        public override void SaveXml(XmlElement node)
        {
            node.InnerText = $"{Value.X} {Value.Y} {Value.Z}";
        }
    }

    public class COLLADAFloat4ParamValue : COLLADAParamValueBinder<Vector4>
    {
        protected override string TagName => "float4";

        public override COLLADAParamType ValueType => COLLADAParamType.Float4;

        public override void LoadXml(XmlElement node)
        {
            var s = node.InnerText.Split(' ');

            Value = new Vector4() {
                X = float.Parse(s[0]),
                Y = float.Parse(s[1]),
                Z = float.Parse(s[2]),
                W = float.Parse(s[3]),
            };
        }

        public override void SaveXml(XmlElement node)
        {
            node.InnerText = $"{Value.X} {Value.Y} {Value.Z} {Value.W}";
        }
    }

    public class COLLADASurfaceParamValue : COLLADAParamValueBinder<COLLADASurface>
    {
        protected override string TagName => "surface";

        public override COLLADAParamType ValueType => COLLADAParamType.Surface;

        public override void LoadXml(XmlElement node)
        {
            Value = COLLADAFactory.CreateFromXml<COLLADASurface>(node);
        }

        public override void SaveXml(XmlElement node)
        {
            Value.SaveXml(node);
        }
    }

    public class COLLADASampler2DParamValue : COLLADAParamValueBinder<COLLADASampler2D>
    {
        protected override string TagName => "sampler2D";

        public override COLLADAParamType ValueType => COLLADAParamType.Sampler2D;

        public override void LoadXml(XmlElement node)
        {
            Value = COLLADAFactory.CreateFromXml<COLLADASampler2D>(node);
        }

        public override void SaveXml(XmlElement node)
        {
            Value.SaveXml(node);
        }
    }

    public class COLLADACommonProfile : COLLADABaseType
    {
        public class CommonNewParam : COLLADABaseType
        {
            protected override string TagName => "newparam";

            public string SubId;

            public string Semantic;

            public COLLADAParamValue Param;

            public override void LoadXml(XmlElement node)
            {
                SubId = node.GetAttribute("sid");
                Semantic = node.GetAttribute("semantic");

                var children = node.ChildNodes;

                if (children.Count != 1)
                    throw new COLLADAXmlException(node, "malformed newparam: expected exactly ONE child!");

                var child = children[0] as XmlElement;

                switch (child.Name)
                {
                case "float":
                    Param = new COLLADAFloatParamValue();
                    break;
                case "float2":
                    Param = new COLLADAFloat2ParamValue();
                    break;
                case "float3":
                    Param = new COLLADAFloat3ParamValue();
                    break;
                case "float4":
                    Param = new COLLADAFloat4ParamValue();
                    break;
                case "surface":
                    Param = new COLLADASurfaceParamValue();
                    break;
                case "sampler2D":
                    Param = new COLLADASampler2DParamValue();
                    break;
                }

                Param.LoadXml(child);
            }

            public override void SaveXml(XmlElement node)
            {
                node.SetAttribute("sid", SubId);

                if (!String.IsNullOrEmpty(Semantic))
                    node.SetAttribute("semantic", Semantic);

                Param.WriteTo(node);
            }
        }

        public class CommonTechnique : COLLADAEffectTechnique
        {
            public List<CommonNewParam> Params;
            public COLLADAShader Shader;

            public override void LoadXml(XmlElement node)
            {
                base.LoadXml(node);

                Params = new List<CommonNewParam>();

                foreach (var child in node.ChildNodes.OfType<XmlElement>())
                {
                    switch (child.Name)
                    {
                    case "newparam":
                        var param = COLLADAFactory.CreateFromXml<CommonNewParam>(child);
                        Params.Add(param);
                        break;
                    case "blinn":
                    case "lambert":
                    case "phong":
                        Shader = new COLLADAShader(child);
                        break;
                    }
                }
            }

            public override void SaveXml(XmlElement node)
            {
                base.SaveXml(node);

                foreach (var param in Params)
                    param.WriteTo(node);

                Shader.WriteTo(node);
            }
        }

        protected override string TagName => "profile_COMMON";
        
        public string Id;

        public List<CommonNewParam> Params;
        public CommonTechnique Technique;

        public override void LoadXml(XmlElement node)
        {
            Id = node.GetAttribute("id");

            Params = new List<CommonNewParam>();

            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                switch (child.Name)
                {
                case "newparam":
                    var param = COLLADAFactory.CreateFromXml<CommonNewParam>(child);
                    Params.Add(param);
                    break;
                case "technique":
                    Technique = COLLADAFactory.CreateFromXml<CommonTechnique>(child);
                    break;
                }
            }
        }

        public override void SaveXml(XmlElement node)
        {
            node.SetAttribute("id", Id);

            foreach (var param in Params)
                param.WriteTo(node);

            Technique.WriteTo(node);
        }
    }
}
