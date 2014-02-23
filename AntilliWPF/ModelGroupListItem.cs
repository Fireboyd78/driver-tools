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

        public uint UID { get; private set; }

        public bool IsNull
        {
            get
            {
                foreach (PartsGroup part in Parts)
                    if (part.Parts[0].Group != null)
                        return false;

                return true;
            }
        }

        public List<PartsGroup> Parts { get; private set; }

        public IModelFile ModelFile { get; private set; }
        public ModelPackage ModelPackage { get; private set; }

        public Model3DGroup Models { get; private set; }

        public ModelGroupListItem(IModelFile modelFile, ModelPackage modelPackage, PartsGroup partBasedOn)
        {
            ModelFile = modelFile;
            ModelPackage = modelPackage;

            UID = partBasedOn.UID;

            Parts = new List<PartsGroup>();

            int startIndex = ModelPackage.Parts.FindIndex((p) => p == partBasedOn);

            for (int p = startIndex; p < ModelPackage.Parts.Count; p++)
            {
                PartsGroup part = ModelPackage.Parts[p];

                if (part.UID != UID)
                    continue;

                do
                    Parts.Add(part);
                while (++p < ModelPackage.Parts.Count && (part = ModelPackage.Parts[p]).UID == UID);

                break;
            }
        }
    }
}
