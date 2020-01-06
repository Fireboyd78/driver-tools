using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;

using DSCript.Spooling;

namespace DSCript.Models
{
    public struct HierarchyInfo
    {
        public int Type;

        public int Count;   // number of parts in hierarchy
        public int UID;     // unique identifier for the hierarchy

        public int PDLSize; // used as offset past PDL for stuff like bullet hole data
    }
}
