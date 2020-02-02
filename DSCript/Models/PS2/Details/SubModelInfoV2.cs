using System.IO;

namespace DSCript.Models
{
    public struct SubModelInfoV2PS2 : IClassDetail<SubModelPS2>
    {
        public short TextureId;
        public short TextureSource;

        public int Unknown_04; // always zero?

        public short DataSizeDiv; // size / 10

        public byte Type;
        public byte Flags;

        public int DataOffset;

        public int Unknown_10; // always zero?

        public SubModelPS2 ToClass()
        {
            return new SubModelPS2() {
                HasBoundBox = false,

                TextureId = TextureId,
                TextureSource = TextureSource,

                Type = Type,
                Flags = Flags,

                Unknown1 = Unknown_04,
                Unknown2 = Unknown_10,
            };
        }

        public SubModelInfoV2PS2(Stream stream)
        {
            TextureId = stream.ReadInt16();
            TextureSource = stream.ReadInt16();

            Unknown_04 = stream.ReadInt32();

            DataSizeDiv = stream.ReadInt16();

            Type = (byte)stream.ReadByte();
            Flags = (byte)stream.ReadByte();

            DataOffset = stream.ReadInt32();

            Unknown_10 = stream.ReadInt32();
        }
    }
}
