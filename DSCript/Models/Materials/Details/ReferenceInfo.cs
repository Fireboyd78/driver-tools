using System.Collections.Generic;
using System.IO;

namespace DSCript.Models
{
    public struct ReferenceInfo<T> : IDetail, IComparer<ReferenceInfo<T>>
        where T : class
    {
        public int Offset;
        
        public T Reference;
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            Offset = stream.ReadInt32();

            if (provider.Version == 6)
                stream.Position += 4;
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(Offset);

            if (provider.Version == 6)
                stream.Write(0);
        }

        int IComparer<ReferenceInfo<T>>.Compare(ReferenceInfo<T> x, ReferenceInfo<T> y)
        {
            return x.Offset.CompareTo(y.Offset);
        }

        public ReferenceInfo(T value)
        {
            Offset = -1;
            Reference = value;
        }

        public ReferenceInfo(T value, int offset)
        {
            Offset = offset;
            Reference = value;
        }
    }
}
