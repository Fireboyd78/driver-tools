using System;
using System.IO;

namespace DSCript.Models
{
    public struct VehicleHierarchyInfo : IDetail
    {
        public int Flags;
        
        public short T1Count; // READ ORDER: 3
        public short T2Count; // READ ORDER: 1
        public short T3Count; // READ ORDER: 2
        public short T4Count; // READ ORDER: 4

        public int ExtraData;
        public int ExtraFlags;
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            Flags = stream.ReadInt32();

            T1Count = stream.ReadInt16();
            T2Count = stream.ReadInt16();
            T3Count = stream.ReadInt16();
            T4Count = stream.ReadInt16();
            
            if (provider.Version == 1)
            {
                ExtraFlags = stream.ReadInt32() & 0; // force to zero
                ExtraData = stream.ReadInt32();
            }
            else
            {
                // Driv3r format
                ExtraData = stream.ReadInt32();
                ExtraFlags = MagicNumber.FIREBIRD;

                // skip junk data
                stream.Position += 4;
            }
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(Flags);

            stream.Write(T1Count);
            stream.Write(T2Count);
            stream.Write(T3Count);
            stream.Write(T4Count);

            if (ExtraFlags != 0)
            {
                stream.Write(ExtraData);
                stream.Write(ExtraFlags);
            }
            else
            {
                stream.Write(0);
                stream.Write(ExtraData);
            }
        }

        public int T1Size
        {
            get { return (ExtraFlags != 0) ? 0x10 : 0x20; }
        }

        public int T2Size
        {
            get { return 0x20; }
        }

        public int T3Size
        {
            get { return 0x50; }
        }

        public int T4Size
        {
            get { return 0x40; }
        }
        
        public int T1Length
        {
            get
            {
                return (T1Count * T1Size);
            }
        }

        public int T2Length
        {
            get { return (T2Count * T2Size); }
        }

        public int T3Length
        {
            get { return (T3Count * T3Size); }
        }

        public int T4Length
        {
            get { return (T4Count * T4Size); }
        }

        public int BufferSize
        {
            get { return (T1Length + T2Length + T3Length + T4Length); }
        }
    }
}
