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

namespace DSCript.Spooling
{
    public class ChunkReader
    {
        /// <summary>
        /// Gets the underlying file stream.
        /// </summary>
        public Stream BaseStream { get; private set; }

        public ChunkReader(Stream stream)
        {
            
        }
    }
}
