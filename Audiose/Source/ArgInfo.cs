using System;

namespace Audiose
{
    struct ArgInfo
    {
        public string Name;
        public string Value;

        public bool HasName
        {
            get { return !String.IsNullOrEmpty(Name); }
        }

        public bool HasValue
        {
            get { return !String.IsNullOrEmpty(Value); }
        }

        public bool IsExplicit
        {
            get { return HasValue && !HasName; }
        }

        public bool IsSwitch
        {
            get { return HasName && !HasValue; }
        }

        public bool IsVariable
        {
            get { return HasValue && HasName; }
        }

        public override string ToString()
        {
            if (HasName && HasValue)
            {
                return $"-{Name}:{Value}";
            }
            else
            {
                if (HasName)
                    return $"-{Name}";
                if (HasValue)
                    return $"{Value}";
            }

            return String.Empty;
        }

        public static implicit operator ArgInfo(string s)
        {
            return new ArgInfo(s);
        }

        public ArgInfo(string arg)
        {
            if (arg == null)
                throw new ArgumentNullException("Argument cannot be null.", nameof(arg));

            var _arg = arg.TrimStart('-');

            if (_arg != arg)
            {
                var splitIdx = _arg.IndexOf(':');

                if (splitIdx != -1)
                {
                    // set variable to value
                    Name = _arg.Substring(0, splitIdx).ToLower();
                    Value = _arg.Substring(splitIdx + 1);
                }
                else
                {
                    // option toggle
                    Name = _arg.ToLower();
                    Value = String.Empty;
                }
            }
            else
            {
                // explicit argument
                Name = String.Empty;
                Value = arg;
            }
        }
    }
}
