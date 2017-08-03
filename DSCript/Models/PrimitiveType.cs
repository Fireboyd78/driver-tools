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
    public enum PrimitiveType : int
    {
        PointList       = 1,
        LineList        = 2,
        LineStrip       = 3,
        TriangleList    = 4,
        TriangleStrip   = 5,
        TriangleFan     = 6,
    }
}
