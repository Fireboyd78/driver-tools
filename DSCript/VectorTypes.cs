using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DSCript
{
    public struct Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public static bool Equals(Vector2 vector1, Vector2 vector2)
        {
            return (vector1 == vector2);
        }

        public bool Equals(Vector2 value)
        {
            return Equals(this, value);
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is Vector2))
                return false;

            return Equals(this, (Vector2)o);
        }

        public static Vector2 Add(Vector2 vector1, Vector2 vector2)
        {
            return new Vector2(
                vector1.X + vector2.X,
                vector1.Y + vector2.Y
            );
        }

        public static Vector2 Subtract(Vector2 vector1, Vector2 vector2)
        {
            return new Vector2(
                vector1.X - vector2.X,
                vector1.Y - vector2.Y
            );
        }

        public static Vector2 Multiply(Vector2 vector, float scalar)
        {
            return new Vector2(
                vector.X * scalar,
                vector.Y * scalar
            );
        }

        public static Vector2 operator +(Vector2 vector1, Vector2 vector2)
        {
            return Add(vector1, vector2);
        }

        public static Vector2 operator -(Vector2 vector1, Vector2 vector2)
        {
            return Subtract(vector1, vector2);
        }

        public static Vector2 operator *(Vector2 vector, float scalar)
        {
            return Multiply(vector, scalar);
        }

        public static bool operator ==(Vector2 vector1, Vector2 vector2)
        {
            return (vector1.X == vector2.X)
                && (vector1.Y == vector2.Y);
        }

        public static bool operator !=(Vector2 vector1, Vector2 vector2)
        {
            return !(vector1 == vector2);
        }
        
        public static implicit operator Point(Vector2 value)
        {
            return new Point(value.X, value.Y);
        }
        
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public static bool Equals(Vector3 vector1, Vector3 vector2)
        {
            return (vector1 == vector2);
        }

        public bool Equals(Vector3 value)
        {
            return Equals(this, value);
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is Vector3))
                return false;

            return Equals(this, (Vector3)o);
        }

        public static Vector3 Add(Vector3 vector1, Vector3 vector2)
        {
            return new Vector3(
                vector1.X + vector2.X,
                vector1.Y + vector2.Y,
                vector1.Z + vector2.Z
            );
        }

        public static Vector3 Subtract(Vector3 vector1, Vector3 vector2)
        {
            return new Vector3(
                vector1.X - vector2.X,
                vector1.Y - vector2.Y,
                vector1.Z - vector2.Z
            );
        }

        public static Vector3 Multiply(Vector3 vector, float scalar)
        {
            return new Vector3(
                vector.X * scalar,
                vector.Y * scalar,
                vector.Z * scalar
            );
        }

        public static Vector3 operator +(Vector3 vector1, Vector3 vector2)
        {
            return Add(vector1, vector2);
        }

        public static Vector3 operator -(Vector3 vector1, Vector3 vector2)
        {
            return Subtract(vector1, vector2);
        }

        public static Vector3 operator *(Vector3 vector, float scalar)
        {
            return Multiply(vector, scalar);
        }

        public static bool operator ==(Vector3 vector1, Vector3 vector2)
        {
            return (vector1.X == vector2.X)
                && (vector1.Y == vector2.Y)
                && (vector1.Z == vector2.Z);
        }

        public static bool operator !=(Vector3 vector1, Vector3 vector2)
        {
            return !(vector1 == vector2);
        }

        public static implicit operator Point3D(Vector3 value)
        {
            return new Point3D(value.X, value.Y, value.Z);
        }

        public static implicit operator Vector3D(Vector3 value)
        {
            return new Vector3D(value.X, value.Y, value.Z);
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct Vector4
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
        }

        public static bool Equals(Vector4 vector1, Vector4 vector2)
        {
            return (vector1 == vector2);
        }

        public bool Equals(Vector4 value)
        {
            return Equals(this, value);
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is Vector4))
                return false;

            return Equals(this, (Vector4)o);
        }
        
        public static Vector4 Add(Vector4 vector1, Vector4 vector2)
        {
            return new Vector4(
                vector1.X + vector2.X,
                vector1.Y + vector2.Y,
                vector1.Z + vector2.Z,
                vector1.W + vector2.W
            );
        }

        public static Vector4 Subtract(Vector4 vector1, Vector4 vector2)
        {
            return new Vector4(
                vector1.X - vector2.X,
                vector1.Y - vector2.Y,
                vector1.Z - vector2.Z,
                vector1.W + vector2.W
            );
        }

        public static Vector4 Multiply(Vector4 vector, float scalar)
        {
            return new Vector4(
                vector.X * scalar,
                vector.Y * scalar,
                vector.Z * scalar,
                vector.W * scalar
            );
        }

        public static Vector4 operator +(Vector4 vector1, Vector4 vector2)
        {
            return Add(vector1, vector2);
        }

        public static Vector4 operator -(Vector4 vector1, Vector4 vector2)
        {
            return Subtract(vector1, vector2);
        }

        public static Vector4 operator *(Vector4 vector, float scalar)
        {
            return Multiply(vector, scalar);
        }

        public static bool operator ==(Vector4 vector1, Vector4 vector2)
        {
            return (vector1.X == vector2.X)
                && (vector1.Y == vector2.Y)
                && (vector1.Z == vector2.Z)
                && (vector1.W == vector2.W);
        }

        public static bool operator !=(Vector4 vector1, Vector4 vector2)
        {
            return !(vector1 == vector2);
        }

        public static implicit operator Point4D(Vector4 value)
        {
            return new Point4D(value.X, value.Y, value.Z, value.W);
        }

        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }
}
