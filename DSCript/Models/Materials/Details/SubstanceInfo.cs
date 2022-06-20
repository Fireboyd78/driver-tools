using System.IO;

namespace DSCript.Models
{
    public struct SubstanceInfo : IDetail
    {
        public byte Bin;

        public int Flags;

        public byte TS1;
        public byte TS2;
        public byte TS3;

        public int TextureFlags;

        public int TextureRefsOffset;
        public int TextureRefsCount;

        public int PaletteRefsOffset;
        public int PaletteRefsCount;

        public int Reserved;
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            var sInf = 0;
            var sFlg = 0;

            if (provider.Version == 6)
            {
                sInf = stream.ReadInt32();
                sFlg = stream.ReadInt32();

                stream.Position += 8;
            }
            else
            {
                sFlg = stream.ReadInt32();
                sInf = stream.ReadInt32();
            }

            Bin = (byte)(sInf & 0xFF);
            Flags = ((sInf >> 8) & 0xFFFFFF);

            TS1 = (byte)((sFlg >> 0) & 0xFF);
            TS2 = (byte)((sFlg >> 8) & 0xFF);
            TS3 = (byte)((sFlg >> 16) & 0xFF);

            TextureFlags = ((sFlg >> 24) & 0xFF);

            TextureRefsOffset = stream.ReadInt32();
            TextureRefsCount = stream.ReadInt32();

            if (provider.Version != 6)
            {
                if ((TextureFlags & (int)(SubstanceExtraFlags.Damage | SubstanceExtraFlags.ColorMask)) != 0)
                {
                    // ughhhh
                    TextureFlags |= (int)SubstanceExtraFlags.DPL_SwapDamageAndColorMaskBits;
                }

                PaletteRefsOffset = stream.ReadInt32();
                PaletteRefsCount = stream.ReadInt32();
                
                Reserved = stream.ReadInt32(); // = 0x8000000
            }
            else
            {
                // TODO: read 'Reserved' properly? 
                stream.Position += 8;
            }
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            var sInf = ((Bin << 0) | ((Flags & 0xFFFFFF) << 8));
            var sFlg = ((TS1 << 0) | (TS2 << 8) | (TS3 << 16) | ((TextureFlags & 0xFF) << 24));

            if (provider.Version == 6)
            {
                stream.Write(sInf);
                stream.Write(sFlg);

                stream.Write(0L);
            }
            else
            {
                stream.Write(sFlg);
                stream.Write(sInf);
            }

            stream.Write(TextureRefsOffset);
            stream.Write(TextureRefsCount);

            if (provider.Version != 6)
            {
                stream.Write(PaletteRefsOffset);
                stream.Write(PaletteRefsCount);

                stream.Write(Reserved);
            }
            else
            {
                // TODO: write 'Reserved' properly? 
                stream.Write(0L);
            }
        }

