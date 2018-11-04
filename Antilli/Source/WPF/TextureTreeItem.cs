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
            Name = $"{BaseName} : {Texture.UID:X8} : {Texture.Width}x{Texture.Height}";
        }
        
        public TextureTreeItem(int id, ITextureData texture)
        {
            Texture = texture;
            BaseName = $"[{id + 1}]: Texture";

            UpdateName();
        }
    }
}
