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
    /// <summary>
    /// An enumeration defining different types of vertices used in DRIV3R/Driver: Parallel Lines.
    /// </summary>
    public enum FVFType : int
    {
        /// <summary>Contains data for Position, Normals, Mapping, and Specular.</summary>
        Vertex12 = 0x30,

        /// <summary>Contains data for Position, Normals, Mapping, Blend Weights, and Specular.</summary>
        Vertex15 = 0x3C,

        /// <summary>Contains data for Position, Normals, Mapping, Blend Weights, Specular, and Unknown.</summary>
        Vertex16 = 0x40,

        /// <summary>Represents an unknown type.</summary>
        Unknown = -1
    }
}
