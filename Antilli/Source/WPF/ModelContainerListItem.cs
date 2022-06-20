using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;

using HelixToolkit.Wpf;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    public class ModelContainerListItem
    {
        public ModelPackage ModelPackage { get; private set; }
        public List<Model> Models { get; private set; }

        public UID UID { get; }
        
        public string Tag { get; }

        public bool Flagged { get; }
        
        public bool IsNull
        {
            get
            {
                foreach (Model model in Models)
                {
                    foreach (var lod in model.Lods)
                    {
                        if (lod != null)
                            return false;
                    }
                }

                return true;
            }
        }

        public string Name
        {
            get
            {
                if (IsNull)
                    return "";

                return UID.ToString();
            }
        }

        public string Text
        {
            get
            {
                var name = Name;

                if (String.IsNullOrEmpty(name))
                    name = "<NULL>";

                if (!String.IsNullOrEmpty(Tag))
                    name += $"[{Tag}]";

                if (Flagged)
                    name += " *";

                return name;
            }
        }
        
        public ModelContainerListItem(ModelPackage modelPackage, Model modelBasis)
        {
            ModelPackage = modelPackage;

            UID = modelBasis.UID;
            Models = new List<Model>();

            int startIndex = ModelPackage.Models.IndexOf(modelBasis);
            
            for (int p = startIndex; p < ModelPackage.Models.Count; p++)
            {
                var model = ModelPackage.Models[p];

                // merge common models together
                if (model.UID != UID)
                    break;

#if DEBUG
                if (String.IsNullOrEmpty(Tag))
                    Tag = $"{model.VertexType}:{model.Flags}";

                var flagged = true;

                switch (model.VertexType)
                {
                case 0:
                    flagged = (model.Flags != 0);
                    break;
                case 1:
                case 2:
                case 5:
                case 6:
                case 7:
                    flagged = (model.Flags <= 0) || (model.Flags > 2);
                    break;

                // hmm
                case 3:
                case 4:
                case 8:
                    flagged = true;
                    break;
                }

                if (flagged)
                {
                    DSC.Log($"**** Model {model.UID}[{p - startIndex}]: type {model.VertexType}, flags {model.Flags}");
                    Flagged = true;
                }
#endif
                Models.Add(model);
            }
        }
    }
}
