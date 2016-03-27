using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GEO2Loader
{
    public enum BlockType : uint
    {
        GEO2 = 0x324F4547,
        MPAK = 0x4B41504D,
        TSC2 = 0x32435354
    }

    public enum ModelType : uint
    {
        VehiclePackage = 0xFF,
        Character = 0x23
    }

    public enum TextureSource : uint
    {
        VehiclePackage = 0xFFFD,
        VehicleGlobals = 0x1D
    }
}
