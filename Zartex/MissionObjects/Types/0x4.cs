using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0x4 : ContainerBlock
    {
        public override int Id
        {
            get { return 0x4; }
        }

        public BlockType_0x4(BinaryReader reader) : base(reader) { }
    }
}
