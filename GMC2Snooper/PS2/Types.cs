using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GMC2Snooper.PS2
{
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 16)]
    public unsafe struct QTag
    {
        [FieldOffset(0)] public fixed byte Byte[16];
        [FieldOffset(0)] public fixed ushort HalfWord[8];
        [FieldOffset(0)] public fixed uint SingleWord[4];
        [FieldOffset(0)] public fixed ulong DoubleWord[2];
        [FieldOffset(0)] public fixed ulong QuadWord[4];
    }
}