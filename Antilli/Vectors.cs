using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Antilli
{
    public class Vector2T
    {
        public double U { get; set; }
        public double V { get; set; }

        public Vector2T()
        {
        }

        public Vector2T(double u, double v)
        {
            U = u;
            V = v;
        }
    }

    public class Vector3
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vector3()
        {
        }

        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class Vector4
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }

        public Vector4()
        {
        }

        public Vector4(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }

    public class Vector4C
    {
        public double R { get; set; }
        public double G { get; set; }
        public double B { get; set; }
        public double A { get; set; }

        /// <summary>Returns a <see cref="System.Drawing.Color"/> based on the values in this vector. All values will be capped at 255 to prevent errors.</summary>
        public Color ColorFromARGB
        {
            get
            {
                int r = (int)Math.Round(R * 255.0);
                int g = (int)Math.Round(G * 255.0);
                int b = (int)Math.Round(B * 255.0);
                int a = (int)Math.Round(A * 255.0);

                return Color.FromArgb(
                    (a > 255) ? 255 : a,
                    (r > 255) ? 255 : r,
                    (g > 255) ? 255 : g,
                    (b > 255) ? 255 : b
                    );
            }
        }

        /// <summary>Creates a <see cref="Vector4C"/> with the values initialized to 0.0</summary>
        public Vector4C()
        {
            R = 0.0;
            G = 0.0;
            B = 0.0;
            A = 0.0;
        }

        /// <summary>Creates a <see cref="Vector4C"/> with the given values. These values should not exceed 1.0.</summary>
        /// <param name="r">The Red component of this color</param>
        /// <param name="g">The Green component of this color</param>
        /// <param name="b">The Blue component of this color</param>
        /// <param name="a">The Alpha component of this color</param>
        public Vector4C(double r, double g, double b, double a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }


        /// <summary>Creates a <see cref="Vector4C"/> based on the given <see cref="System.Drawing.Color"/>.</summary>
        /// <param name="color">The color that will be converted</param>
        public Vector4C(Color color)
        {
            R = color.R / 255.0;
            G = color.G / 255.0;
            B = color.B / 255.0;
            A = color.A / 255.0;
        }
    }

    public class Vertex
    {
        /// <summary>Gets or sets the position of the vertex</summary>
        public Vector3 Position { get; set; }

        /// <summary>Gets or sets the normals of the vertex</summary>
        public Vector3 Normals { get; set; }

        /// <summary>Gets or sets the UV mapping of the vertex</summary>
        public Vector2T UVMap { get; set; }

        /// <summary>Gets or sets the specular color of the vertex (RGBA)</summary>
        public Vector4C Specular { get; set; }

        /// <summary>Creates a new <see cref="Vertex"/> with the values initialized to 0.0.</summary>
        public Vertex()
        {
            Position = new Vector3(0.0, 0.0, 0.0);
            Normals = new Vector3(0.0, 0.0, 0.0);
            UVMap = new Vector2T(0.0, 0.0);
            Specular = new Vector4C(0.0, 0.0, 0.0, 0.0);
        }
    }

    public class Vertex15 : Vertex
    {
        /// <summary>Gets or sets the blending weights of the vertex</summary>
        public Vector3 BlendWeights { get; set; }

        /// <summary>Creates a new <see cref="Vertex15"/> with the values initialized to 0.0.</summary>
        public Vertex15()
        {
            Position = new Vector3(0.0, 0.0, 0.0);
            Normals = new Vector3(0.0, 0.0, 0.0);
            UVMap = new Vector2T(0.0, 0.0);
            BlendWeights = new Vector3(0.0, 0.0, 0.0);
            Specular = new Vector4C(0.0, 0.0, 0.0, 0.0);
        }
    }

    public class Vertex16 : Vertex15
    {
        /// <summary>Gets or sets the unknown value of the vertex</summary>
        public double Unknown { get; set; }

        public Vertex16()
        {
            Position = new Vector3(0.0, 0.0, 0.0);
            Normals = new Vector3(0.0, 0.0, 0.0);
            UVMap = new Vector2T(0.0, 0.0);
            BlendWeights = new Vector3(0.0, 0.0, 0.0);
            Specular = new Vector4C(0.0, 0.0, 0.0, 0.0);
            Unknown = 0.0;
        }
    }
}
