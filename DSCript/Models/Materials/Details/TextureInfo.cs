﻿using System.IO;

namespace DSCript.Models
{
    public struct TextureInfo : IDetail
    {
        public int UID;
        public int Handle;

        public int DataOffset;
        public int DataSize;

        public int Type;

        public short Width;
        public short Height;

        public int Flags;

        public int Reserved;
        
        int GetPackedBits(int value)
        {
            int bits = 0;

            for (bits = 0; value > 1; bits++)
                value >>= 1;
            
            return (value == 1) ? bits : 0;
        }

        void IDetail.Deserialize(Stream stream, IDetailProvider provider)
        {
            UID = stream.ReadInt32();
            Handle = stream.ReadInt32();

            if (provider.Platform == PlatformType.Wii)
            {
                Type = stream.ReadInt32() & 0xFF;
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
                    Type = stream.ReadInt32() & 0xFF;
                    stream.Position += 4; // (1,4)
                }

                DataOffset = stream.ReadInt32();
                DataSize = stream.ReadInt32();

                if (provider.Version != 6)
                {
                    var format = stream.ReadInt32();

                    // packed data -- very clever!
                    Width = (short)(1 << ((format >> 20) & 0xF));
                    Height = (short)(1 << ((format >> 24) & 0xF));

                    // not 100% sure on this one
                    //Type = (format >> 16) & 0xF;

                    // packed D3DFORMAT
                    Flags = (format & 0xFFFFF);

                    Reserved = stream.ReadInt32();
                }
                else
                {
                    Type = stream.ReadInt32();

                    Width = stream.ReadInt16();
                    Height = stream.ReadInt16();

                    Flags = stream.ReadInt32();

                    Reserved = stream.ReadInt32();
                }
            }
        }

        void IDetail.Serialize(Stream stream, IDetailProvider provider)
        {
            stream.Write(UID);
            stream.Write(Handle);

            if (provider.Platform == PlatformType.Wii)
            {
                stream.Write(0xF00 | (Type & 0xFF));
                stream.Write(DataOffset);

                for (int i = 0; i < 8; i++)
                    stream.Write(-1);
            }
            else
            {
                if (provider.Version != 6)
                {
                    stream.Write(0xF00 | (Type & 0xFF));

                    stream.Write((short)1);
                    stream.Write((short)4);
                }

                stream.Write(DataOffset);
                stream.Write(DataSize); // unlike Reflections, we don't write zero ;)

                if (provider.Version != 6)
                {
                    var format = 0;

                    format |= (GetPackedBits(Width) & 0xF) << 20;
                    format |= (GetPackedBits(Height) & 0xF) << 24;
                    format |= (Flags & 0xFFFFF);

                    stream.Write(format);
                    stream.Write(Reserved);
                }
                else
                {
                    stream.Write(Type);

                    stream.Write(Width);
                    stream.Write(Height);

                    stream.Write(Flags);

                    stream.Write(Reserved);
                }
            }
        }
    }
}
