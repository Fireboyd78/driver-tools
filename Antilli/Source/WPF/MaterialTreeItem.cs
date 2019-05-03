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

        protected int Id;

        public string Name
        {
            get
            {
                var name = $"[{Id}]: {s_MaterialTypes[(int)Material.Type]}";
                
                if (Material.Type == MaterialType.Animated)
                    name = $"{Name} ({Material.Substances.Count()} frames)";

                return name;
            }
        }

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
            Id = id;
            Material = material;
        }
    }
}
