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
    public class MissionObject_1 : MissionObject
    {
        public interface IFieldData
        {
            int Type { get; }
            
            int Reserved { get; set; }

            void Load(Stream stream);
            void Save(Stream stream);
        }
        
        public class FieldData
        {
            public int Offset { get; set; }

            public byte Type { get; set; }

            [TypeConverter(typeof(CollectionConverter))]
            public List<double> Floats { get; set; }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<FieldData> Fields { get; set; }

        [Browsable(false)]
        protected int FieldSize { get; set; }

        public override int Id
        {
            get { return 1; }
        }

        public override int Size
        {
            get
            {
                if (Fields == null)
                    throw new Exception("Cannot retrieve size from an uninitialized block.");

                return (32 + FieldSize);
            }
        }
        
        public short Reserved { get; set; }
        public short Unknown { get; set; }

        public Vector3 Position { get; set; }

        public int VehicleID { get; set; }
    }
}
