using System.IO;

namespace DSCript.Models
{
    public struct SubModelInfoPS2 : IClassDetail<SubModelPS2>
    {
        public Vector3 BoxOffset;

        public short TextureId;
        public short TextureSource;

        public Vector3 BoxScale;

        public int Unknown_1C; // always zero?

        public short DataSizeDiv; // size / 10

        public byte Type; // same as GEO2.Type?
        public byte Flags;

        public int DataOffset;

        public int Unknown_28; // always zero?
        public int Unknown_2C; // always zero?

        public SubModelPS2 ToClass()
        {
            return new SubModelPS2() {
                HasBoundBox = true,
            
                BoxOffset = BoxOffset,
                BoxScale = BoxScale,
            
                TextureId = TextureId,
                TextureSource = TextureSource,
            
                Type = Type,
                Flags = Flags,
            
                Unknown1 = Unknown_1C,
                Unknown2 = Unknown_28,
            };
        }

        public SubModelInfoPS2(Stream stream)
        {
            BoxOffset = stream.Read<Vector3>();

            TextureId = stream.ReadInt16();
            TextureSource = stream.ReadInt16();

            BoxScale = stream.Read<Vector3>();

            Unknown_1C = stream.ReadInt32();

            DataSizeDiv = stream.ReadInt16();

            Type = (byte)stream.ReadByte();
            Flags = (byte)stream.ReadByte();

            DataOffset = stream.ReadInt32();

            Unknown_28 = stream.ReadInt32();
            Unknown_2C = stream.ReadInt32();
        }
    }
}
