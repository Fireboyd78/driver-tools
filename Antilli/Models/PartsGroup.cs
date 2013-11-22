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

using Antilli.Models;

namespace Antilli.Models
{
    public class PartsGroup
    {
        public class Entry
        {
            public int ID { get; set; }

            public uint Unknown { get; set; }
            public uint Reserved { get; set; }

            public PartsGroup Parent { get; set; }
            public MeshGroup Group { get; set; }

            public Entry(int id)
            {
                ID = id;
            }
        }

        uint _uid;

        public uint UID
        {
            get { return _uid; }
            set
            {
                _uid = (value != 0) ? value : (uint)new Random((int)DateTime.Now.ToBinary()).Next(0, 100000);
            }
        }

        public uint Handle { get; set; }

        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }

        public List<Entry> Parts { get; set; }

        public PartsGroup()
        {
            Parts = new List<Entry>(7);
        }
    }
}
