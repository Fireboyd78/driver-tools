using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex
{
    public class MissionObject_11 : MissionObject
    {
        public override int Id
        {
            get { return 11; }
        }

        public override int Size
        {
            get { return (4 + (Floats.Count * 4)); }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats { get; set; }
    }
}
