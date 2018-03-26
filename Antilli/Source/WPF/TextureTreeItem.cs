using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DSCript.Models;

namespace Antilli
{
    public class TextureTreeItem
    {
        protected string BaseName { get; }
        
        public string Name { get; protected set; }

        public ITextureData Texture { get; }

        public void UpdateName()
        {
            Name = $"{BaseName} : {Texture.UID:X8}";
        }
        
        public TextureTreeItem(int id, ITextureData texture)
        {
            Texture = texture;
            BaseName = $"Texture {id + 1}";

            UpdateName();
        }
    }
}
