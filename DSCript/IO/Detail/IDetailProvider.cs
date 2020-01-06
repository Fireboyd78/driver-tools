using System.IO;

namespace DSCript
{
    public interface IDetailProvider
    {
        PlatformType Platform { get; }

        int Version { get; }

        int Flags { get; set; }

        TDetail Deserialize<TDetail>(Stream stream) where TDetail : IDetail, new();
        void Serialize<TDetail>(Stream stream, ref TDetail detail) where TDetail : IDetail;
    }
}
