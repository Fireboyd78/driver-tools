using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GMC2Snooper.PS2
{
    public enum VifAdditionMode
    {
        Normal      = 0,
        Offset      = 1,
        Difference  = 2,
        Undefined   = 3,
    }

    public enum VifWriteMaskType
    {
        Data,
        Row,
        Column,
        Protect,
    }

    public struct VifCycle
    {
        private int m_cycl;

        public static readonly int[] CL_VAL = { 0, 0x3F };
        public static readonly int[] WL_VAL = { 8, 0x3FF };

        private int GetVal(int[] info)
        {
            return ((m_cycl >> info[0]) & info[1]);
        }

        private void SetVal(int[] info, int value)
        {
            m_cycl &= (ushort)(~(info[1] << info[0]));
            m_cycl |= (ushort)((value & info[1]) << info[0]);
        }

        public int Length
        {
            get { return GetVal(CL_VAL); }
            set { SetVal(CL_VAL, value); }
        }

        public int WriteLength
        {
            get { return GetVal(WL_VAL); }
            set { SetVal(WL_VAL, value); }
        }

        public VifCycle(int data)
        {
            m_cycl = data;
        }
    }

    public struct VifWriteMask
    {
        private int m_mask;

        public VifWriteMaskType this[int m]
        {
            get
            {
                return (VifWriteMaskType)((m_mask >> (m * 2)) & 0x3);
            }
        }

        public static explicit operator int (VifWriteMask mask)
        {
            return mask.m_mask;
        }
        
        public VifWriteMask(int data)
        {
            m_mask = data;
        }
    }

    public delegate void VifUnpackHandler(VifParser parser, VifUnpackType packType, bool flag, bool masked, long[][] values);

    public class VifParser
    {
        /// <summary>
        /// The cycle information for unpacking data.
        /// </summary>
        /// <remarks>VIFn_CYCLE</remarks>
        public VifCycle Cycle { get; set; }

        /// <summary>
        /// The masking pattern for unpacking data.
        /// </summary>
        /// /// <remarks>VIFn_MASK</remarks>
        public VifWriteMask Mask { get; set; }

        /// <summary>
        /// The addition processing mode for unpacking data.
        /// </summary>
        /// <remarks>VIFn_MODE</remarks>
        public VifAdditionMode Mode { get; set; }

        // VIF1_STAT-> DBF
        public bool DoubleBuffered { get; set; }

        public int Addr { get; set; }

        /// <summary>
        /// The base address of the double-buffered data.
        /// </summary>
        /// <remarks>VIF1_BASE</remarks>
        public int Base { get; set; }

        /// <summary>
        /// The offset into the double-buffered data.
        /// </summary>
        /// <remarks>VIF1_OFST</remarks>
        public int Offset { get; set; }

        /// <summary>
        /// The current data address.
        /// </summary>
        /// <remarks>VIFn_ITOP</remarks>
        public int ITop { get; set; }

        /// <summary>
        /// The next available data address.
        /// </summary>
        /// <remarks>VIFn_ITOPS</remarks>
        public int ITops { get; set; }

        /// <summary>
        /// The current data buffer address.
        /// </summary>
        /// <remarks>VIF1_TOP</remarks>
        public int Top { get; set; }

        /// <summary>
        /// The current unpacked data buffer address.
        /// </summary>
        /// <remarks>VIF1_TOPS</remarks>
        public int Tops { get; set; }

        /// <summary>
        /// The current number of unpacked values in the buffer.
        /// </summary>
        /// <remarks>VIF1_NUM</remarks>
        public int Num { get; set; }
        
        /// <summary>
		/// The current set of rows.
		/// </summary>
		/// <remarks>STROW</remarks>
        public int[] Rows { get; set; }

        /// <summary>
		/// The current set of cols.
		/// </summary>
		/// <remarks>STCOL</remarks>
        public int[] Cols { get; set; }
        
        /// <summary>
        /// The most recently processed VIFcode.
        /// </summary>
        public VifTag Code { get; set; }
        
        public VifWriteMaskType GetWriteMask(int kind)
        {
            if (kind < 0 || kind > 4)
                throw new InvalidOperationException("Bad masking field, must be 0-3!");

            switch (Cycle.Length)
            {
            case 1:
                return Mask[kind];
            case 2:
                return Mask[kind + 4];
            case 3:
                return Mask[kind + 8];
            default:
                return Mask[kind + 12];
            }
        }

        /// <summary>
        /// Reads in the next VIFcode and updates the state machine.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        public bool ReadTag(Stream stream, VifUnpackHandler unpackHandler)
        {
            if (unpackHandler == null)
                throw new ArgumentNullException(nameof(unpackHandler), "Unpack handler cannot be null.");

            Code = stream.ReadStruct<VifTag>();

            var imdt = new VifImmediate(Code.IMDT);
            var cmd = new VifCommand(Code.CMD);

            Addr = imdt.ADDR;

            // set when we encounter something not implemented/supported
            var unhandled = false;

            switch ((VifCommandType)Code.CMD)
            {
            case VifCommandType.Nop:
                // no operation
                break;
            case VifCommandType.StCycl:
                Cycle = new VifCycle(Code.IMDT);
                break;
            case VifCommandType.Offset:
                Offset = imdt.IMDT_OFFSET;

                Base = Tops;
                DoubleBuffered = false;
                break;
            case VifCommandType.Base:
                Base = imdt.IMDT_BASE;
                break;
            case VifCommandType.ITop:
                ITops = imdt.IMDT_ITOP;
                break;
            case VifCommandType.StMod:
                Mode = (VifAdditionMode)imdt.IMDT_STMOD;
                break;

            case VifCommandType.MskPath3:
            case VifCommandType.Mark:
                //unhandled = true;
                break;

            // no need to handle these
            case VifCommandType.FlushE:
            case VifCommandType.Flush:
            case VifCommandType.FlushA:
                break;

            case VifCommandType.MsCal:
            case VifCommandType.MsCalf:
            case VifCommandType.MsCnt:
                ITop = ITops;
                Top = Tops;

                Tops = (DoubleBuffered) ? (Base + Offset) : Base;
                DoubleBuffered = !DoubleBuffered;
                break;

            case VifCommandType.StMask:
                Mask = stream.ReadStruct<VifWriteMask>();
                break;

            case VifCommandType.StRow:
                Rows = new int[4];

                for (int i = 0; i < Rows.Length; i++)
                    Rows[i] = stream.ReadInt32();

                break;

            case VifCommandType.StCol:
                Cols = new int[4];

                for (int i = 0; i < Cols.Length; i++)
                    Cols[i] = stream.ReadInt32();

                break;

            // don't know how to handle this (yet), will cause crashes if we continue
            case VifCommandType.Mpg:
                throw new InvalidOperationException("MPG not implemented.");

            case VifCommandType.Direct:
            case VifCommandType.DirectHL:
                stream.Position += ((imdt.IMDT_DIRECT * 16) + 4);
                unhandled = true;
                break;

            default:
                if (cmd.P == 3)
                {
                    var packType = cmd.GetUnpackDataType();

                    if (packType == VifUnpackType.Invalid)
                    {
                        throw new InvalidOperationException($"Invalid VIF unpack type!");
                    }
                    else
                    {
                        // packSize and packNum can be -1,
                        // but not since we're checking against invalid types
                        var packSize = cmd.GetUnpackDataSize();
                        var packNum = cmd.GetUnpackDataCount();

                        if (packNum > 4)
                            throw new InvalidOperationException("too many packed values!!!");

                        var num = Code.NUM;
                        var vals = new long[num][];

                        // initialize arrays
                        for (int v = 0; v < vals.Length; v++)
                        {
                            vals[v] = new long[packNum];

                            for (int n = 0; n < packNum; n++)
                                vals[v][n] = 0;
                        }

                        for (int i = 0; i < num; i++)
                        {
                            switch (packSize)
                            {
                            // byte
                            case 1:
                                {
                                    for (int n = 0; n < packNum; n++)
                                    {
                                        long val = (imdt.USN) ? stream.ReadByte() : (sbyte)stream.ReadByte();

                                        if (imdt.FLG && (val > 127))
                                            val -= 128;
                                        
                                        vals[i][n] = val;
                                    }
                                }
                                break;
                            // short
                            case 2:
                                {
                                    for (int n = 0; n < packNum; n++)
                                    {
                                        long val = (imdt.USN) ? stream.ReadUInt16() : (long)stream.ReadInt16();

                                        vals[i][n] = val;
                                    }
                                }
                                break;
                            // int
                            case 4:
                                {
                                    for (int n = 0; n < packNum; n++)
                                    {
                                        long val = (imdt.USN) ? stream.ReadUInt32() : (long)stream.ReadInt32();

                                        vals[i][n] = val;
                                    }
                                }
                                break;
                            }
                        }

                        unpackHandler(this, packType, imdt.FLG, (cmd.M == 1), vals);
                    }
                }
#if THROW_ON_INVALID_UNPACK_COMMANDS
                else
                {
                    throw new InvalidOperationException("Something went wrong when parsing a VIF tag!");
                }
#endif
                break;
            }
            
            // verify we handled everything
            return !unhandled;
        }

        public string DumpRegisters()
        {
            return $"BASE={Base:X},OFFSET={Offset:X},DBF={(DoubleBuffered ? 1 : 0)},ITOPS={ITops:X},ITOP={ITop:X},TOPS={Tops:X},TOP={Top:X}";
        }
    }
}
