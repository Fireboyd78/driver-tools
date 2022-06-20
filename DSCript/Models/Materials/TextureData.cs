using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DSCript.Models
{
    public interface ITextureData : ICopyCat<ITextureData>
    {
        int UID { get; set; }
        int Handle { get; set; }
        
        int Type { get; set; }
        int Flags { get; set; }

        int Width { get; set; }
        int Height { get; set; }

        int ExtraData { get; set; }

        byte[] Buffer { get; set; }
    }

    public sealed class TextureDataPC : ITextureData, ICopyCat<TextureDataPC>
    {
        bool ICopyCat<ITextureData>.CanCopy(CopyClassType copyType)                         => true;
        bool ICopyCat<ITextureData>.CanCopyTo(ITextureData obj, CopyClassType copyType)     => true;
        
        bool ICopyCat<ITextureData>.IsCopyOf(ITextureData obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        ITextureData ICopyClass<ITextureData>.Copy(CopyClassType copyType)
        {
            var texture = new TextureDataPC();

            CopyTo(texture, copyType);

            return texture;
        }

        void ICopyClassTo<ITextureData>.CopyTo(ITextureData obj, CopyClassType copyType)
        {
            CopyTo(obj, copyType);
        }

        bool ICopyCat<TextureDataPC>.CanCopy(CopyClassType copyType)                        => true;
        bool ICopyCat<TextureDataPC>.CanCopyTo(TextureDataPC obj, CopyClassType copyType)   => true;
        
        bool ICopyCat<TextureDataPC>.IsCopyOf(TextureDataPC obj, CopyClassType copyType)
        {
            throw new NotImplementedException();
        }

        TextureDataPC ICopyClass<TextureDataPC>.Copy(CopyClassType copyType)
        {
            var texture = new TextureDataPC();

            CopyTo(texture, copyType);

            return texture;
        }

        void ICopyClassTo<TextureDataPC>.CopyTo(TextureDataPC obj, CopyClassType copyType)
        {
            CopyTo(obj, copyType);
        }

        private void CopyTo(ITextureData obj, CopyClassType copyType)
        {
            obj.UID = UID;
            obj.Handle = Handle;
            obj.Type = Type;
            obj.Width = Width;
            obj.Height = Height;
            obj.Flags = Flags;
            obj.ExtraData = ExtraData;

            byte[] buffer = null;

            if (copyType == CopyClassType.DeepCopy)
            {
                if (Buffer != null)
                {
                    var count = Buffer.Length;

                    // copy the buffer contents                    
                    buffer = new byte[count];

                    System.Buffer.BlockCopy(Buffer, 0, buffer, 0, count);
                }
            }
            else
            {
                // reuse buffer
                buffer = Buffer;
            }

            obj.Buffer = buffer;
        }

        public int UID { get; set; }
        public int Handle { get; set; }

        public int Type { get; set; }
        
        public int Width { get; set; }
        public int Height { get; set; }
        
        public int Flags { get; set; }

        public int ExtraData { get; set; }

        public byte[] Buffer { get; set; }

        public override string ToString()
        {
            if (UID == 0x01010101)
                return $"{Handle:X8}";

            var texUid = new UID(UID, Handle);

            return texUid.ToString();
        }

        public override int GetHashCode()
        {
            return ((Handle << 1) + UID) * 95;
        }
    }
}
