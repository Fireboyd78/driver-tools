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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DSCript.Models
{
    public struct UID : IEquatable<UID>
    {
        public int Low;
        public int High;

        public bool Equals(UID other)
        {
            return ((other.Low == Low) && (other.High == High));
        }

        public override bool Equals(object obj)
        {
            if (obj is UID)
                return Equals((UID)obj);

            return false;
        }

        public static bool operator ==(UID lhs, UID rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(UID lhs, UID rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            // I like to live dangerously ;)
            return Low ^ High;
        }
        
        public override string ToString()
        {
            //var bin = (Low >> 24) & 0xFF;
            //var grp = (Low >> 16) & 0xFF;

            /*
            var u1 = (High >> 24) & 0xFF;
            var u2 = High & 0xFFFFFF;

            var id1 = ((Low >> 24) & 0xFF) | (u2 << 8);
            var id2 = (Low >> 16) & 0xFF;
            var id3 = (Low & 0x3FFF);
            */

            //--var id1 = (High >> 8) & 0xFFFFFF;
            //--var id2 = High & 0xFF;
            //--
            //--var id3 = (Low & 0xFFFF);
            //--var id4 = (Low >> 16) & 0xFFFF;
            //--
            //--return $"{id1:X6}:{id2:X2}:{id3:X4}:{id4:X4}";

            var id1 = (Low >> 16) & 0xFFFF;
            var id2 = (Low & 0xFFFF);

            return $"{High:X8}:{id1:X4}:{id2:X4}";
        }

        public UID(int low, int high)
        {
            Low = low;
            High = high;
        }
    }

    public class Model
    {
        public UID UID;
        
        // only used in DPL!?
        public Vector4 Scale { get; set; }

        /// <summary>
        /// Gets or sets the Vertex Buffer to use when accessing vertices
        /// </summary>
        public VertexBuffer VertexBuffer { get; set; }
        
        // type of vertices in the buffer
        // resolves to a vertex declaration
        public short VertexType { get; set; }

        public int Flags { get; set; }

        // something shadow related?
        public int Unknown3 { get; set; }

        public Vector4[] Transform { get; set; } = new Vector4[8];
        public Lod[] Lods { get; set; } = new Lod[7];
    }
}
