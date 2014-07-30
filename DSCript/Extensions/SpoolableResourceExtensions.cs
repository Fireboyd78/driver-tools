using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSCript.Spooling
{
    public static class SpoolableResourceExtensions
    {
        public static bool ProcessSchema(this SpoolableResource<SpoolablePackage> @this, ChunkSchema schema)
        {
            return schema.Process(@this);
        }
    }
}
