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

namespace Antilli.Models
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

    public class VertexData
    {
        /// <summary>
        /// Gets the length of each <see cref="Vertex"/> entry.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="Vertex"/> buffer.
        /// </summary>
        public Vertex[] Buffer { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Vertex"/> at the specified index.
        /// </summary>
        /// <param name="id">The index into the buffer.</param>
        /// <returns>The vertex at the specified index in the buffer.</returns>
        public Vertex this[int id]
        {
            get { return Buffer[id]; }
            set { Buffer[id] = value; }
        }

        /// <summary>
        /// Gets the <see cref="FVFType"/> of the vertices. The returned value is based on the 'Length' property.
        /// </summary>
        public FVFType VertexType
        {
            get { return Enum.IsDefined(typeof(FVFType), Length) ? (FVFType)Length : FVFType.Unknown; }
        }

        /// <summary>
        /// Creates a new vertex buffer used for storing vertices.
        /// </summary>
        /// <param name="count">The number of vertices in the buffer.</param>
        /// <param name="length">The length of each <see cref="Vertex"/> in the buffer. This should be based on an existing <see cref="FVFType"/>.</param>
        public VertexData(int count, int length)
        {
            Buffer = new Vertex[count];
            Length = length;
        }
    }
}