        static int[,] m_BinEffectLookup = {
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

        public static void Resolve(ref SubstanceInfo substance)
        {
            var ts1 = substance.TS1;
            var specular = 0;

            // 1 | 2
            if ((ts1 & 3) != 0)
            {
                var ts2 = substance.TS2;

                // 1 | 2
                if ((ts2 & 3) != 0)
                    specular = 1;
            }

            var bin = substance.Bin;
            var effect = -1;

            if (bin < (m_BinEffectLookup.Length / 2))
                effect = m_BinEffectLookup[bin, 1];

            var flags = substance.Flags;

            if (effect == -1)
            {
                effect = 8;

                if ((flags & 0x40) != 0)
                {
                    if (specular == 0)
                        effect = (bin == (int)RenderBinType.Clutter) ? 20 : 18;
                }
            }

            var flg1 = 0;
            var flg2 = 0;
            
            if ((flags & 0x1) != 0)
                flg2 |= 0x40;
            if ((flags & 0x2) != 0)
                flg2 |= 0x20;
            if ((flags & 0x4) != 0)
                flg1 = 1;
            if ((flags & 0x80) != 0)
                flg2 |= 2;
            if ((flags & 0x100) != 0)
                flg2 |= 8;
            if ((flags & 0x200) != 0)
                flg2 |= 0x80;
            if ((flags & 0x400) != 0)
                flg2 |= 1;

            if (flg2 != 0)
                flg2 = ((flg1 == 0) ? 3 : 4);

            // shiny road?
            if ((effect == 7) && ((flg2 & 0x80) != 0))
            {
                if ((flg2 & 2) != 0)
                {
                    flg2 = 0;
                    flg1 = 0;

                    effect = 4;
                }
            }

            // flg3 = 0
            substance.Bin = (byte)effect;
            substance.Flags = ((flg2 << 8) | flg1);
        }

        public static bool Compile(ref SubstanceInfo substance)
        {
            var flags = (SubstanceExtraFlags)substance.TextureFlags;

            var bump_map = 0;

            if (flags.HasFlag(SubstanceExtraFlags.BumpMap))
                bump_map = 1;

            var effect = substance.Bin;

            var ts1 = substance.TS1;
            var ts2 = substance.TS2;
            var ts3 = substance.TS3;

            var flg1 = (substance.Flags & 0xFF);
            var flg2 = ((substance.Flags >> 8) & 0xFF);
            var flg3 = ts3;

            if (flags.HasFlag(SubstanceExtraFlags.FLAG_1))
            {
                ts3 = 2;
            }
            else
            {
                if ((ts1 & 3) != 0)
                {
                    if ((ts2 & 3) != 0)
                    {
                        if (effect == 9)
                        {
                            substance.TextureFlags = (int)SubstanceExtraFlags.FLAG_1;
                            substance.TS1 = 129;
                            substance.TS2 = 0;
                            substance.TS3 = 0;
                            substance.Bin = 13;
                            substance.Flags = ((flg3 << 16) | (flg2 << 8) | flg1);

                            return true;
                        }

                        switch (flg1)
                        {
                        case 0:
                            flg1 = 2;
                            break;
                        case 1:
                            flg2 |= 0x10;
                            flg1 = 5;
                            break;
                        case 3:
                            flg1 = 5;
                            break;
                        }
                        
                        switch (ts2)
                        {
                        case 0:
                            flags = SubstanceExtraFlags.ColorMask;
                            ts1 = 2;
                            ts2 = 1;
                            break;
                        case 1:
                            flags = (SubstanceExtraFlags.FLAG_1 | SubstanceExtraFlags.BumpMap);
                            ts1 = 2;
                            ts2 = 2;
                            ts3 = 0;
                            break;
                        case 2:
                            flags = SubstanceExtraFlags.ColorMask;
                            ts1 = 2;
                            ts2 = 1;
                            ts3 = 0;
                            break;
                        }
                    }
                }
                else
                {
                    switch (ts2)
                    {
                    case 1:
                        flags = (SubstanceExtraFlags.BumpMap | SubstanceExtraFlags.ColorMask);
                        ts1 = 1;
                        ts2 = 2;
                        ts3 = 0;
                        break;
                    case 2:
                        flags = SubstanceExtraFlags.ColorMask;
                        ts1 = 1;
                        ts2 = 1;
                        ts3 = 0;
                        break;
                    default:
                        if (bump_map == 1)
                        {
                            if (ts2 == 0)
                            {
                                flags = SubstanceExtraFlags.FLAG_1;
                                ts1 = 1;
                                ts2 = 0;
                                ts3 = 0;
                            }
                        }
                        else
                        {
                            flags = SubstanceExtraFlags.FLAG_1;
                            ts1 = 129;
                            ts2 = 0;
                            ts3 = 0;
                        }
                        break;
                    }
                }

                if (bump_map == 1)
                {
                    flags = SubstanceExtraFlags.BumpMap;
                    ts3 = 1;
                }
            }
            
            if (effect == 16)
            {
                if (flg1 == 2)
                    flg1 = ((flg2 == 0x10) ? 1 : 0);

                flg2 &= 0xFB;
                ts3 = 4;
            }

            substance.TextureFlags = (byte)flags;
            substance.TS1 = ts1;
            substance.TS2 = ts2;
            substance.TS3 = ts3;
            substance.Flags = ((flg3 << 16) | (flg2 << 8) | flg1);

            return true;

        }
    }
}
