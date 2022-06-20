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
#if OLD_VISUALS
    using ModelVisual3DType = ModelVisual3DGroup;
#else
    using ModelVisual3DType = ModelVisual3DGroup;
#endif
    public class ModelListItem
    {
        protected List<ModelListItem> Parent { get; set; }
        
        public ModelVisual3DType Model { get; protected set; }

        public int Index
        {
            get { return Parent.IndexOf(this); }
        }

        public string Name
        {
            get
            {
#if OLD_VISUALS
                if (!String.IsNullOrEmpty(Model.Name))
                    return Model.Name;
                else
#endif
                    return String.Format("Model {0}", Index + 1);
            }
        }

        public ModelListItem(List<ModelListItem> parent, ModelVisual3DType model)
        {
            Parent = parent;
            Model = model;
        }
    }

    public class ModelVisual3DGroupListItem : ModelListItem
    {
#if OLD_VISUALS
        public List<ModelVisual3D> Models
#else
        public List<SubModelVisual3D> Models
#endif
        {
            get { return (Model != null) ? Model.Children : null; }
        }

        public ModelVisual3DGroupListItem(List<ModelListItem> parent, ModelVisual3DType model) : base(parent, model) { }
    }
}
