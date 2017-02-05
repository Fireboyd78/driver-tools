using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GMC2Snooper
{
    public enum VifCommandType
    {
        Nop			= 0x00,	// No Operation
        StCycl		= 0x01,	// Sets CYCLE register
        Offset		= 0x02,	// Sets OFFSET register (VIF1)
        Base		= 0x03,	// Sets BASE register (VIF1)
        ITop		= 0x04,	// Sets ITOPS register
        StMod		= 0x05,	// Sets MODE register
        MskPath3	= 0x06,	// Mask GIF transfer (VIF1)
        Mark		= 0x07,	// Sets Mark register
        FlushE		= 0x10,	// Wait for end of microprogram
        Flush		= 0x11,	// Wait for end of microprogram & Path 1/2 GIF xfer (VIF1)
        FlushA		= 0x13,	// Wait for end of microprogram & all Path GIF xfer (VIF1)
        MsCal		= 0x14,	// Activate microprogram
        MsCnt		= 0x17,	// Execute microrprogram continuously
        MsCalf		= 0x15,	// Activate microprogram (VIF1)
        StMask		= 0x20,	// Sets MASK register
        StRow		= 0x30,	// Sets ROW register
        StCol		= 0x31,	// Sets COL register
        Mpg			= 0x4A,	// Load microprogram
        Direct		= 0x50,	// Transfer data to GIF (VIF1)
        DirectHL    = 0x51,	// Transfer data to GIF but stall for Path 3 IMAGE mode (VIF1)
    }

    public enum VifUnpackType : int
    {
        S_8,        // single-component, 8-bit
        S_16,       // single-component, 16-bit
        S_32,       // single-component, 32-bit
        
        V2_8,       // 2-components, 8-bit
        V2_16,      // 2-components, 16-bit
        V2_32,      // 2-components, 32-bit

        V3_8,       // 3-components, 8-bit
        V3_16,      // 3-components, 16-bit
        V3_32,      // 3-components, 32-bit

        V4_8,       // 4-components, 8-bit
        V4_16,      // 4-components, 16-bit
        V4_32,      // 4-components, 32-bit

        V4_5551,    // 4-components, 16-bit (RGB555A1)

        Invalid,
    }

    public struct VifCommand
    {
        private byte m_cmd;

        public static readonly int[] VL_VAL     = { 0, 0x0003 };
        public static readonly int[] VN_VAL     = { 2, 0x0003 };
        public static readonly int[] M_VAL      = { 4, 0x0001 };
        public static readonly int[] P_VAL      = { 5, 0x0003 }; // padding?? (couldn't find info on this)

        private static readonly string[] VN_LOOKUP       = { "S", "V2", "V3", "V4" };
        private static readonly int[] VL_LOOKUP          = { 32, 16, 8, 5 };

        private static readonly VifUnpackType[,] UNPACK_TYPE_LOOKUP = {
            { VifUnpackType.S_32,   VifUnpackType.S_16,     VifUnpackType.S_8,      VifUnpackType.Invalid },
            { VifUnpackType.V2_32,  VifUnpackType.V2_16,    VifUnpackType.V2_8,     VifUnpackType.Invalid },
            { VifUnpackType.V3_32,  VifUnpackType.V3_16,    VifUnpackType.V3_8,     VifUnpackType.Invalid },
            { VifUnpackType.V4_32,  VifUnpackType.V4_16,    VifUnpackType.V4_8,     VifUnpackType.V4_5551 },
        };

        private static readonly int[,] UNPACK_SIZE_LOOKUP = {
            // size of a _single_ component, in bytes
            // e.g. V3_8 would be 1, NOT 3!
            { 4,  2,  1, -1 },
            { 4,  2,  1, -1 },
            { 4,  2,  1, -1 },
            { 4,  2,  1,  2 },
        };

        private static readonly int[,] UNPACK_NUM_LOOKUP = {
            // how many actual components there are
            { 1,  1,  1, -1 },
            { 2,  2,  2, -1 },
            { 3,  3,  3, -1 },
            { 4,  4,  4,  1 },
        };

        private byte GetVal(int[] info)
        {
            return (byte)((m_cmd >> info[0]) & info[1]);
        }

        private void SetVal(int[] info, byte value)
        {
            m_cmd &= (byte)(~(info[1] << info[0]));
            m_cmd |= (byte)((value & info[1]) << info[0]);
        }

        public byte VL
        {
            get { return GetVal(VL_VAL); }
            set { SetVal(VL_VAL, value); }
        }

        public byte VN
        {
            get { return GetVal(VN_VAL); }
            set { SetVal(VN_VAL, value); }
        }

        public byte M
        {
            get { return GetVal(M_VAL); }
            set { SetVal(M_VAL, value); }
        }

        public byte P
        {
            get { return GetVal(P_VAL); }
            set { SetVal(P_VAL, value); }
        }

        public override string ToString()
        {
            // we're limited to 3-bits,
            // so errors aren't possible! :D
            var name = $"{VN_LOOKUP[VN]}_{VL_LOOKUP[VL]}";

            if (M == 1)
                name += "_MASKED";

            return name;
        }

        public VifUnpackType GetUnpackDataType()
        {
            return UNPACK_TYPE_LOOKUP[VN, VL];
        }

        public int GetUnpackDataCount()
        {
            return UNPACK_NUM_LOOKUP[VN, VL];
        }
        
        public int GetUnpackDataSize()
        {
            return UNPACK_SIZE_LOOKUP[VN, VL];
        }
        
        public VifCommand(byte data)
        {
            m_cmd = data;
        }
    }

    public struct VifImmediate
    {
        private ushort m_imdt;

        public static readonly int[] ADDR_VAL   = { 0, 0x03FF };
        public static readonly int[] USN_VAL    = { 14, 0x0001 };
        public static readonly int[] FLG_VAL    = { 15, 0x0001 };

        private int GetVal(int[] info)
        {
            return ((m_imdt >> info[0]) & info[1]);
        }

        private void SetVal(int[] info, int value)
        {
            m_imdt &= (ushort)(~(info[1] << info[0]));
            m_imdt |= (ushort)((value & info[1]) << info[0]);
        }

        private bool GetBoolVal(int[] info)
        {
            return (GetVal(info) == 1) ? true : false;
        }

        private void SetBoolVal(int[] info, bool value)
        {
            SetVal(info, (value) ? 1 : 0);
        }

        public ushort ADDR
        {
            get { return (ushort)GetVal(ADDR_VAL); }
            set { SetVal(ADDR_VAL, value); }
        }

        public bool USN
        {
            get { return GetBoolVal(USN_VAL); }
            set { SetBoolVal(USN_VAL, value); }
        }

        public bool FLG
        {
            get { return GetBoolVal(FLG_VAL); }
            set { SetBoolVal(FLG_VAL, value); }
        }

        public byte IMDT_STCYCL_CL      => (byte)(m_imdt & 0xFF);
        public byte IMDT_STCYCL_WL      => (byte)((m_imdt >> 8) & 0xFF);
        public ushort IMDT_OFFSET       => (ushort)(m_imdt & 0x3FF);
        public ushort IMDT_BASE         => (ushort)(m_imdt & 0x3FF);
        public ushort IMDT_ITOP         => (ushort)(m_imdt & 0x3FF);
        public byte IMDT_STMOD          => (byte)(m_imdt & 0x3);
        public bool IMDT_MSKPATH3       => (bool)(((m_imdt >> 15) & 0x1) == 1);
        public ushort IMDT_MARK         => (ushort)(m_imdt & 0xFFFF);
        public ushort IMDT_MSCAL        => (ushort)(m_imdt & 0xFFFF);
        public ushort IMDT_MSCALF       => (ushort)(m_imdt & 0xFFFF);
        public ushort IMDT_MPG          => (ushort)(m_imdt & 0xFFFF);
        public ushort IMDT_DIRECT       => (ushort)(m_imdt & 0xFFFF);
        public ushort IMDT_DIRECTHL     => (ushort)(m_imdt & 0xFFFF);

        public VifImmediate(ushort data)
        {
            m_imdt = data;
        }
    }
}
