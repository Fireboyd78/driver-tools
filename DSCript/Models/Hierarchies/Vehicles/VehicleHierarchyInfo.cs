using System;
using System.IO;

namespace DSCript.Models
{
    public struct VehicleHierarchyInfo : IDetail
    {
        public int Flags;
        
        public short MarkerPointsCount;
        public short MovingPartsCount;
        public short DamagingPartsCount;
        public short InstancePartsCount;

        public int ExtraData;
        public int ExtraFlags;
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            Flags = stream.ReadInt32();

            MarkerPointsCount = stream.ReadInt16();
            MovingPartsCount = stream.ReadInt16();
            DamagingPartsCount = stream.ReadInt16();
            InstancePartsCount = stream.ReadInt16();
            
            // truthfully all junk, but figured I'd keep it anyways
            if (provider.Version == 1)
            {
                ExtraFlags = stream.ReadInt32() & 0; // force to zero
                ExtraData = stream.ReadInt32();
            }
            else
            {
                ExtraData = stream.ReadInt32();
                ExtraFlags = MagicNumber.FIREBIRD;

                // skip junk data
                stream.Position += 4;
            }
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(Flags);

            stream.Write(MarkerPointsCount);
            stream.Write(MovingPartsCount);
            stream.Write(DamagingPartsCount);
            stream.Write(InstancePartsCount);

            if (provider.Version == 1)
            {
                stream.Write(0);
                stream.Write(ExtraData);
            }
            else
            {
                stream.Write(ExtraData);
                stream.Write(0);
            }
        }

        public int MarkerPointSize
        {
            get { return (ExtraFlags != 0) ? 0x10 : 0x20; }
        }

        public int MovingPartSize
        {
            get { return 0x20; }
        }

        public int DamagingPartSize
        {
            get { return 0x50; }
        }

        public int InstancePartSize
        {
            get { return 0x40; }
        }
        
        public int MarkerPointsLength
        {
            get
            {
                return (MarkerPointsCount * MarkerPointSize);
            }
        }

        public int MovingPartsLength
        {
            get { return (MovingPartsCount * MovingPartSize); }
        }

        public int DamagingPartsLength
        {
            get { return (DamagingPartsCount * DamagingPartSize); }
        }

        public int InstancePartsLength
        {
            get { return (InstancePartsCount * InstancePartSize); }
        }

        public int BufferSize
        {
            get { return (MarkerPointsLength + MovingPartsLength + DamagingPartsLength + InstancePartsLength); }
        }
    }
}
