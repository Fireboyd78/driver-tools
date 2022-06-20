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
            return ToString(":");
        }

        public string ToString(string separator)
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

            return $"{High:X8}{separator}{id1:X4}{separator}{id2:X4}";
        }

        public UID(int low, int high)
        {
            Low = low;
            High = high;
        }
    }

    public struct BBox
    {
        public static readonly Vector4 VectorZero = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        public Vector4 V11, V12, V13, V14;
        public Vector4 V21, V22, V23, V24;

        public void SetDefaults()
        {
            V11 = VectorZero;
            V12 = VectorZero;
            V13 = VectorZero;
            V14 = VectorZero;

            V21 = VectorZero;
            V22 = VectorZero;
            V23 = VectorZero;
            V24 = VectorZero;
        }
    }

    public class Model : ICopyCat<Model>
    {
        bool ICopyCat<Model>.CanCopy(CopyClassType copyType)                => true;
        bool ICopyCat<Model>.CanCopyTo(Model obj, CopyClassType copyType)   => true;
        
        bool ICopyCat<Model>.IsCopyOf(Model obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        Model ICopyClass<Model>.Copy(CopyClassType copyType)
        {
            var model = new Model();
            
            CopyTo(model, copyType);

            return model;
        }

        public void CopyTo(Model obj, CopyClassType copyType = CopyClassType.SoftCopy)
        {
            obj.UID = UID;
            obj.Scale = Scale;
            obj.VertexType = VertexType;
            obj.Flags = Flags;
            obj.BoundingBox = BoundingBox;

            // **** REUSE VERTEX BUFFER DUE TO POOR DESIGN CHOICES ****
            obj.VertexBuffer = VertexBuffer;

            var lods = new List<Lod>(7);

            if (copyType == CopyClassType.DeepCopy)
            {
                foreach (var _lod in Lods)
                {
                    var lod = new Lod(_lod.Type)
                    {
                        // parent to NEW model
                        // allows copy operation to reparent for us
                        Parent = obj,
                    };

                    // DEEP COPY: all new instances down the line
                    CopyCatFactory.CopyToB(_lod, lod, CopyClassType.DeepCopy);

                    // add new lod
                    lods.Add(lod);
                }
            }
            else
            {
                // reuse lod references
                lods.AddRange(Lods);
            }

            obj.Lods = lods;
        }

        public UID UID;

        // only used in DPL!?
        public Vector4 Scale;

        /// <summary>
        /// Gets or sets the Vertex Buffer to use when accessing vertices
        /// </summary>
        public VertexBuffer VertexBuffer { get; set; }
        
        // type of vertices in the buffer
        // resolves to a vertex declaration
        public short VertexType { get; set; }

        public int Flags { get; set; }

        public BBox BoundingBox;

        public List<Lod> Lods { get; set; }

        public Model()
        {
            Lods = new List<Lod>(7);

            // pre-allocate the data
            for (int i = 0; i < 7; i++)
                Lods.Add(new Lod(i));
        }
    }
}
