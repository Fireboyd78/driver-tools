namespace DSC.IO.Types
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using DSC.Base;
    using DSC.IO;

    using FreeImageAPI;

    public sealed class PCMPData
    {
        public class DDSInfo
        {
            public byte Unk1 { get; set; }
            public byte Unk2 { get; set; }
            public byte Unk3 { get; set; }
            public byte Unk4 { get; set; }

            public uint CRC32 { get; set; }
            public uint Offset { get; set; }
            public uint Size { get; set; }
            public uint Type { get; set; }

            public ushort Width { get; set; }
            public ushort Height { get; set; }

            public uint Unk5 { get; set; }
            public uint Unk6 { get; set; }

            public string Filename { get; set; }

            public Bitmap File { get; set; }
        }

        public class SubMaterial
        {
            public uint Offset { get; set; }
            public uint DDSInfoOffset { get; set; }
            public uint t4Offset { get; set; }
            public uint t4Count { get; set; }

            public uint Unk1 { get; set; }
            public uint Unk2 { get; set; }
            public uint Unk3 { get; set; }
            public uint Unk4 { get; set; }

            public uint Unk5 { get; set; }
            public uint Unk6 { get; set; }

            public List<PCMPData.DDSInfo> Textures = new List<PCMPData.DDSInfo> { };
        }

        public class Material
        {
            public uint Unk1 { get; set; }
            public uint Unk2 { get; set; }
            public uint Unk3 { get; set; }
            public uint Unk4 { get; set; }

            public uint t2Count { get; set; }
            public uint t2Offset { get; set; }
            public uint t2SubMatOffset { get; set; }

            public List<PCMPData.SubMaterial> SubMaterial = new List<PCMPData.SubMaterial> { };
        }

        public const uint Magic = 0x504D4350; // 'PCMP'
        public const uint Version = 0x3;

        public uint tGroupCount { get; set; }
        public uint tGroupOffset { get; set; }

        public uint t2Count { get; set; }
        public uint t2Offset { get; set; }

        public uint tSubMatCount { get; set; }
        public uint tSubMatOffset { get; set; }

        public uint t4Count { get; set; }
        public uint t4Offset { get; set; }

        public uint DDSInfoCount { get; set; }
        public uint DDSInfoOffset { get; set; }
        public uint DDSOffset { get; set; }

        public uint Size { get; set; }
        public uint Offset { get; set; }

        public List<PCMPData.Material> Materials = new List<PCMPData.Material> { };
    }

}