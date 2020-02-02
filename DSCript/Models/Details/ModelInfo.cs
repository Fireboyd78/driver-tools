using System.IO;

namespace DSCript.Models
{
    public struct ModelInfo : IDetail
    {
        public UID UID;

        public Vector4 Scale;

        // INCOMING TRANSMISSION...
        // RE: OPERATION S.T.E.R.N....
        // ...
        // YOUR ASSISTANCE HAS BEEN NOTED...
        // ...
        // <END OF TRANSMISSION>...
        public short BufferIndex;
        public short BufferType;

        public int Flags;

        // reserved space for effect index
        // sadly can't be used to force a specific effect cause game overwrites it :(
        public int Reserved;
        
        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(UID);
            stream.Write(Scale);

            stream.Write(BufferIndex);
            stream.Write(BufferType);

            stream.Write(Flags);
            stream.Write(0);

            if (provider.Version == 6)
                stream.Write(0);
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            UID = stream.Read<UID>();
            Scale = stream.Read<Vector4>();

            BufferIndex = stream.ReadInt16();
            BufferType = stream.ReadInt16();

            Flags = stream.ReadInt32();
            Reserved = stream.ReadInt32();

            if (provider.Version == 6)
                stream.Position += 4;
        }
    }
}
