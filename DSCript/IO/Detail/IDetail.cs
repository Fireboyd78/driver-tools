using System.IO;
using System.Xml.Linq;

namespace DSCript
{
    public interface IDetail
    {
        void Serialize(Stream stream, IDetailProvider provider);
        void Deserialize(Stream stream, IDetailProvider provider);
    }

    public interface IXmlDetail
    {
        void Serialize(XElement node, IDetailProvider provider);
        void Deserialize(XElement node, IDetailProvider provider);
    }

    public interface IDetail<TProvider>
        where TProvider : IProvider
    {
        void Serialize(Stream stream, TProvider provider);
        void Deserialize(Stream stream, TProvider provider);
    }
}
