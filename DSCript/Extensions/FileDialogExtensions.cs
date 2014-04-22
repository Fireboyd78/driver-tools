using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32;

namespace DSCript
{
    public static class FileDialogExtensions
    {
        public static void AddFilter(this FileDialog @this, string filter)
        {
            if (!String.IsNullOrEmpty(filter))
            {
                var _filter = @this.Filter;

                @this.Filter = (!String.IsNullOrEmpty(_filter)) ? String.Join("|", _filter, filter) : filter;
            }
        }
    }
}
