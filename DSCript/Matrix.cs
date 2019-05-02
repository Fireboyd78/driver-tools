using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Media.Media3D;

namespace DSCript
{
    public struct Matrix34
    {
        public float M11;
        public float M12;
        public float M13;
        public float M14;

        public float M21;
        public float M22;
        public float M23;
        public float M24;

        public float M31;
        public float M32;
        public float M33;
        public float M34;

        public bool Equals(Matrix44 other)
        {
            return (other.M11 == M11) && (other.M12 == M12) && (other.M13 == M13) && (other.M14 == M14)
                && (other.M21 == M21) && (other.M22 == M22) && (other.M23 == M23) && (other.M24 == M24)
                && (other.M31 == M31) && (other.M32 == M32) && (other.M33 == M33) && (other.M34 == M34);
        }

        public static bool Equals(Matrix34 left, Matrix34 right)
        {
            return left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Matrix34))
                return false;

            return Equals((Matrix34)obj);
        }

        public static bool operator ==(Matrix34 left, Matrix34 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Matrix34 left, Matrix34 right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            return M11.GetHashCode()
                ^ M12.GetHashCode()
                ^ M13.GetHashCode()
                ^ M14.GetHashCode()
                ^ M21.GetHashCode()
                ^ M22.GetHashCode()
                ^ M23.GetHashCode()
                ^ M24.GetHashCode()
                ^ M31.GetHashCode()
                ^ M32.GetHashCode()
                ^ M33.GetHashCode()
                ^ M34.GetHashCode();
        }
        
        public static implicit operator Matrix44(Matrix34 value)
        {
            return new Matrix44(value);
        }

        public Matrix34(float m11, float m12, float m13, float m14,
            float m21, float m22, float m23, float m24,
            float m31, float m32, float m33, float m34)
        {
            M11 = m11; M12 = m12; M13 = m13; M14 = m14;
            M21 = m21; M22 = m22; M23 = m23; M24 = m24;
            M31 = m31; M32 = m32; M33 = m33; M34 = m34;
        }

        public Matrix34(Matrix34 value)
            : this(value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34)
        { }
    }

    public struct Matrix44
    {
        public float M11;
        public float M12;
        public float M13;
        public float M14;

        public float M21;
        public float M22;
        public float M23;
        public float M24;

        public float M31;
        public float M32;
        public float M33;
        public float M34;

        public float M41;
        public float M42;
        public float M43;
        public float M44;

        public static readonly Matrix44 Identity = new Matrix44(
            1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 1.0f, 0.0f,
            1.0f, 0.0f, 0.0f, 1.0f
        );

        public Vector4 this[int row]
        {
            get { return GetRow(row); }
            set { SetRow(row, value); }
        }

        public Vector4 GetRow(int row)
        {
            switch (row)
            {
            case 0: return new Vector4(M11, M12, M13, M14);
            case 1: return new Vector4(M21, M22, M23, M24);
            case 2: return new Vector4(M31, M32, M33, M34);
            case 3: return new Vector4(M41, M42, M43, M44);

            default:
                throw new ArgumentOutOfRangeException(nameof(row), row, "Invalid matrix row.");
            }
        }

        public void SetRow(int row, Vector4 value)
        {
            switch (row)
            {
            case 0: M11 = value.X; M12 = value.Y; M13 = value.Z; M14 = value.W; break;
            case 1: M21 = value.X; M22 = value.Y; M23 = value.Z; M24 = value.W; break;
            case 2: M31 = value.X; M32 = value.Y; M33 = value.Z; M34 = value.W; break;
            case 3: M41 = value.X; M42 = value.Y; M43 = value.Z; M44 = value.W; break;

            default:
                throw new ArgumentOutOfRangeException(nameof(row), row, "Invalid matrix row.");
            }
        }
        
        public bool Equals(Matrix44 other)
        {
            return (other.M11 == M11) && (other.M12 == M12) && (other.M13 == M13) && (other.M14 == M14)
                && (other.M21 == M21) && (other.M22 == M22) && (other.M23 == M23) && (other.M24 == M24)
                && (other.M31 == M31) && (other.M32 == M32) && (other.M33 == M33) && (other.M34 == M34)
                && (other.M41 == M41) && (other.M42 == M42) && (other.M43 == M43) && (other.M44 == M44);
        }

        public static bool Equals(Matrix44 left, Matrix44 right)
        {
            return left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Matrix44))
                return false;

            return Equals((Matrix44)obj);
        }

        public static bool operator ==(Matrix44 left, Matrix44 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Matrix44 left, Matrix44 right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            return M11.GetHashCode()
                ^ M12.GetHashCode()
                ^ M13.GetHashCode()
                ^ M14.GetHashCode()
                ^ M21.GetHashCode()
                ^ M22.GetHashCode()
                ^ M23.GetHashCode()
                ^ M24.GetHashCode()
                ^ M31.GetHashCode()
                ^ M32.GetHashCode()
                ^ M33.GetHashCode()
                ^ M34.GetHashCode()
                ^ M41.GetHashCode()
                ^ M42.GetHashCode()
                ^ M43.GetHashCode()
                ^ M44.GetHashCode();
        }

        public static implicit operator Matrix34(Matrix44 value)
        {
            return new Matrix34(value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34);
        }

        public static implicit operator Matrix3D(Matrix44 value)
        {
            return new Matrix3D(value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34,
                value.M41, value.M42, value.M43, value.M44);
        }

        public Matrix44(float m11, float m12, float m13, float m14,
            float m21, float m22, float m23, float m24,
            float m31, float m32, float m33, float m34,
            float m41, float m42, float m43, float m44)
        {
            M11 = m11; M12 = m12; M13 = m13; M14 = m14;
            M21 = m21; M22 = m22; M23 = m23; M24 = m24;
            M31 = m31; M32 = m32; M33 = m33; M34 = m34;
            M41 = m41; M42 = m42; M43 = m43; M44 = m44;
        }

        public Matrix44(Matrix34 value)
            : this(value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34,
                0.0f, 0.0f, 0.0f, 1.0f)
        { }

        public Matrix44(Matrix44 value)
            : this(value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34,
                value.M41, value.M42, value.M43, value.M44)
        { }
    }
}
