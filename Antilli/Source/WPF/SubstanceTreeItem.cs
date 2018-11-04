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
                    textures.Add(new TextureTreeItem(count++, texture));

                return textures;
            }
        }

        public SubstanceTreeItem(int id, ISubstanceData subMaterial)
        {
            Substance = subMaterial;

            var name = (subMaterial is ISubstanceDataPC) ? subMaterial.RenderBin : "Substance";

            Name = $"[{id}]: {name}";

            if (subMaterial is ISubstanceDataPC)
            {
                var sPC = (subMaterial as ISubstanceDataPC);

                int[] regs = {
                    (subMaterial.Mode & 0xFF),
                    (subMaterial.Mode >> 8),
                    (subMaterial.Type & 0xFF),
                };

                Name = $"{Name} : 0x{subMaterial.Flags:X} {regs[0]} {regs[1]} {regs[2]} 0x{(int)sPC.ExtraFlags:X}";
            }
        }
    }
}
