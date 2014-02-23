using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using System.Windows.Media.Media3D;

namespace Antilli
{
    public class ModelListItem
    {
        public int Index
        {
            get { return Parent.IndexOf(this); }
        }

        public IList<ModelListItem> Parent { get; private set; }

        public string Name
        {
            get { return (Model.Name != "") ? Model.Name : String.Format("Model {0}", Index); ; }
        }

        public ModelVisual3DGroup Model { get; private set; }

        public ModelListItem(IList<ModelListItem> parent, ModelVisual3DGroup model)
        {
            Parent = parent;
            Model = model;
        }
    }
}
