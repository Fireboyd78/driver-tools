using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

namespace DSCript.Spoolers
{
    /// <summary>
    /// Represents a class for spoolers with a buffer as their content.
    /// </summary>
    public class SpoolableData : Spooler
    {
        private static readonly int maxBufferSize = 1024 * 384;

        private byte[] buffer;
        private int size;

        private string TempFileName
        {
            get { return String.Format("{0}\\{1}.tmp", DSC.TempDirectory, GetHashCode()); }
        }

        private FileStream TempFile;

        /// <summary>
        /// Gets or sets the buffer.
        /// </summary>
        public byte[] Buffer
        {
            get
            {
                if (buffer == null && TempFile != null)
                {
                    var buf = new byte[size];

                    TempFile.Position = 0;
                    TempFile.Read(buf, 0, size);

                    return buf;
                }

                return buffer;
            }
            set
            {
                size = (value != null) ? value.Length : 0;

                if (size > maxBufferSize)
                {
                    if (TempFile == null)
                        TempFile = File.Create(TempFileName, size, FileOptions.DeleteOnClose);
                    else
                        TempFile.SetLength(size);

                    TempFile.Position = 0;
                    TempFile.Write(value, 0, size);

                    buffer = null;
                }
                else
                {
                    if (TempFile != null)
                        TempFile = null;

                    buffer = value;
                }
            }
        }

        public override void Dispose()
        {
            Buffer = null;

            if (TempFile != null)
                TempFile = null;
        }

        public override int Size
        {
            get { return size; }
        }

        public override void Load(Stream stream)
        {
            if (Size == 0)
                throw new Exception("Cannot load stream into uninitialized or null buffer.");

            var buffer = new byte[size];

            stream.Read(buffer, 0, size);

            Buffer = buffer;
        }

        public override void WriteTo(Stream stream)
        {
            if (Buffer != null)
                stream.Write(Buffer);
        }

        protected SpoolableData() : base() { }

        public SpoolableData(int magic, byte reserved) : base(magic, reserved) { }

        public SpoolableData(int magic, byte reserved, int size) : base(magic, reserved)
        {
            this.size = size;
        }

        ~SpoolableData()
        {
            Dispose();
        }
    }
}
