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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Antilli
{
    public class Vector2T
    {
        public double U { get; set; }
        public double V { get; set; }

        public System.Windows.Point ToPoint()
        {
            return new System.Windows.Point(U, V);
        }

        /// <summary>Creates a <see cref="Vector2T"/> with the values initialized to 0.0</summary>
        public Vector2T()
        {
            U = 0.0;
            V = 0.0;
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

        public Point3D ToPoint3D()
        {
            return ToPoint3D(false);
        }

        public Point3D ToPoint3D(bool negX)
        {
            return ToPoint3D(negX, new Point3D(0.0, 0.0, 0.0));
        }

        public Point3D ToPoint3D(bool negX, Point3D blendWeights)
        {
            return new Point3D(
                    ((negX) ? -X : X + (blendWeights.X * 1.0)),
                    (Z + (blendWeights.Z * 1.0)),
                    (Y + (blendWeights.Y * 1.0))
                );
        }

        public Vector3D ToVector3D()
        {
            return ToVector3D(false);
        }

        public Vector3D ToVector3D(bool negX)
        {
            return new Vector3D((negX) ? -X : X, Z, Y);
        }

        /// <summary>Creates a <see cref="Vector3"/> with the values initialized to 0.0</summary>
        public Vector3()
        {
            X = 0.0;
            Y = 0.0;
            Z = 0.0;
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

        public Point4D ToPoint4D()
        {
            return new Point4D(X, Y, Z, W);
        }

        /// <summary>Creates a <see cref="Vector4"/> with the values initialized to 0.0</summary>
        public Vector4()
        {
            X = 0.0;
            Y = 0.0;
            Z = 0.0;
            W = 0.0;
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
        public System.Drawing.Color ColorFromARGB
        {
            get
            {
                int r = (int)Math.Round(R * 255.0);
                int g = (int)Math.Round(G * 255.0);
                int b = (int)Math.Round(B * 255.0);
                int a = (int)Math.Round(A * 255.0);

                return System.Drawing.Color.FromArgb(
                    a < 255 ? a : 255,
                    r < 255 ? r : 255,
                    g < 255 ? g : 255,
                    b < 255 ? b : 255 
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
        public Vector4C(System.Drawing.Color color)
        {
            R = color.R / 255.0;
            G = color.G / 255.0;
            B = color.B / 255.0;
            A = color.A / 255.0;
        }
    }

    public class VertexOld
    {
        /// <summary>Gets or sets the position of the vertex</summary>
        public Vector3 Position { get; set; }

        /// <summary>Gets or sets the normals of the vertex</summary>
        public Vector3 Normals { get; set; }

        /// <summary>Gets or sets the UV mapping of the vertex</summary>
        public Vector2T UVMap { get; set; }

        /// <summary>Gets or sets the specular color of the vertex (RGBA)</summary>
        public Vector4C Specular { get; set; }

        /// <summary>Creates a new <see cref="VertexOld"/> with the values initialized to 0.0.</summary>
        public VertexOld()
        {
            Position = new Vector3(0.0, 0.0, 0.0);
            Normals = new Vector3(0.0, 0.0, 0.0);
            UVMap = new Vector2T(0.0, 0.0);
            Specular = new Vector4C(0.0, 0.0, 0.0, 0.0);
        }
    }

    public class Vertex15 : VertexOld
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
