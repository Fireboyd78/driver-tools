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
    public class COLLADAMatrix : COLLADABaseType
    {
        protected override string TagName => "matrix";

        private float[] m_values;

        public float[] Values
        {
            get { return m_values; }
        }

        public string SubId;

        public override void LoadXml(XmlElement node)
        {
            SubId = node.GetAttribute("sid");

            var valIdx = 0;

            foreach (var val in node.InnerText.Split(' '))
                m_values[valIdx++] = float.Parse(val);
        }

        public override void SaveXml(XmlElement node)
        {
            node.SetAttribute("sid", SubId);
            node.InnerText = String.Join(" ", Values.ToArray());
        }

        public COLLADAMatrix()
        {
            m_values = new float[16];
        }
    }
}
