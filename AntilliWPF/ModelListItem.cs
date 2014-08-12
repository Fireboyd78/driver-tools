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
        protected List<ModelListItem> Parent { get; set; }
        
        public ModelVisual3DGroup Model { get; protected set; }

        public int Index
        {
            get { return Parent.IndexOf(this); }
        }

        public string Name
        {
            get
            {
                if (!String.IsNullOrEmpty(Model.Name))
                    return Model.Name;
                else
                    return String.Format("Model {0}", Index + 1);
            }
        }

        public ModelListItem(List<ModelListItem> parent, ModelVisual3DGroup model)
        {
            Parent = parent;
            Model = model;
        }
    }

    public class ModelVisual3DGroupListItem : ModelListItem
    {
        public List<ModelVisual3D> Models
        {
            get { return (Model != null) ? Model.Children : null; }
        }

        public ModelVisual3DGroupListItem(List<ModelListItem> parent, ModelVisual3DGroup model) : base(parent, model) { }
    }
}
