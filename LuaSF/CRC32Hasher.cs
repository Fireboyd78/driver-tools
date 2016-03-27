using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LuaSF
{
    static class CRC32Hasher
    {
        private static long[] hashTable;

        private static void GenerateTable()
        {
            hashTable = new long[256];

            for (int i = 0, j = 0; i < hashTable.Length; i++, j++)
            {
                var key = j ^ 255L;

                for (int k = 0; k < 8; k++)
                    key = (key >> 1) ^ (-((key & 255) & 1) != 0 ? 0xEDB88320L : 0);

                hashTable[i] = key ^ 0xFF000000L;
            }
        }

        public static int GetHash(string filename)
        {
            // don't generate table until we actually request something
            if (hashTable == null)
                GenerateTable();

            var hash = 0xFFFFFFFFL;

            for (int k = 0; k < filename.Length; k++)
                hash = (hash >> 8) ^ hashTable[filename[k] ^ (hash & 255)];

            return (int)hash;
        }
    }
}
