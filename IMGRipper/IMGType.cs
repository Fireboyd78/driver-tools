using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMGRipper
{
    public enum IMGType
    {
        IMG2 = 0x32474D49,
        IMG3 = 0x33474D49,
        IMG4 = 0x34474D49
    }

    public enum IMGDataType
    {
        Packed, // all data inside IMG file
        Lumped  // data is located inside external files
    }

    public enum IMGVersion
    {
        Unknown = 0,

        IMG2 = 2,
        IMG3 = 3,
        IMG4 = 4
    }
}
