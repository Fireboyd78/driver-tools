using System.Diagnostics;
using System.IO;

namespace DSCript.Models
{
    public struct TextureFormat
    {
        public int Type;

        public int Flags;

        public int MipMaps;
        public int Dimensions;

        public int USize;
        public int VSize;
        public int PSize;

        public static TextureFormat Unpack(int format)
        {
            var result = new TextureFormat();

            result.FromInt32(format);

            return result;
        }

        public void FromInt32(int format)
        {
            // based on a packed D3DFORMAT from internal O.G. XBox stuff..
            // Reflections used this for both XBox/PC, so may as well do the same here ;)
            //
            // interesting how Reflections synchronized XBox/PC together starting with DPL,
            // even if that meant PC has completely redundant XBox information...lol

            /*
            #define D3DFORMAT_RESERVED1_MASK        0x00000003      // Must be zero

            #define D3DFORMAT_DMACHANNEL_MASK       0x00000003
            #define D3DFORMAT_DMACHANNEL_A          0x00000001      // DMA channel A - the default for all system memory
            #define D3DFORMAT_DMACHANNEL_B          0x00000002      // DMA channel B - unused
            #define D3DFORMAT_CUBEMAP               0x00000004      // Set if the texture if a cube map
            #define D3DFORMAT_BORDERSOURCE_COLOR    0x00000008
            #define D3DFORMAT_DIMENSION_MASK        0x000000F0      // # of dimensions
            #define D3DFORMAT_DIMENSION_SHIFT       4
            #define D3DFORMAT_FORMAT_MASK           0x0000FF00
            #define D3DFORMAT_FORMAT_SHIFT          8
            #define D3DFORMAT_MIPMAP_MASK           0x000F0000
            #define D3DFORMAT_MIPMAP_SHIFT          16
            #define D3DFORMAT_USIZE_MASK            0x00F00000      // Log 2 of the U size of the base texture
            #define D3DFORMAT_USIZE_SHIFT           20
            #define D3DFORMAT_VSIZE_MASK            0x0F000000      // Log 2 of the V size of the base texture
            #define D3DFORMAT_VSIZE_SHIFT           24
            #define D3DFORMAT_PSIZE_MASK            0xF0000000      // Log 2 of the P size of the base texture
            #define D3DFORMAT_PSIZE_SHIFT           28
            */

            // grab everything except the DMA channel (always 1)
            PSize = (1 << ((format >> 28) & 0xF));
            VSize = (1 << ((format >> 24) & 0xF));
            USize = (1 << ((format >> 20) & 0xF));
            MipMaps = (format >> 16) & 0xF;
            Type = (format >> 8) & 0xFF;
            Dimensions = (format >> 4) & 0xF;
            Flags = (format & 0xC);
        }

        public int ToInt32(int version)
        {
            var format = 0;

            if (version != 6)
            {
                format |= (GetNumBits(PSize) & 0xF) << 28;
                format |= (GetNumBits(VSize) & 0xF) << 24;
                format |= (GetNumBits(USize) & 0xF) << 20;
                format |= (MipMaps & 0xF) << 16;
                format |= (Type & 0xFF) << 8;
                format |= (Flags & 0xC);
                format |= 1; // DMA channel
            }

            return format;
        }

        int GetNumBits(int value)
        {
            int bits = 0;

            for (bits = 0; value > 1; bits++)
                value >>= 1;

            return (value == 1) ? bits : 0;
        }
    }

    public struct TextureInfo : IDetail
    {
        public const int FLAGS_DRIV3R_PC = 32768; // lower 8 bits not set, so converted version is zero ;)

        public int UID;
        public int Handle;

        public int Flags;

        public int DataOffset;
        public int DataSize;

        public int Type;

        public int Width;
        public int Height;

        public TextureFormat Format;
        public bool UsesFormat;

        public int Reserved;
        
        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            UID = stream.ReadInt32();
            Handle = stream.ReadInt32();

            Flags = FLAGS_DRIV3R_PC;
            UsesFormat = false;

            if (provider.Platform == PlatformType.Wii)
            {
                Flags = stream.ReadInt32() & 0xFF;
                DataOffset = stream.ReadInt32();

                for (int i = 0; i < 8; i++)
                {
                    var check = stream.ReadInt32();

                    if (check != -1)
                        throw new InvalidDataException($"Wii texture info contains unknown data ({i}: {check:X8})");
                }
            }
            else
            {
                if (provider.Version != 6)
                {
                    Flags = stream.ReadInt32() & 0xFF;
                    stream.Position += 4; // (1,4)
                }

                DataOffset = stream.ReadInt32();
                DataSize = stream.ReadInt32();

                if (Flags == FLAGS_DRIV3R_PC)
                {
                    Type = stream.ReadInt32();

                    Width = stream.ReadInt16();
                    Height = stream.ReadInt16();   
                }

                var format = stream.ReadInt32();

                if (provider.Version != 6)
                {
                    Format = TextureFormat.Unpack(format);

                    Type = Format.Type;
                    Width = Format.USize;
                    Height = Format.VSize;

                    UsesFormat = true;
                }

                Reserved = stream.ReadInt32();
            }
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(UID);
            stream.Write(Handle);

            if (provider.Platform == PlatformType.Wii)
            {
                stream.Write(0xF00 | (Flags & 0xFF));
                stream.Write(DataOffset);

                for (int i = 0; i < 8; i++)
                    stream.Write(-1);
            }
            else
            {
                if (provider.Version != 6)
                {
                    stream.Write(0xF00 | (Flags & 0xFF));

                    stream.Write((short)1);
                    stream.Write((short)4);
                }

                stream.Write(DataOffset);
                stream.Write(DataSize); // unlike Reflections, we don't write zero ;)

                if (provider.Version != 6)
                {
                    var type = Type;

                    if (!UsesFormat && Flags == FLAGS_DRIV3R_PC)
                    {
                        switch (Type)
                        {
                        case 0:
                            // bump map?
                            break;
                        case 1:
                            type = (int)D3DFormat.DXT1;
                            break;
                        case 3:
                            type = (int)D3DFormat.DXT3;
                            break;
                        case 5:
                            type = (int)D3DFormat.DXT5;
                            break;
                        case 128:
                            type = (int)D3DFormat.A8R8G8B8;
                            break;
                        }
                    }

                    Format.Type = type;
                    Format.USize = Width;
                    Format.VSize = Height;
                }
                else
                {
                    var type = Type;

                    if (UsesFormat && Flags != FLAGS_DRIV3R_PC)
                    {
                        // convert to Driv3r PC format
                        switch ((D3DFormat)Type)
                        {
                        case D3DFormat.DXT1:
                            type = 1;
                            break;
                        case D3DFormat.DXT3:
                            type = 3;
                            break;
                        case D3DFormat.DXT5:
                            type = 5;
                            break;
                        case D3DFormat.A8R8G8B8:
                            type = 128;
                            break;
                        }
                    }

                    // DRIV3R PC: format is unused, write these out fully
                    stream.Write(type);
                    stream.Write((short)Width);
                    stream.Write((short)Height);
                }

                var format = Format.ToInt32(provider.Version);
                
                stream.Write(format);
                stream.Write(Reserved);
            }
        }
    }
}
