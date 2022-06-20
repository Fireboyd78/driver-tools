using System.IO;

namespace DSCript.Models
{
    public struct LodInfo : IDetail
    {
        public int InstancesOffset;
        public int InstancesCount;

        // vertex + triangle count?
        public int Reserved;

        public int Flags;
        public int Mask;

        public int ExtraData;
        
        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(InstancesOffset);

            if (provider.Version == 6)
                stream.Write(0);

            stream.Write(InstancesCount);

            if (InstancesCount > 0)
                stream.Write((int)MagicNumber.FIREBIRD); // ;)
            else
                stream.Write(0);
            
            stream.Write(Flags);

            stream.Write(Mask);
            stream.Write(ExtraData);

            if (provider.Version == 6)
                stream.Write(0);
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            // initialize with offset
            InstancesOffset = stream.ReadInt32();

            if (provider.Version == 6)
                stream.Position += 4;

            InstancesCount = stream.ReadInt32();
            
            Reserved = stream.ReadInt32();
            
            Flags = stream.ReadInt32();
            Mask = stream.ReadInt32();

            ExtraData = stream.ReadInt32();
            
            if (provider.Version == 6)
                stream.Position += 4;
        }
    }
}
