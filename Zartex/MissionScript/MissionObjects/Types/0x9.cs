using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex
{
    public class MissionObject_9 : MissionObject
    {
        public override int Id
        {
            get { return 9; }
        }

        public override int Size
        {
            get { return 0x24; }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats1 { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats2 { get; set; }
    }
}
