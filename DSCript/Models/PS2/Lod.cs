using System.Collections.Generic;

namespace DSCript.Models
{
    public class LodPS2
    {
        // holds no actual data
        public bool IsDummy { get; set; }

        public int Mask { get; set; }
        public int NumTriangles { get; set; }

        public Vector4 Scale { get; set; }

        public List<LodInstancePS2> Instances { get; set; }
    }
}
