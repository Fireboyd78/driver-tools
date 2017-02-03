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

    public struct VifCommand
    {
        private byte m_cmd;

        public static readonly int[] VL_VAL     = { 0, 0x0003 };
        public static readonly int[] VN_VAL     = { 2, 0x0003 };
        public static readonly int[] M_VAL      = { 4, 0x0001 };
        public static readonly int[] P_VAL      = { 5, 0x0003 }; // padding?? (couldn't find info on this)

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

        public VifImmediate(ushort data)
        {
            m_imdt = data;
        }
    }
}
