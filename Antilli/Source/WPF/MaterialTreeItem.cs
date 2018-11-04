using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DSCript.Models;

namespace Antilli
{
    public class MaterialTreeItem
    {
        static readonly string[] s_MaterialTypes = {
            "Material",
            "Animation",
        };

        public string Name { get; private set; }

        public IMaterialData Material { get; private set; }

        public List<SubstanceTreeItem> Substances
        {
            get
            {
                var substances = new List<SubstanceTreeItem>();

                int count = 0;

                foreach (var substance in Material.Substances)
                    substances.Add(new SubstanceTreeItem(++count, substance));

                return substances;
            }
        }

        public MaterialTreeItem(int id, MaterialDataPC material)
        {
            Material = material;
            
            Name = $"[{id}]: {s_MaterialTypes[(int)material.Type]}";

            if (material.Type == MaterialType.Animated)
                Name = $"{Name} ({material.Substances.Count} frames, {material.AnimationSpeed:F2} fps)";
        }
    }
}
