using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GEO2Loader
{
    public class GMC2Model
    {
        public const BlockType Magic = BlockType.MPAK;

        public ModelType Type { get; set; }

        public const uint Pad1 = 0x0;
        public const uint Pad2 = 0x0;
        public const uint Pad3 = 0x0;

        public uint nGeometry { get; set; }

        public const uint Pad4 = 0x0;

        public List<GEO2Block> Geometry { get; set; }
        public TSC2Block TSC2 = new TSC2Block();
    }
}
