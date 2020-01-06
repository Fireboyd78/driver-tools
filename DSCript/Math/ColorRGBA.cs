using System.Drawing;

namespace DSCript.Models
{
    public struct ColorRGBA
    {
        byte R;
        byte G;
        byte B;
        byte A;

        public static implicit operator Color(ColorRGBA color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static implicit operator ColorRGBA(Color color)
        {
            return new ColorRGBA(color.R, color.G, color.B, color.A);
        }

        public static implicit operator Vector4(ColorRGBA color)
        {
            var fR = (color.R / 255.999f);
            var fG = (color.G / 255.999f);
            var fB = (color.B / 255.999f);
            var fA = (color.A / 255.999f);

            return new Vector4(fR, fG, fB, fA);
        }

        public static implicit operator ColorRGBA(Vector4 color)
        {
            var r = (byte)(color.X * 255.999f);
            var g = (byte)(color.Y * 255.999f);
            var b = (byte)(color.Z * 255.999f);
            var a = (byte)(color.W * 255.999f);

            return new ColorRGBA(r, g, b, a);
        }

        public ColorRGBA(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }
}
