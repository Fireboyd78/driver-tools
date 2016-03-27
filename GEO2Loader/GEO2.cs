using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GEO2Loader
{
    public class GEO2Block
    {
        public struct Thing1
        {
            public float UPad1;
            public float UPad2;
            public float UPad3;
            public float UPad4;

            public uint T2Count;
            public uint T2Offset;
            public uint Unknown;

            public const uint Pad = 0x0;
        }

        public struct Thing2
        {
            public uint UnkGUID; // guid?
            public uint Unk2; // padding?
            public uint T3Offset;
        }

        public struct Thing3
        {
            public const uint Pad1 = 0x0;
            public const uint Pad2 = 0x0;

            public float UFloat1;
            public float UFloat2;
            public float UFloat3;

            public ushort TexID;
            public ushort TexSrc; // from vvs, car globals, etc

            public float UFloat4;
            public float UFloat5;
            public float UFloat6;

            public uint UnkPad;

            public ushort Unk1;

            public byte UnkFlag1;
            public byte UnkFlag2;

            public uint T4Offset;
        }

        public struct Thing4
        {
            // some sort of model stuff
        }

        public const BlockType Magic = BlockType.GEO2;

        public uint Offset { get; set; }

        public byte Thing1Count { get; set; }
        public byte Thing2Count { get; set; }
        public byte Thing3Count { get; set; }
        public byte UnkCount { get; set; }

        public ushort UnkShort1 { get; set; }
        public ushort UnkShort2 { get; set; }
        public ushort UnkShort3 { get; set; }
        public ushort UnkShort4 { get; set; }

        public List<Thing1> Thing1Entries { get; set; }
        public List<Thing2> Thing2Entries { get; set; }
        public List<Thing3> Thing3Entries { get; set; }

        public uint UnkOffset { get; set; }

        public GEO2Block(uint offset)
        {
            Offset = offset;
        }
    }
}
