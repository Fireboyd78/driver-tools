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

        public bool HasBlendWeights
        {
            get { return (Buffer != null) ? (VertexType == FVFType.Vertex15) : false; }
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
