using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DSCript.Models
{
    public class MaterialDataPS2
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Detail : IClassDetail<MaterialDataPS2>
        {
            public MaterialDataPS2 ToClass()
            {
                return new MaterialDataPS2() {
                    Type = (MaterialType)Type,
                    AnimationSpeed = AnimationSpeed,

                    Substances = new List<SubstanceDataPS2>(NumSubstances),
                };
            }

            public byte NumSubstances;
            public byte Type;
            
            public float AnimationSpeed;

            public int Reserved;
            
            public int SubstanceRefsOffset;
        }
        
        public MaterialType Type { get; set; }

        public float AnimationSpeed { get; set; } = 25.0f;

        public List<SubstanceDataPS2> Substances { get; set; }

        public MaterialDataPS2()
        {
            Substances = new List<SubstanceDataPS2>();
        }
    }
}
