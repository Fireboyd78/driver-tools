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

namespace DSCript
{
    public sealed class TempFilePool
    {
        public static readonly byte Version = 1;

        private static int nPools = 0;

        internal static readonly List<TempFilePool> Pools = new List<TempFilePool>();
        internal FileStream Stream { get; private set; }

        public static byte[] GetPooledData(uint id)
        {
            var poolId = (int)(id / 255);

            if ((poolId + 1) > nPools)
                throw new Exception("Cannot get pooled data - file pool is not initialized.");

            var stream = Pools[poolId].Stream;

            stream.Position = 8 + (id * 8);

            int offset = stream.ReadInt32();
            int size = stream.ReadInt32();

            stream.Position = offset + 0x800;

            byte[] buffer = new byte[size];

            stream.Read(buffer, 0, buffer.Length);

            return buffer;
        }

        private void InitPool()
        {
            Stream = new FileStream(Path.Combine(DSC.TempDirectory, String.Format("{0}.tmp", nPools)), FileMode.Create);

            Stream.Write(((Version << 24) | 0x7C95FB));
            Stream.Write(0x3E3E3E3E);
            Stream.Write(Enumerable.Repeat((byte)0xFF, 0x7F8).ToArray());
        }

        private TempFilePool()
        {
            Pools.Add(this);
            ++nPools;

            InitPool();
        }
    }
}
