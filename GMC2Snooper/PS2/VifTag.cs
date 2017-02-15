using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GMC2Snooper.PS2
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = sizeof(uint))]
    public struct VifTag
    {
        private uint m_tag;

        public static readonly int[] IMDT_VAL   = { 0, 0xFFFF };
        public static readonly int[] NUM_VAL    = { 16, 0x00FF };
        public static readonly int[] CMD_VAL    = { 24, 0x00FF };
        public static readonly int[] IRQ_VAL    = { 31, 0x0001 };

        private int GetVal(int[] info)
        {
            return ((int)(m_tag >> info[0]) & info[1]);
        }

        private void SetVal(int[] info, int value)
        {
            m_tag &= (~(uint)(info[1] << info[0]));
            m_tag |= ((uint)(value & info[1]) << info[0]);
        }

        private bool GetBoolVal(int[] info)
        {
            return (GetVal(info) == 1) ? true : false;
        }

        private void SetBoolVal(int[] info, bool value)
        {
            SetVal(info, (value) ? 1 : 0);
        }

        public ushort IMDT
        {
            get { return (ushort)GetVal(IMDT_VAL); }
            set { SetVal(IMDT_VAL, value); }
        }

        public byte NUM
        {
            get { return (byte)GetVal(NUM_VAL); }
            set { SetVal(NUM_VAL, value); }
        }

        public byte CMD
        {
            get { return (byte)GetVal(CMD_VAL); }
            set { SetVal(CMD_VAL, value); }
        }

        public bool IRQ
        {
            get { return GetBoolVal(IRQ_VAL); }
            set { SetBoolVal(IRQ_VAL, value); }
        }

        public uint ToBinary()
        {
            return m_tag;
        }
        
        public VifTag(uint tag)
        {
            m_tag = tag;
        }
    }
}
