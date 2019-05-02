using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using System.Runtime.InteropServices;

using DSCript.Spooling;

namespace DSCript.Models
{
    public enum HierarchyDataType : int
    {
        Prop    = 1,
        Vehicle = 6,
    }
    
    public struct PropHierarchyInfo
    {
        // related to mass?
        public float V1;
        public float V2;

        public int Reserved;
    }
    
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
    
    public struct HierarchyInfo
    {
        public int Type;

        public int Count;   // number of parts in hierarchy
        public int UID;     // unique identifier for the hierarchy

        public int PDLSize; // used as offset past PDL for stuff like bullet hole data
    }

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
