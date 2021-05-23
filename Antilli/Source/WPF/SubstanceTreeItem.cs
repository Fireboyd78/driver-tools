using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DSCript.Models;

namespace Antilli
{
    public class SubstanceTreeItem
    {
        protected int Id;

        public string Name
        {
            get
            {
                var name = "Substance";

                name = $"[{Id}]: {name}";

                /*
                if (Substance is ISubstanceDataPC)
                {
                    var sPC = (Substance as ISubstanceDataPC);

                    int[] regs = {
                        (Substance.Mode & 0xFF),
                        (Substance.Mode >> 8),
                        (Substance.Type & 0xFF),
                    };

                    name = $"{name} : 0x{Substance.Flags:X} {regs[0]} {regs[1]} {regs[2]} 0x{(int)sPC.ExtraFlags:X}";
                }
                */

                return name;
            }
        }

        public ISubstanceData Substance { get; }

        public IMaterialPackage Owner { get; set; }

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
            Id = id;
            Substance = subMaterial;
        }
    }
}
