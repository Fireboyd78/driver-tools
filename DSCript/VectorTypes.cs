using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DSCript
{
    public struct Vector2 : IEquatable<Vector2>
    {
        public float X;
        public float Y;

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public static bool Equals(Vector2 left, Vector2 right)
        {
            return left.Equals(right);
        }

        public bool Equals(Vector2 other)
        {
            return (other.X == X)
                && (other.Y == Y);
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

        public static Vector2 Multiply(Vector2 vector1, Vector2 vector2)
        {
            return new Vector2(
                vector1.X * vector2.X,
                vector1.Y * vector2.Y
            );
        }

        public static Vector2 Multiply(Vector2 vector, float scalar)
        {
            return new Vector2(
                vector.X * scalar,
                vector.Y * scalar
            );
        }

        public static Vector2 Scale(Vector2 vector1, Vector2 vector2)
        {
            return new Vector2(
                vector1.X / vector2.X,
                vector1.Y / vector2.Y
            );
        }

        public static Vector2 Scale(Vector2 vector, float scalar)
        {
            return new Vector2(
                vector.X / scalar,
                vector.Y / scalar
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

        public static Vector2 operator *(Vector2 vector1, Vector2 vector2)
        {
            return Multiply(vector1, vector2);
        }

        public static Vector2 operator *(Vector2 vector, float scalar)
        {
            return Multiply(vector, scalar);
        }

        public static Vector2 operator /(Vector2 vector1, Vector2 vector2)
        {
            return Scale(vector1, vector2);
        }

        public static Vector2 operator /(Vector2 vector, float scalar)
        {
            return Scale(vector, scalar);
        }

        public static bool operator ==(Vector2 left, Vector2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector2 left, Vector2 right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Point(Vector2 value)
        {
            return new Point(value.X, value.Y);
        }
        
        public override string ToString()
        {
            return $"{X:F4},{Y:F4}";
        }
        
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public struct Vector3 : IEquatable<Vector3>
    {
        public float X;
        public float Y;
        public float Z;

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public static bool Equals(Vector3 left, Vector3 right)
        {
            return left.Equals(right);
        }

        public bool Equals(Vector3 other)
        {
            return (other.X == X)
                && (other.Y == Y)
                && (other.Z == Z);
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

        public static Vector3 Multiply(Vector3 vector1, Vector3 vector2)
        {
            return new Vector3(
                vector1.X * vector2.X,
                vector1.Y * vector2.Y,
                vector1.Z * vector2.Z
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

        public static Vector3 Scale(Vector3 vector1, Vector3 vector2)
        {
            return new Vector3(
                vector1.X / vector2.X,
                vector1.Y / vector2.Y,
                vector1.Z / vector2.Z
            );
        }

        public static Vector3 Scale(Vector3 vector, float scalar)
        {
            return new Vector3(
                vector.X / scalar,
                vector.Y / scalar,
                vector.Z / scalar
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

        public static Vector3 operator *(Vector3 vector1, Vector3 vector2)
        {
            return Multiply(vector1, vector2);
        }

        public static Vector3 operator *(Vector3 vector, float scalar)
        {
            return Multiply(vector, scalar);
        }

        public static Vector3 operator /(Vector3 vector1, Vector3 vector2)
        {
            return Scale(vector1, vector2);
        }

        public static Vector3 operator /(Vector3 vector, float scalar)
        {
            return Scale(vector, scalar);
        }

        public static bool operator ==(Vector3 left, Vector3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3 left, Vector3 right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Point3D(Vector3 value)
        {
            return new Point3D(value.X, value.Y, value.Z);
        }

        public static implicit operator Vector3D(Vector3 value)
        {
            return new Vector3D(value.X, value.Y, value.Z);
        }

        public static implicit operator Color(Vector3 value)
        {
            return Color.FromScRgb(1.0f, value.X, value.Y, value.Z);
        }

        public static implicit operator Vector3(Vector4 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }
        
        public override string ToString()
        {
            return $"{X:F4},{Y:F4},{Z:F4}";
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct Vector4 : IEquatable<Vector4>
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
        }

        public static bool Equals(Vector4 left, Vector4 right)
        {
            return left.Equals(right);
        }

        public bool Equals(Vector4 other)
        {
            return (other.X == X)
                && (other.Y == Y)
                && (other.Z == Z)
                && (other.W == W);
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is Vector4))
                return false;

            return Equals((Vector4)o);
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
                vector1.W - vector2.W
            );
        }

        public static Vector4 Multiply(Vector4 vector1, Vector4 vector2)
        {
            return new Vector4(
                vector1.X * vector2.X,
                vector1.Y * vector2.Y,
                vector1.Z * vector2.Z,
                vector1.W * vector2.W
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

        public static Vector4 Scale(Vector4 vector1, Vector4 vector2)
        {
            return new Vector4(
                vector1.X / vector2.X,
                vector1.Y / vector2.Y,
                vector1.Z / vector2.Z,
                vector1.W / vector2.W
            );
        }

        public static Vector4 Scale(Vector4 vector, float scalar)
        {
            return new Vector4(
                vector.X / scalar,
                vector.Y / scalar,
                vector.Z / scalar,
                vector.W / scalar
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

        public static Vector4 operator *(Vector4 vector1, Vector4 vector2)
        {
            return Multiply(vector1, vector2);
        }

        public static Vector4 operator *(Vector4 vector, float scalar)
        {
            return Multiply(vector, scalar);
        }

        public static Vector4 operator /(Vector4 vector1, Vector4 vector2)
        {
            return Scale(vector1, vector2);
        }

        public static Vector4 operator /(Vector4 vector, float scalar)
        {
            return Scale(vector, scalar);
        }

        public static bool operator ==(Vector4 left, Vector4 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector4 left, Vector4 right)
        {
            return !left.Equals(right);
        }
        
        public static implicit operator Point4D(Vector4 value)
        {
            return new Point4D(value.X, value.Y, value.Z, value.W);
        }

        public static implicit operator Color(Vector4 value)
        {
            return Color.FromScRgb(value.W, value.X, value.Y, value.Z);
        }
        
        public static implicit operator Vector4(Vector3 value)
        {
            return new Vector4(value.X, value.Y, value.Z, 1.0f);
        }

        public static implicit operator Vector4(Color value)
        {
            return new Vector4(value.ScR, value.ScG, value.ScB, value.ScA);
        }

        public override string ToString()
        {
            return $"{X:F4},{Y:F4},{Z:F4},{W:F4}";
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
