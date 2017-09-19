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

        int GetCompiledFlags(int resolved);
        int GetResolvedData();
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
            get { return ((Flags & 0x18000) != 0 || (Flags & 0x7F) == 0x1E); }
        }

        public virtual bool Transparency
        {
            get { return ((Flags & 0x400) != 0) && !Specular; }
        }

        static int[,] m_BinLookup = {
            {  0,  0 },
            {  1,  1 },
            {  2, 21 },
            {  3,  6 },
            {  4, -1 },
            {  5, -1 },
            {  6, 12 },
            {  7, 30 },
            {  8, 32 },
            {  9, 48 },
            { 10, 47 },
            { 11, 49 },
            { 12, 33 },
            { 13, 34 },
            { 14, 35 },
            { 15, 36 },
            { 16, 37 },
            { 17, 38 },
            { 18, 39 },
            { 19, 40 },
            { 20, 16 },
            { 21, 10 },
            { 22, 19 },
            { 23, 11 },
            { 24, 22 },
            { 25, 17 },
            { 26, 14 },
            { 27, 15 },
            { 28, 41 },
            { 29, 42 },
            { 30, 29 },
            { 31, 43 },
            { 32, 44 },
            { 33, -1 },
            { 34, 25 },
            { 35,  7 },
            { 36, -1 },
            { 37, 28 },
            { 38, 51 },
            { 39,  9 },
            { 40, 46 },
        };

        public int GetCompiledFlags(int resolved)
        {
            var result = 0;

            var rst     = (resolved >> 0) & 0xFF;
            var stage   = (resolved >> 8) & 0xFFFF;
            var flags   = (resolved >> 16) & 0xFFFF;

            var bin = 0;
            var alpha = 0;

            switch (rst)
            {
            case 4:
                {
                    rst = 7;
                    stage = 0;
                    flags = 0;

                    flags |= 2;
                    flags |= 0x80;
                } break;
            case 18:
            case 20:
                rst = 8;
                result |= 0x4000;
                break;
            }
            
            if (stage != 0)
            {
                var s = (stage - 3);

                if (s == (s & 1))
                    alpha = s;
            }
            
            if ((flags & 1) != 0)
                result |= 0x40000;
            if ((flags & 2) != 0)
                result |= 0x8000;
            if ((flags & 8) != 0)
                result |= 0x10000;
            if ((flags & 0x20) != 0)
                result |= 0x200;
            if ((flags & 0x40) != 0)
                result |= 0x100;
            if ((flags & 0x80) != 0)
                result |= 0x20000;

            if (alpha == 1)
                result |= 0x400;
            
            return result;
        }

        public int GetResolvedData()
        {
            var bin = (Flags & 0xFF);

            var v1 = (Mode & 0xFF);
            var v2 = 0;

            if ((v1 & 3) != 0)
            {
                var v3 = ((Mode >> 8) & 0xFF);

                if ((v3 & 3) != 0)
                    v2 = 1;
            }
            
            var rst = m_BinLookup[bin, 1];

            if (rst == -1)
            {
                rst = 8;

                if ((Flags & 0x4000) != 0)
                {
                    if (v2 == 0)
                        rst = (bin == 5) ? 20 : 18;
                }
            }

            var alpha = 0;
            var flags = 0;
            var stage = 0;

            if ((Flags & 0x100) != 0)
                flags |= 0x40;
            if ((Flags & 0x200) != 0)
                flags |= 0x20;
            if ((Flags & 0x400) != 0)
                alpha = 1;
            if ((Flags & 0x8000) != 0)
                flags |= 2;
            if ((Flags & 0x10000) != 0)
                flags |= 8;
            if ((Flags & 0x20000) != 0)
                flags |= 0x80;
            if ((Flags & 0x40000) != 0)
                flags |= 1;

            if (flags != 0)
                stage = ((alpha == 0) ? 3 : 4);

            if ((rst == 7) && ((flags & 0x80) != 0))
            {
                if ((flags & 2) != 0)
                {
                    stage = 0;
                    flags = 0;

                    rst = 4;
                }
            }

            return ((flags << 16) | (stage << 8) | rst);
        }

        public SubstanceDataPC() : base() { }
    }
}
