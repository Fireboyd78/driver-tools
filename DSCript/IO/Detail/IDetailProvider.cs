using System.IO;

namespace DSCript
{
    public interface IProvider
    {
        PlatformType Platform { get; }

        int Version { get; }

        int Flags { get; set; }
    }

    public interface IDetailProvider
    {
        PlatformType Platform { get; }

        int Version { get; }

        int Flags { get; set; }
    }
}
