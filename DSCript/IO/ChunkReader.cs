#define log

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using DSCript;
using DSCript.Methods;

namespace DSCript.IO
{
    public static class ChunkReaderExtensions
    {
        [Browsable(false)]
        public static void Seek(this ChunkReader i, long offset, SeekOrigin origin)
        {
            i.Stream.Seek(offset, origin);
        }

        [Browsable(false)]
        public static CTypes ReadType(this ChunkReader i)
        {
            return (CTypes)i.Reader.ReadUInt32();
        }

        [Browsable(false)]
        public static string ReadString(this ChunkReader i, int length)
        {
            return Encoding.UTF8.GetString(i.Reader.ReadBytes(length));
        }

        [Browsable(false)]
        public static string ReadUnicodeString(this ChunkReader i, int length)
        {
            byte[] str = new byte[length];

            for (int s = 0; s < length; s++)
            {
                str[s] = i.Reader.ReadByte();

                if (i.Reader.ReadByte() != 0)
                    --i.Position;
            }

            return Encoding.UTF8.GetString(str);
        }
    }

    public sealed class DSC
    {
        [Browsable(false)]
        public static void Log(string str)
        {
        #if log
            Console.WriteLine(str);
        #else
            return;
        #endif
        }

        [Browsable(false)]
        public static void Log(string str, params object[] args)
        {
        #if log
            Console.WriteLine(str, args);
        #else
            return;
        #endif
        }
    }

    public sealed class ChunkReader
    {
        [Browsable(false)]
        public FileStream Stream { get; private set; }

        [Browsable(false)]
        public BinaryReader Reader { get; private set; }

        public string Filename { get; private set; }

        public bool IsChunk { get; private set; }

        public List<ChunkBlock> Chunk = new List<ChunkBlock>();

        [Browsable(false)]
        public long Position
        {
            get { return Stream.Position; }
            set { Stream.Position = value; }
        }

        [Browsable(false)]
        public void ReadChunks(int s, int i, uint baseOffset)
        {
            Chunk[s].Subs.Insert(i, new SubChunkBlock(i, Chunk[s]));

            var c = Chunk[s].Subs[i];

            c.Magic = Reader.ReadUInt32();

            c.Offset = Reader.ReadUInt32();
            
            c.Unk1 = Reader.ReadByte();
            c.StrLen = Reader.ReadByte();
            c.Unk2 = Reader.ReadByte();
            c.Unk3 = Reader.ReadByte();
            
            c.Size = Reader.ReadUInt32();

            long op = Position;

            if (c.StrLen != 0)
            {
                this.Seek(baseOffset + (c.Offset + c.Size), SeekOrigin.Begin);

                c.Description = this.ReadString(c.StrLen);
                //DSC.Log("{0}",c.Description);
            }

            // SLOW!!
            // DSC.Log(
            //     "{0}, " +
            //     "{1:X}, " +
            //     "{2}, " +
            //     "{3}, " +
            //     "{4}, " +
            //     "{5}, " +
            //     "{6:X}, " +
            //     "{7}", Chunks.Magic2Str(c.Magic), c.Offset, c.Unk1, c.StrLen, c.Unk2, c.Unk3, c.Size, c.Description);

            this.Seek(baseOffset + c.Offset, SeekOrigin.Begin);

            CTypes typ = this.ReadType();

            switch (typ)
            {
                case CTypes.CHUNK:
                    uint subBaseOffset = (uint)(Position - 0x4);

                    int ss = Chunk.Count;
                    int ii = 0;

                    Chunk.Insert(ss, new ChunkBlock(ss, subBaseOffset, c));

                    Chunk[ss].Size = Reader.ReadUInt32();
                    Chunk[ss].SubCount = Reader.ReadInt32();

                    int numSubChunks = Chunk[ss].SubCount - 1;

                    bool recurse = ((ii + 1) != Chunk[ss].SubCount) ? true : false;

                    this.Seek(0x4, SeekOrigin.Current);

                    if (recurse)
                    {
                        for (ii = 0; ii <= numSubChunks; ii++)
                            ReadChunks(ss, ii, subBaseOffset);
                        goto default;
                    }
                    ReadChunks(ss, 0, subBaseOffset);
                    goto default;
                // case (CTypes)0x6:
                //     if ((CTypes)c.Magic == CTypes.MODEL_PACKAGE_PC)
                //         DSC.Log("Found MDPC model, skipping ...");
                //     goto default;
                // case (CTypes)0x1:
                //     if ((CTypes)c.Magic == CTypes.MODEL_PACKAGE_PC_X)
                //         DSC.Log("Found MDXN model, skipping ...");
                //     goto default;
                default:
                    break;
            }

            this.Seek(op, SeekOrigin.Begin);
        }

        [Browsable(false)]
        public ChunkReader(string filename)
        {
            Stopwatch Timer = new Stopwatch();
            Stopwatch AliveTimer = new Stopwatch();

            Timer.Start();

            Filename = filename;

            Stream = File.Open(Filename, FileMode.Open);
            Reader = new BinaryReader(Stream);

            IsChunk = (this.ReadType() == CTypes.CHUNK);

            if (!IsChunk)
            {
                Console.WriteLine("Sorry, this is not a chunk file!");
                return;
            }

            Chunk.Insert(0, new ChunkBlock(0, 0x0));

            Chunk[0].Size = Reader.ReadUInt32();
            Chunk[0].SubCount = Reader.ReadInt32();

            if (Reader.ReadInt32() != ChunkBlock.Version)
            {
                Console.WriteLine("Sorry, this chunk file version is unsupported!");
                return;
            }

            int ck = 0;

            DSC.Log("Please wait...there are {0} chunks that need to be loaded!", Chunk[0].SubCount);
            for (ck = 0; ck < Chunk[0].SubCount; ck++)
            {
                ReadChunks(0, ck, 0x0);
            }
            Timer.Stop();

            Reader.Dispose();
            Stream.Dispose();

            Console.WriteLine(
                "Parsed file in {0}ms" +
                ((Timer.ElapsedMilliseconds >= 1000) ? " / {1:F3} seconds" : ""), Timer.ElapsedMilliseconds, Timer.ElapsedMilliseconds / 1000.0);
        }
    }
}
