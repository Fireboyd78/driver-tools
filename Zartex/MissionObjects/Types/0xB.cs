using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0xB : IMissionObject
    {
        // number of floats in this unknown list
        private const int nFloats = 3;

        public int ID
        {
            get { return 0x3; }
        }

        public int Size
        {
            get
            {
                // identifier, floats, padding
                return (sizeof(uint) + sizeof(float) * nFloats);
            }
        }

        [TypeConverter(typeof(CollectionConverter))]
        public List<Double> Floats { get; set; }

        public BlockType_0xB(BinaryReader reader)
        {
            Floats = new List<Double>(nFloats);

            for (int i = 0; i < nFloats; i++)
            {
                Floats.Add(BitConverter.ToSingle(BitConverter.GetBytes(reader.ReadSingle()), 0));
            }
        }
    }
}
