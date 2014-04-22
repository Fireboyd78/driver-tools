#if LEGACY
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Legacy;

namespace DSCript.Object
{
    public class NodeTag
    {
        public SubChunkBlock BaseChunk { get; set; }

        public NodeTag(SubChunkBlock chunk)
        {
            BaseChunk = chunk;
        }
    }
}
#endif