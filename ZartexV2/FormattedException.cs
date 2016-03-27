using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zartex
{
    public sealed class FormattedException : Exception
    {
        public FormattedException()
            : base()
        {

        }

        public FormattedException(string messageFormat, params object[] messageArgs)
            : base(String.Format(messageFormat, messageArgs))
        {

        }

        public FormattedException(Exception innerException, string messageFormat, params object[] messageArgs)
            : base(String.Format(messageFormat, messageArgs), innerException)
        {

        }
    }
}
