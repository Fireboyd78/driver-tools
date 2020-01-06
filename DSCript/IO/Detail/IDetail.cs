using System.IO;

namespace DSCript
{
    public interface IDetail
    {
        void Serialize(Stream stream, IDetailProvider provider);
        void Deserialize(Stream stream, IDetailProvider provider);
    }
}
