using System.IO;

namespace DSCript.Models
{
    public struct LodInstanceInfo : IDetail
    {
        public struct ExtraInfo : IDetail
        {
            public int Reserved;
            public short Handle;
            
            void IDetail.Serialize(Stream stream, IDetailProvider provider)
            {
                stream.Write(Reserved);
                stream.Write(Handle);

                if (provider.Version == 6)
                    stream.Write((short)MagicNumber.FB); // ;)
            }

            void IDetail.Deserialize(Stream stream, IDetailProvider provider)
            {
                Reserved = stream.ReadInt32();
                Handle = stream.ReadInt16();

                if (provider.Version == 6)
                    stream.Position += 2;
            }
        }

        public int SubModelsOffset;
        
        public Matrix44 Transform;

        public short SubModelsCount;
        public short UseTransform;
        
        public ExtraInfo Info;
        
        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(SubModelsOffset);

            if (provider.Version == 6)
                stream.Write(0);

            stream.Write(Transform);

            stream.Write(SubModelsCount);
            stream.Write(UseTransform);

            if (provider.Version == 6)
                stream.Write((int)MagicNumber.FIREBIRD); // ;)

            provider.Serialize(stream, ref Info);
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            // initialize with offset
            SubModelsOffset = stream.ReadInt32();

            if (provider.Version == 6)
                stream.Position += 4;

            Transform = stream.Read<Matrix44>();

            SubModelsCount = stream.ReadInt16();
            UseTransform = stream.ReadInt16();
            
            if (provider.Version == 6)
                stream.Position += 4;

            Info = provider.Deserialize<ExtraInfo>(stream);
        }
    }
}
