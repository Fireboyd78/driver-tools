using System.IO;

namespace DSCript.Models
{
    public struct LodInstanceInfoPS2
    {
        public int RotationOffset;
        public int TranslationOffset;

        public int SubModelOffset;

        public LodInstanceInfoPS2(Stream stream)
        {
            RotationOffset = stream.ReadInt32();
            TranslationOffset = stream.ReadInt32();
            SubModelOffset = stream.ReadInt32();
        }
    }
}
