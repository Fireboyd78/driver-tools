using System;

namespace DSCript
{
    public enum CopyClassType
    {
        /// <summary>
        /// Copy the top-level of the class, reusing any class/struct members as-is.
        /// </summary>
        SoftCopy,

        /// <summary>
        /// Deeply copy the class, nesting into any class/struct members.
        /// </summary>
        DeepCopy,
    }

    /// <summary>
    /// Interface for creating a new copy instance of a class.
    /// </summary>
    /// <typeparam name="T">The type of class a copy will be created of.</typeparam>
    public interface ICopyClass<T>
        where T : class
    {
        /// <summary>
        /// Create a copy of the class using the specified copy type.
        /// </summary>
        /// <param name="copyType">The type of copy to perform.</param>
        /// <returns>A copy of the class.</returns>
        T Copy(CopyClassType copyType = CopyClassType.SoftCopy);
    }

    /// <summary>
    /// Interface for copying to an existing instance of a class.
    /// </summary>
    /// <typeparam name="T">The type of class to copy to.</typeparam>
    public interface ICopyClassTo<T>
        where T : class
    {
        /// <summary>
        /// Copies the class to another instance using the specified copy type.
        /// </summary>
        /// <param name="obj">The object to copy data to.</param>
        /// <param name="copyType">The type of copy to perform.</param>
        void CopyTo(T obj, CopyClassType copyType = CopyClassType.SoftCopy);
    }

    /// <summary>
    /// Interface for creating new/copying to instances of a class.
    /// </summary>
    /// <typeparam name="T">The type of class that will be newly created and/or copied to.</typeparam>
    public interface ICopyCat<T> : ICopyClass<T>, ICopyClassTo<T>
        where T : class
    {
        /// <summary>
        /// Validates data can be copied from this class.
        /// </summary>
        /// <param name="copyType">The type of copy to check if can be performed.</param>
        /// <returns>True if the class can be copied; otherwise, False.</returns>
        bool CanCopy(CopyClassType copyType = CopyClassType.SoftCopy);
        
        /// <summary>
        /// Validates data can be copied to the class instance.
        /// </summary>
        /// <param name="obj">The object to check if can be copied.</param>
        /// <param name="copyType">The type of copy to check if can be performed.</param>
        /// <returns>True if the class can copy to the instance; otherwise, False.</returns>
        bool CanCopyTo(T obj, CopyClassType copyType = CopyClassType.SoftCopy);

        /// <summary>
        /// Checks if the specified class instance is already a copy of the specified copy type of the class.
        /// </summary>
        /// <param name="obj">The object to check if is a copy of the specified copy type.</param>
        /// <param name="copyType">The type of copy to check the object with.</param>
        /// <returns>True if the instance is a copy of the class of the specified copy type; otherwise, False.</returns>
        bool IsCopyOf(T obj, CopyClassType copyType = CopyClassType.SoftCopy);
    }
}
