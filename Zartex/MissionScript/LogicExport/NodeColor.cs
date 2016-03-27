using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zartex
{
    public class NodeColor
    {
        private byte[] _default = { 127, 127, 127, 255 };

        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public override bool Equals(object obj)
        {
            return (obj.GetHashCode() == GetHashCode());
        }

        public override int GetHashCode()
        {
            return (int)((R << 0) | (G << 8) | (B << 16) | (A << 24));
        }

        public override string ToString()
        {
            return String.Format("[{0},{1},{2},{3}]", R, G, B, A);
        }

        public static implicit operator System.Drawing.Color(NodeColor color) {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public NodeColor()
        {
            R = _default[0];
            G = _default[1];
            B = _default[2];
            A = _default[3];
        }

        public NodeColor(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public NodeColor(double r, double g, double b, double a)
        {
            R = (byte)(r * byte.MaxValue);
            G = (byte)(g * byte.MaxValue);
            B = (byte)(b * byte.MaxValue);
            A = (byte)(a * byte.MaxValue);
        }
    }
}
