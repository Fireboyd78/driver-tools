using System.IO;

namespace DSCript.Models
{
    public struct PhysicsInfo : IDetail
    {
        public static readonly string Magic = "PDL001.002.003a";

        public int T1Count;
        public int T2Count;

        public int T1Offset;
        public int T2Offset;
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            T1Count = stream.ReadInt32();
            T2Count = stream.ReadInt32();

            T1Offset = stream.ReadInt32();
            T2Offset = stream.ReadInt32();

            var check = stream.ReadString(16);

            if (check != Magic)
                throw new InvalidDataException("Invalid PDL magic!");
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(T1Count);
            stream.Write(T2Count);

            stream.Write(T1Offset);
            stream.Write(T2Offset);

            stream.Write(Magic + "\0");
        }
    }
}
