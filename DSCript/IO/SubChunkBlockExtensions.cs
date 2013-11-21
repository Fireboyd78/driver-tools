using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using DSCript;
using DSCript.Methods;

namespace DSCript.IO
{
    public static class ChunkBlockExtensions
    {
        public static SubChunkBlock FirstOrNull(this ChunkBlockOld i, ChunkType type)
        {
            int si = i.Subs.FindIndex((c) => c.Magic == (uint)type);

            return (si != -1) ? i.Subs[si] : null;
        }

        public static SubChunkBlock FirstOrNull(this List<ChunkBlockOld> i, ChunkType type)
        {
            for (int k = 0; k < i.Count; k++)
            {
                int si = i[k].Subs.FindIndex((c) => c.Magic == (uint)type);

                if (si != -1)
                    return i[k].Subs[si];
            }
            return null;
        }
    }
}
