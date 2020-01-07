using System;
using System.Collections.Generic;

namespace DSCript.Parser
{
    public static class Tokenizer
    {
        public static readonly char CommentLineKey = '#';

        public static bool IsCommentLine(char value)
        {
            return (value == CommentLineKey);
        }

        public static bool IsCommentLine(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Argument cannot be null.");

            return IsCommentLine(value[0]);
        }
        
        public static string[] SplitTokens(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str), "Argument cannot be null.");
            
            if (str.Length < 1)
                return new[] { str };

            var values = new List<String>(32);

            var start = 0;
            var length = 0;

            var stringOpen = false;
            var stringEscaped = false;

            var commentOpen = false;

            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];
                var flags = CharUtils.GetCharFlags(c);

                // break on null
                if ((flags & CharacterTypeFlags.Null) != 0)
                    break;

                if ((flags & CharacterTypeFlags.TabOrWhitespace) != 0)
                {
                    // process tabs/whitespace outside of strings/comments
                    if (!stringOpen)
                    {
                        if (!commentOpen)
                        {
                            if (length > 0)
                                values.Add(str.Substring(start, length));

                            start = (i + 1); // "ABC|  DEF" -> "ABC | DEF" -> "ABC |DEF"
                            length = 0;
                        }
                        continue;
                    }
                }

                // check for inline comments
                if (IsCommentLine(c))
                {
                    commentOpen = true;
                    continue;
                }

                if (commentOpen)
                    continue;
                
                if ((flags & CharacterTypeFlags.ExtendedOperators) != 0)
                {
                    if (!stringOpen)
                    {
                        if (length > 0)
                            values.Add(str.Substring(start, length));

                        values.Add(c.ToString());

                        start = (i + 1);
                        length = 0;

                        continue;
                    }
                }

                // increase string length
                ++length;

                if ((flags & CharacterTypeFlags.Quote) != 0)
                {
                    if (stringOpen)
                    {
                        if (stringEscaped)
                        {
                            stringEscaped = false;
                        }
                        else
                        {
                            // complete the string (include last quote)
                            if (length > 0)
                                values.Add(str.Substring(start, length + 1));

                            start = (i + 1); // "ABC|" -> "ABC"|
                            length = 0;

                            stringOpen = false;
                        }
                    }
                    else
                    {
                        start = i; // |"ABC"
                        length = 0;

                        stringOpen = true;
                    }
                }
                else if (stringEscaped)
                {
                    // not an escape sequence
                    stringEscaped = false;
                    continue;
                }

                if (stringOpen && (c == '\\'))
                {
                    stringEscaped = true;
                    continue;
                }
            }

            // final add
            if (length > 0 && !commentOpen)
                values.Add(str.Substring(start, length));

            return values.ToArray();
        }

        public static string StripQuotes(string str)
        {
            var start = 0;
            var length = 0;

            var isOpen = false;
            var isEscaped = false;

            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i];
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
                            // all done!
                            return str.Substring(start, length);
                        }
                    }
                }
                length++;
            }

            return str;
        }
    }
}
