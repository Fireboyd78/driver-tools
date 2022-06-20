using System;
using System.Runtime.InteropServices;

namespace DSCript.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 2, Size = 4)]
    public struct MaterialHandle : IComparable<MaterialHandle>, IEquatable<MaterialHandle>, IComparable<int>, IEquatable<int>
    {
        public ushort Handle;
        public ushort UID;

        public bool Equals(int other)
        {
            var hash = GetHashCode();

            return (hash == other);
        }

        public bool Equals(uint other)
        {
            var hash = (uint)GetHashCode();

            return (hash == other);
        }

        public bool Equals(MaterialHandle other)
        {
            return ((Handle == other.Handle) && (UID == other.UID));
        }

        public int CompareTo(int other)
        {
            var hash = GetHashCode();

            return hash.CompareTo(other);
        }

        public int CompareTo(MaterialHandle other)
        {
            if (Equals(other))
                return 0;
            
            var hash = other.GetHashCode();

            return CompareTo(hash);
        }

        public override int GetHashCode()
        {
            return (Handle | (UID << 16));
        }

        public override bool Equals(object obj)
        {
            if (obj is MaterialHandle)
                return Equals((MaterialHandle)obj);
            if (obj is int)
                return Equals((int)obj);
            if (obj is uint)
                return Equals((uint)obj);

            return base.Equals(obj);
        }

        public static bool operator ==(MaterialHandle lhs, MaterialHandle rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(MaterialHandle lhs, MaterialHandle rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator ==(MaterialHandle lhs, int rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(MaterialHandle lhs, int rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator ==(MaterialHandle lhs, uint rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(MaterialHandle lhs, uint rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static implicit operator int(MaterialHandle obj)
        {
            return (obj.Handle | (obj.UID << 16));
        }

        public static implicit operator uint(MaterialHandle obj)
        {
            return (uint)(obj.Handle | (obj.UID << 16));
        }

        public override string ToString()
        {
            return $"{Handle:X4}:{UID:X4}";
        }

        public MaterialHandle(int material)
        {
            Handle = (ushort)(material & 0xFFFF);
            UID = (ushort)((material >> 16) & 0xFFFF);
        }

        public MaterialHandle(ushort handle, ushort uid)
        {
            Handle = handle;
            UID = uid;
        }
    }
}