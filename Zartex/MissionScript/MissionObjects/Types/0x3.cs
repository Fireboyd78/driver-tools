using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex
{
    public class MissionObject_3 : MissionObject
    {
        public override int Id
        {
            get { return 3; }
        }

        public override int Size
        {
            get { return (8 + (Floats.Count * 4)); }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats { get; set; }
    }
}
