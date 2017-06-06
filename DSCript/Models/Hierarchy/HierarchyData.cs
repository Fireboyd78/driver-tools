using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using System.Runtime.InteropServices;

using DSCript.Spooling;

// uninitialized and unused struct member warnings
#pragma warning disable 169, 649
namespace DSCript.Models
{
    public enum HierarchyDataType : int
    {
        Prop    = 1,
        Vehicle = 6,
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct PropHierarchyInfoData
    {
        // some kind of transformation data?
        public float V1;
        public float V2;

        public int Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VehicleHierarchyInfoData
    {
        public int Flags;
        
        public short T1Count;
        public short T2Count;
        public short T3Count;
        public short T4Count;
        
        public int T1Length
        {
            get { return (T1Count * 0x10); }
        }

        public int T2Length
        {
            get { return (T2Count * 0x20); }
        }

        public int T3Length
        {
            get { return (T3Count * 0x50); }
        }

        public int T4Length
        {
            get { return (T4Count * 0x40); }
        }

        public int BufferSize
        {
            get { return (T1Length + T2Length + T3Length + T4Length); }
        }

        /*
            Offsets are relative to the 'HierarchyDataHeader.ExtraPartsDataOffset' value
        */

        public int T1Offset
        {
            get { return T3Offset + T3Length; }
        }

        public int T2Offset
        {
            get { return 0; }
        }

        public int T3Offset
        {
            get { return T2Offset + T2Length; }
        }

        public int T4Offset
        {
            get { return T1Offset + T1Length; }
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct HierarchyDataHeader
    {
        private int m_type;     // kind of data present (prop=1, vehicle=6, may be other unused ones)

        public int PartsCount;  // number of parts in hierarchy
        public int UID;         // unique identifier for the hierarchy
        public int PDLSize;     // used as offset past PDL for stuff like bullet hole data

        [StructLayout(LayoutKind.Explicit, Size = 0xC)]
        private struct HierarchyInfoData
        {
            [FieldOffset(0)]
            public VehicleHierarchyInfoData VehicleInfo;

            [FieldOffset(0)]
            public PropHierarchyInfoData PropInfo;
        } HierarchyInfoData m_hierInfo;
        
        public PropHierarchyInfoData PropHierarchyInfo
        {
            get { return m_hierInfo.PropInfo; }
            set { m_hierInfo.PropInfo = value; }
        }

        public VehicleHierarchyInfoData VehicleHierarchyInfo
        {
            get { return m_hierInfo.VehicleInfo; }
            set { m_hierInfo.VehicleInfo = value; }
        }

        public HierarchyDataType HierarchyType
        {
            get { return (HierarchyDataType)m_type; }
            set { m_type = (int)value; }
        }

        // size of header including header info
        // NOTE: this includes the unused stuff at the beginning!
        public static int HeaderSize
        {
            get { return 0x28; }
        }

        public int PartsOffset
        {
            get
            {
                switch (m_type)
                {
                case 1: return (HeaderSize + 0x18);
                case 6: return (HeaderSize + 0x8);
                }
                return -1;
            }
        }
        
        public int PartsLength
        {
            get
            {
                switch (m_type)
                {
                case 1: return (PartsCount * 0x50);
                case 6: return (PartsCount * 0x20);
                }
                return -1;
            }
        }
        
        public int PDLOffset
        {
            get
            {
                var pdlOffset = (PartsOffset + PartsLength);

                if (m_type == 6)
                    pdlOffset += VehicleHierarchyInfo.BufferSize;

                return pdlOffset;
            }
        }

        public int ExtraPartsDataOffset
        {
            get
            {
                if (m_type == 6)
                    return (PartsOffset + PartsLength);

                // no extra data present
                return -1;
            }
        }
    }
}
#pragma warning restore 169, 649
