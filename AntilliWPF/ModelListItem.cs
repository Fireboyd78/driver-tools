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
        public int Id { get; private set; }

        public DriverModelGroup Model { get; private set; }

        public override string ToString()
        {
            return String.Format("Model {0}", Id);
        }

        public ModelListItem(int id, DriverModelGroup model)
        {
            Id = id;
            Model = model;
        }
    }
}
