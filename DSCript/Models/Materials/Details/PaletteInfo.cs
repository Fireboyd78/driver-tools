using System.IO;

namespace DSCript.Models
{
    public struct PaletteInfo : IDetail
    {
        public int Index;
        public int Count;

        public int DataOffset;
        public int DataSize => (Count * 4);

        private static readonly int[] s_PaletteSizes = { 256, 128, 64, 32 };

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            var resource = stream.Read<D3DResource>();
            var type = DDSUtils.GetResourceType(ref resource, provider.Platform);

            if (type != D3DResourceType.Palette)
                throw new InvalidDataException($"Expected a palette resource but got a {type.ToString()} instead!");

            var size = ((resource.Common >> 30) & 0x3);

            Index = ((resource.Common >> 28) & 0x3);
            Count = s_PaletteSizes[size];

            DataOffset = resource.Data;
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            var resource = default(D3DResource);
            var common = 0x30001;

            var size = -1;

            for (int i = 0; i < 4; i++)
            {
                var paletteSize = s_PaletteSizes[i];

                if (Count == paletteSize)
                {
                    size = i;
                    break;
                }
            }

            if (size == -1)
                throw new InvalidDataException($"Invalid palette count '{Count}'!");

            common |= ((size & 0x3) << 30);
            common |= ((Index & 0x3) << 28);

            resource.Common = common;
            resource.Data = DataOffset;

            stream.Write(resource);
        }
    }
}