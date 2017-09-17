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
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

using FreeImageAPI;

namespace DSCript.Models
{
    public interface ISubstanceData
    {
        int Flags { get; set; }

        int Mode { get; set; }
        int Type { get; set; }

        IEnumerable<ITextureData> Textures { get; }

        ITextureData GetTexture(int index);
    }

    public interface ISubstanceDataPC
    {
        bool AlphaMask { get; }

        bool Damage { get; }

        bool Specular { get; }
        bool Emissive { get; }
        bool Transparency { get; }
    }

    public abstract class SubstanceDataWrapper<TTextureData> : ISubstanceData
        where TTextureData : ITextureData
    {
        IEnumerable<ITextureData> ISubstanceData.Textures
        {
            get { return (IEnumerable<ITextureData>)Textures; }
        }

        ITextureData ISubstanceData.GetTexture(int index)
        {
            return Textures[index];
        }

        public int Flags { get; set; }

        public int Mode { get; set; }
        public int Type { get; set; }

        public List<TTextureData> Textures { get; set; }
        
        public SubstanceDataWrapper()
        {
            Textures = new List<TTextureData>();
        }
    }

    public class SubstanceDataPC : SubstanceDataWrapper<TextureDataPC>, ISubstanceDataPC
    {
        public virtual bool AlphaMask
        {
            get { return (Type == 0x400 || Type == 0x1000); }
        }

        public virtual bool Damage
        {
            get { return (Type == 0x800 || Type == 0x1000); }
        }

        public virtual bool Specular
        {
            get { return (Mode == 0x201 || Mode == 0x102); }
        }

        public virtual bool Emissive
        {
            get { return ((Flags & 0x18000) != 0 || (Flags & 0x1E) == 0x1E); }
        }

        public virtual bool Transparency
        {
            get { return (((Flags & 0x1) != 0 || Flags == 0x4) && !Specular); }
        }

        public SubstanceDataPC() : base() { }
    }
}
