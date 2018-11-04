using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Antilli.Parser
{
    internal static class StringExtensions
    {
        public static string StripQuotes(this string @this, bool wrap = false)
        {
            var start = 0;
            var length = 0;

            var isOpen = false;
            var isEscaped = false;

            for (int i = 0; i < @this.Length; i++)
            {
                var c = @this[i];
                var flags = CharUtils.GetCharFlags(c);

                if (c == '\\')
                    isEscaped = true;

                if ((flags & CharacterTypeFlags.Quote) != 0)
                {
                    if (!isOpen)
                    {
                        if (!isEscaped)
                        {
                            isOpen = true;
                            start = (i + 1);
                            continue;
                        }
                    }
                    else
                    {
                        if (isEscaped)
                        {
                            isEscaped = false;
                        }
                        else
                        {
                            var result = @this.Substring(start, length);

                            // wrap in '<value>' ?
                            if (wrap)
                                result = $"'{result}'";

                            return result;
                        }
                    }
                }
                length++;
            }

            return @this;
        }
            
        
    }
}
