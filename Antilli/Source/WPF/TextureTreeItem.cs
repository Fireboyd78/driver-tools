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

        public IMaterialPackage Owner { get; set; }

        public void UpdateName()
        {
            var texUid = new UID(Texture.UID, Texture.Handle);
            var texName = texUid.ToString();

            if (Texture.UID == 0x01010101)
                texName = $"{Texture.Handle:X8}";

            Name = $"{BaseName} : {texName} : {Texture.Width}x{Texture.Height}";
        }
        
        public TextureTreeItem(int id, ITextureData texture)
        {
            Texture = texture;
            BaseName = $"[{id + 1}]: Texture";

            UpdateName();
        }
    }
}
