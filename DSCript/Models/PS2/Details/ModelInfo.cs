using System;
using System.IO;

namespace DSCript.Models
{
    public struct ModelInfoPS2
    {
        public static readonly int Magic = 0x324F4547; // 'GEO2'

        public byte LodCount;
        public byte LodInstanceCount;
        public byte SubModelCount;
        public byte Type;

        public int Handle;
        public int UID;

        public Vector3 BoxOffset;

        public int Unknown_1C; // always zero?

        public Vector3 BoxScale;

        public int Unknown_2C;
        public int Unknown_30; // always zero?
        public int Unknown_34; // non-zero in .DAM files (offset?)

        // zero-padding?
        public int Unknown_38;
        public int Unknown_3C;
        
        public ModelInfoPS2(Stream stream)
        {
            var magic = stream.ReadInt32();

            if (magic != Magic)
                throw new InvalidOperationException($"Invalid GEO2 data!");

            LodCount = (byte)stream.ReadByte();
            LodInstanceCount = (byte)stream.ReadByte();
            SubModelCount = (byte)stream.ReadByte();
            Type = (byte)stream.ReadByte();

            Handle = stream.ReadInt32();
            UID = stream.ReadInt32();

            BoxOffset = stream.Read<Vector3>();

            Unknown_1C = stream.ReadInt32();

            BoxScale = stream.Read<Vector3>();

            Unknown_2C = stream.ReadInt32();
            Unknown_30 = stream.ReadInt32();
            Unknown_34 = stream.ReadInt32();

            // might just be alignment padding (0x38 > 0x40)
            Unknown_38 = stream.ReadInt32();
            Unknown_3C = stream.ReadInt32();
        }
    }
}
