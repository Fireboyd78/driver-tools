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
        
        public string Name { get; }

        public IMaterialData Material { get; }

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

            var name = s_MaterialTypes[(int)Material.Type];

            if (Material.Type == MaterialType.Animated)
                name += $" ({Material.Substances.Count()} frames)";

            Name = $"[{id}]: {name}";
}
    }
}
