using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DSCript.Models
{
    public enum PackageType : uint
    {
        Type0           = 0x00,
        Type1           = 0x01,
        Type2           = 0x02,
        Type3           = 0x03,
        Type4           = 0x04,
        Type5           = 0x05,
        Type6           = 0x06,
        Type7           = 0x07,
        Type8           = 0x08,
        Type9           = 0x09,
        Type10          = 0x0A,
        Type11          = 0x0B,
        Type12          = 0x0C,
        Type13          = 0x0D,
        Type14          = 0x0E,
        Type30          = 0x1E,
        Type35          = 0x23,
        Type40          = 0x28,
        Type44          = 0x2C,
        Type52          = 0x34,
        Type53          = 0x35,
        Type56          = 0x38,
        Type57          = 0x39,
        Type59          = 0x3B,
        Type60          = 0x3C,
        Type61          = 0x3D,
        Type62          = 0x3E,

        VehicleGlobals  = 0x1D,
        VehiclePackage  = 0x2D,
        VehicleStandard = 0xFF,

        Unknown         = 0x7FFFFFFF
    }
}
