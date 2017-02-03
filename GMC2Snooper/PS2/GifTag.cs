using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GMC2Snooper.PS2
{
    [StructLayout(LayoutKind.Sequential, Size = sizeof(ulong))]
    public struct GifTag
    {
        private ulong m_tag;

        // bit position and mask for each value
        public static readonly int[] NLOOP_VAL  = { 0,  0x7FFF };
        public static readonly int[] EOP_VAL    = { 15, 0x0001 };
        public static readonly int[] PRE_VAL    = { 46, 0x0001 };
        public static readonly int[] PRIM_VAL   = { 47, 0x07FF };
        public static readonly int[] FLG_VAL    = { 58, 0x0003 };
        public static readonly int[] NREG_VAL   = { 60, 0x000F };
        
        private int GetVal(int[] info)
        {
            return ((int)(m_tag >> info[0]) & info[1]);
        }

        private void SetVal(int[] info, int value)
        {
            // preserve all other data and replace the old value
            m_tag &= ~((ulong)(info[1] << info[0]));
            m_tag |= ((ulong)(value & info[1]) << info[0]);
        }

        private bool GetBoolVal(int[] info)
        {
            return (GetVal(info) == 1) ? true : false;
        }

        private void SetBoolVal(int[] info, bool value)
        {
            SetVal(info, (value) ? 1 : 0);
        }

        /// <summary>
        /// Holds the data size of the primitive.
        /// </summary>
        public short NLoop
        {
            get { return (short)GetVal(NLOOP_VAL); }
            set { SetVal(NLOOP_VAL, value); }
        }

        /// <summary>
        /// Specifies whether the following data is the last primitive in a GS packet.
        /// </summary>
        /// <remarks>
        /// <para>A value of 0 means that more GIFtags with more primitives will be sent after the current primitive.</para>
        /// <para>A value of 1 indicates that the following primitive data is the last in the packet.</para>
        /// </remarks>
        public bool Eop
        {
            get { return GetBoolVal(EOP_VAL); }
            set { SetBoolVal(EOP_VAL, value); }
        }

        /// <summary>
        /// Specifies whether the PRIM field in the GIFtag is enabled.
        /// </summary>
        /// <remarks>
        /// A value of 0 ignores the prim field, while a value of 1 outputs the PRIM field to the PRIM register.
        /// </remarks>
        public bool Pre
        {
            get { return GetBoolVal(PRE_VAL); }
            set { SetBoolVal(PRE_VAL, value); }
        }

        /// <summary>
        /// Contains data to be set in the PRIM register.
        /// </summary>
        /// <remarks>
        /// See <see cref="GifPrimitive"/> for more information.
        /// </remarks>
        public short Prim
        {
            get { return (short)GetVal(PRIM_VAL); }
            set { SetVal(PRIM_VAL, value); }
        }

        /// <summary>
        /// Contains the data format for the primitive.
        /// </summary>
        /// <remarks>
        /// See <see cref="GifFormat"/> for more information.
        /// </remarks>
        public GifFormat Flg
        {
            get { return (GifFormat)GetVal(NLOOP_VAL); }
            set { SetVal(NLOOP_VAL, (int)value); }
        }

        /// <summary>
        /// Holds the number of register descriptors (REGS), up to a maximum of 16 values.
        /// </summary>
        public int NReg
        {
            get { return GetVal(NREG_VAL); }
            set { SetVal(NREG_VAL, value); }
        }

        public GifTag(ulong data)
        {
            m_tag = data;
        }
    }
}
