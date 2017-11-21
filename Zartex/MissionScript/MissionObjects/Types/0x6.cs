using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

using Zartex.Converters;

namespace Zartex
{
    public class MissionObject_6 : MissionObject
    {
        public override int Id
        {
            get { return 6; }
        }

        public override int Size
        {
            get { return 0x1C; }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats { get; set; }

        public double UnkFloat { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public int UnkID { get; set; }
    }
}
