using System;
using System.Reflection;

namespace Antilli
{
    public static class TypeExtensions
    {
        public static object GetValue(this PropertyInfo @this, object obj, BindingFlags flags)
        {
            return @this.GetValue(obj, flags, null, null, null);
        }
    }
}
