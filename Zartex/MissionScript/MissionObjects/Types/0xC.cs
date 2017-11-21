using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex
{
    public class MissionObject_12 : MissionObject
    {
        public override int Id
        {
            get { return 12; }
        }

        public override int Size
        {
            get { return 0x1C; }
        }

        public double VarFloat { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats { get; set; }
    }
}
