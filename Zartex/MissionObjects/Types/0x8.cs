using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace Zartex.MissionObjects
{
    public class BlockType_0x8 : MissionObject
    {
        public override int Id
        {
            get { return 0x8; }
        }

        public override int Size
        {
            get
            {
                if (Flags.Length == 0)
                    throw new Exception("Cannot return size of uninitialized block");
                
                return 0x1C;
            }
        }

        public int[] Flags;

        public BlockType_0x8(BinaryReader reader)
        {
            Offset = (int)reader.GetPosition();

            Flags = new int[6];

            for (int i = 0; i < Flags.Length; i++)
                Flags[i] = reader.ReadInt32();
        }
    }
}
