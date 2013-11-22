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
        public int Length { get; private set; }

        public Vertex[] Buffer { get; set; }

        public Vertex this[int id]
        {
            get { return Buffer[id]; }
            set { Buffer[id] = value; }
        }

        public FVFType VertexType
        {
            get { return Enum.IsDefined(typeof(FVFType), Length) ? (FVFType)Length : FVFType.Unknown; }
        }

        public VertexData(int count, int length)
        {
            Buffer = new Vertex[count];
            Length = length;
        }
    }
}
