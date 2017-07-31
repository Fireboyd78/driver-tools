using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DSCript.Models;

namespace Antilli
{
    public class TextureTreeItem
    {
        public string Name { get; }

        public ITextureData Texture { get; }
        
        public TextureTreeItem(int id, ITextureData texture)
        {
            Texture = texture;
            Name = $"Texture {id + 1} : {texture.UID:X8}";
        }
    }
}
