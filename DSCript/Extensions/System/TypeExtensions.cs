using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class TypeExtensions
    {
        // Source: http://stackoverflow.com/a/457708/3676210
        // Original code by JaredPar
        public static bool IsSubclassOfRawGeneric(this Type @this, Type genericType)
        {
            while (@this != null && @this != typeof(object))
            {
                var curType = (@this.IsGenericType) ? @this.GetGenericTypeDefinition() : @this;
                
                if (curType == genericType)
                    return true;
                
                @this = @this.BaseType;
            }

            return false;
        }
    }
}
