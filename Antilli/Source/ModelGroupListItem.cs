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
    public class ModelGroupListItem
    {
        public string Text
        {
            get { return (!IsNull) ? UID.ToString() : "<NULL>"; }
        }

        public UID UID { get; private set; }
        
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

        public ModelPackage ModelPackage { get; private set; }
        public List<Model> Models { get; private set; }

        public ModelGroupListItem(ModelPackage modelPackage, Model modelBasis)
        {
            ModelPackage = modelPackage;

            UID = modelBasis.UID;
            Models = new List<Model>();

            int startIndex = ModelPackage.Models.IndexOf(modelBasis);

            for (int p = startIndex; p < ModelPackage.Models.Count; p++)
            {
                Model part = ModelPackage.Models[p];

                if (part.UID != UID)
                    continue;

                do
                    Models.Add(part);
                while (++p < ModelPackage.Models.Count && (part = ModelPackage.Models[p]).UID == UID);

                break;
            }
        }
    }
}
