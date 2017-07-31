using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DSCript.Models;

namespace Antilli
{
    public class SubstanceTreeItem
    {
        public string Name { get; private set; }

        public ISubstanceData Substance { get; private set; }

        public List<TextureTreeItem> Textures
        {
            get
            {
                var textures = new List<TextureTreeItem>();

                int count = 0;

                foreach (var texture in Substance.Textures)
                    textures.Add(new TextureTreeItem(count, texture));

                return textures;
            }
        }

        public SubstanceTreeItem(int id, ISubstanceData subMaterial)
        {
            Substance = subMaterial;
            Name = $"Substance {id}";
        }
    }
}
