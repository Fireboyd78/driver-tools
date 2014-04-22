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

namespace System.IO
{
    public static class FileStreamExtensions
    {
        private static readonly int FSCTL_SET_SPARSE = 590020;

        public static void MarkAsSparseFile(this FileStream @this)
        {
            var handle = @this.SafeFileHandle;

            if (handle.IsClosed || handle.IsInvalid)
                throw new Exception("INVALID HANDLE - Cannot mark as sparse file!");

            int bytesReturned = 0;

            NativeOverlapped lpOverlapped = new NativeOverlapped();
            
            bool result = NativeMethods.DeviceIoControl(@this.SafeFileHandle,
                FSCTL_SET_SPARSE,
                IntPtr.Zero,
                0,
                IntPtr.Zero,
                0,
                ref bytesReturned,
                ref lpOverlapped);

            if (!result)
                throw new Win32Exception();
        }
    }
}
