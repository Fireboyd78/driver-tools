using System;

namespace DSCript.Models
{
    public enum PackageType : int
    {
        VehicleGlobals  = 0x1D,
        VehiclePackage  = 0x2D,
        VehicleStandard = 0xFF,

        SpooledModels,
    }
}
