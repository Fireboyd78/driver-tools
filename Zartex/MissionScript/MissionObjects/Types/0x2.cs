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
    public class MissionObject_2 : MissionObject
    {
        private short _type = 0;

        public override int Id
        {
            get { return 2; }
        }

        public override int Size
        {
            get
            {
                if (_type == 0) throw new Exception("Cannot retrieve size of uninitalized block");

                return (_type != 0x14) ? 0x38 : 0x30;
            }
        }

        public short Reserved { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public int Flags1 { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats1 { get; set; }

        [TypeConverter(typeof(CollectionConverter))]
        public List<double> Floats2 { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public int GUID { get; set; }

        [TypeConverter(typeof(HexStringConverter))]
        public int Flags2 { get; set; }
    }
}
