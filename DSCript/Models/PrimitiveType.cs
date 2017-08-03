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
    /// Defines the primitives supported by Direct3D. 
    /// Source: http://msdn.microsoft.com/en-us/library/windows/desktop/bb172589%28v=vs.85%29.aspx
    /// </summary>
    public enum D3DPRIMITIVETYPE : int
    {
        /// <summary>
        /// Renders the vertices as a collection of isolated points. This value is unsupported for indexed primitives.
        /// </summary>
        D3DPT_POINTLIST = 1,

        /// <summary>
        /// Renders the vertices as a list of isolated straight line segments.
        /// </summary>
        D3DPT_LINELIST = 2,

        /// <summary>
        /// Renders the vertices as a single polyline.
        /// </summary>
        D3DPT_LINESTRIP = 3,

        /// <summary>
        /// Renders the specified vertices as a sequence of isolated triangles. Each group of three vertices defines a separate triangle.
        /// 
        /// Back-face culling is affected by the current winding-order render state.
        /// </summary>
        D3DPT_TRIANGLELIST = 4,

        /// <summary>
        /// Renders the vertices as a triangle strip. The backface-culling flag is automatically flipped on even-numbered triangles.
        /// </summary>
        D3DPT_TRIANGLESTRIP = 5,

        /// <summary>
        /// Renders the vertices as a triangle fan. 
        /// </summary>
        D3DPT_TRIANGLEFAN = 6,

        /// <summary>
        /// Forces this enumeration to compile to 32 bits in size. Without this value, some compilers would allow this enumeration to compile to a size other than 32 bits. This value is not used. 
        /// </summary>
        D3DPT_FORCE_DWORD = 0x7fffffff
    }
}
