using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript
{
    public static class CopyCatFactory
    {
        public static bool IsCopyOfB<T>(T objA, T objB, CopyClassType copyType = CopyClassType.SoftCopy)
            where T : class, ICopyCat<T>
        {
            // references are NOT copies, duh!
            if (Object.ReferenceEquals(objA, objB))
                throw new Exception("Cannot check if an object is a copy of itself!");

            return objA.IsCopyOf(objB, copyType);
        }

        public static bool CopyToB<T>(T objA, T objB, CopyClassType copyType = CopyClassType.SoftCopy)
            where T : class, ICopyCat<T>
        {
            if (Object.ReferenceEquals(objA, objB))
                throw new Exception("Cannot copy an object into itself!");

            if (objA.CanCopyTo(objB, copyType))
            {
                objA.CopyTo(objB, copyType);
                return true;
            }

            return false;
        }

        public static T GetCopy<T>(T obj, CopyClassType copyType = CopyClassType.SoftCopy)
            where T : class, ICopyCat<T>
        {
            if (!obj.CanCopy(copyType))
                throw new Exception("Cannot safely create a copy of the object.");

            return obj.Copy(copyType);
        }

        public static bool TryCopy<T>(T obj, CopyClassType copyType, out T copy)
            where T : class, ICopyCat<T>
        {
            if (obj.CanCopy(copyType))
            {
                copy = obj.Copy(copyType);
                return true;
            }

            copy = null;
            return false;
        }
    }
}
