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
            get
            {
                //var bin = (UID >> 24) & 0xFF;
                //var grp = (UID >> 16) & 0xFF;
                
                /*
                var u1 = (Handle >> 24) & 0xFF;
                var u2 = Handle & 0xFFFFFF;

                var id1 = ((UID >> 24) & 0xFF) | (u2 << 8);
                var id2 = (UID >> 16) & 0xFF;
                var id3 = (UID & 0x3FFF);
                */

                var id1 = (Handle >> 8) & 0xFFFFFF;
                var id2 = Handle & 0xFF;

                var id3 = (UID & 0xFFFF);
                var id4 = (UID >> 16) & 0xFFFF;


                return (!IsNull) ? $"{id1:X6}:{id2:X2}:{id3:X4}:{id4:X4}" : "<NULL>";
            }
        }

        public int UID { get; private set; }
        public int Handle { get; private set; }

        public bool IsNull
        {
            get
            {
                foreach (PartsGroup group in Parts)
                    foreach (var part in group.Parts)
                        if (part != null)
                            return false;

                return true;
            }
        }

        public ModelPackagePC ModelPackage { get; private set; }
        public List<PartsGroup> Parts { get; private set; }

        public ModelGroupListItem(ModelPackagePC modelPackage, PartsGroup partBasedOn)
        {
            ModelPackage = modelPackage;

            UID = partBasedOn.UID;
            Handle = partBasedOn.Handle;

            Parts = new List<PartsGroup>();

            int startIndex = ModelPackage.Parts.IndexOf(partBasedOn);

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
