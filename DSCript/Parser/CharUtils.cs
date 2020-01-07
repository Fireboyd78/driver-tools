using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Parser
{
    [Flags]
    public enum CharacterTypeFlags : int
    {
        Null = (1 << 0),

        NewLine = (1 << 1),

        Tab = (1 << 2),
        Whitespace = (1 << 3),

        TabOrWhitespace = (Tab | Whitespace),

        Digit = (1 << 4),

        Lowercase = (1 << 5),
        Uppercase = (1 << 6),

        Quote = (1 << 7),

        Letter = (Lowercase | Uppercase),
        Alphanumerical = (Letter | Digit),

        EndOfToken = (Null | NewLine | TabOrWhitespace),

        Control = (1 << 8), // 0x1 - 0x1F
        Operator = (1 << 9),
        Separator = (1 << 10),

        OpenBrace = (1 << 11),
        CloseBrace = (1 << 12),

        Brace = (OpenBrace | CloseBrace),

        ExtendedOperators = (Operator | Separator | Brace),

        Unknown = (1 << 15),
    }

    public static class CharUtils
    {
        // fast lookup
        private static bool IsLookupReady = false;
        private static int[] LookupTypes = new int[128];

        private static void MapLookupTypes()
        {
            // map once
            if (IsLookupReady)
                return;

            for (int i = 0; i < LookupTypes.Length; i++)
            {
                int type = 0;

                if (i >= 127)
                    type |= (int)CharacterTypeFlags.Unknown;

                if (i >= 'a' && i <= 'z')
                    type |= (int)CharacterTypeFlags.Lowercase;
                if (i >= 'A' && i <= 'Z')
                    type |= (int)CharacterTypeFlags.Uppercase;

                if (i >= '0' && i <= '9')
                    type |= (int)CharacterTypeFlags.Digit;

                if (i >= 1 && i <= 31)
                    type |= (int)CharacterTypeFlags.Control;

                if (i == ' ')
                    type |= (int)CharacterTypeFlags.Whitespace;
                if (i == '\t')
                    type |= (int)CharacterTypeFlags.Tab;

                if (i == '\0')
                    type |= (int)CharacterTypeFlags.Null;

                switch (i)
                {
                case '"':
                    type |= (int)CharacterTypeFlags.Quote;
                    break;

                case '\r': case '\n':
                    type |= (int)CharacterTypeFlags.NewLine;
                    break;

                case '@':
                    type |= (int)CharacterTypeFlags.Operator;
                    break;

                case ',': case ':':
                    type |= (int)CharacterTypeFlags.Separator;
                    break;

                case '(': case '{': case '[':
                    type |= (int)CharacterTypeFlags.OpenBrace;
                    break;

                case ')': case '}': case ']':
                    type |= (int)CharacterTypeFlags.CloseBrace;
                    break;
                }
                
                LookupTypes[i] = type;
            }
            IsLookupReady = true;
        }

        public static CharacterTypeFlags GetCharFlags(int value)
        {
            MapLookupTypes();
            return ((value >= 0) && (value <= 127)) ? (CharacterTypeFlags)LookupTypes[value] : CharacterTypeFlags.Unknown;
        }

        public static bool HasCharFlags(int value, CharacterTypeFlags charFlags)
        {
            return ((GetCharFlags(value) & charFlags) != 0);
        }
    }
}
