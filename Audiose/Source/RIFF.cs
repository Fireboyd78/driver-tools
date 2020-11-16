using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Audiose
{
    public struct Identifier
    {
        int m_value;

        public static implicit operator int(Identifier id)
        {
            return id.m_value;
        }

        public static implicit operator Identifier(int id)
        {
            return new Identifier(id);
        }

        public static implicit operator Identifier(string id)
        {
            if (id.Length != 4)
                throw new ArgumentException("Identifier MUST be 4 characters long!", nameof(id));

            var c1 = id[0];
            var c2 = id[1];
            var c3 = id[2];
            var c4 = id[3];

            return new Identifier((c4 << 24) | (c3 << 16) | (c2 << 8) | (c1 << 0));
        }

        public override string ToString()
        {
            var str = "";

            if (m_value > 0)
            {
                for (int b = 0; b < 4; b++)
                {
                    var c = (m_value >> (b * 8)) & 0xFF;
                    if (c != 0)
                        str += (char)c;
                }
            }

            return str;
        }

        private Identifier(int id)
        {
            m_value = id;
        }
    }

    public interface IRIFFChunkData
    {
        Identifier Identifier { get; }
        int Size { get; }

        Type Type { get; }
    }

    public struct AudioFormatChunk : IRIFFChunkData
    {
        public Identifier Identifier
        {
            get { return "fmt "; }
        }

        public int Size
        {
            get { return 0x10 /* sizeof(AudioFormatChunk) */; }
        }

        Type IRIFFChunkData.Type
        {
            get { return typeof(AudioFormatChunk); }
        }

        public short AudioFormat;
        public short NumChannels;

        public int SampleRate;
        public int ByteRate;

        public short BlockAlign;
        public short BitsPerSample;

        public AudioFormatChunk(int numChannels, int sampleRate)
            : this(1, numChannels, sampleRate, 16)
        { }

        public AudioFormatChunk(int numChannels, int sampleRate, int bitsPerSample)
            : this(1, numChannels, sampleRate, bitsPerSample)
        { }

        public AudioFormatChunk(int audioFormat, int numChannels, int sampleRate, int bitsPerSample)
        {
            AudioFormat = (short)(audioFormat & 0xFFFF);
            NumChannels = (short)(numChannels & 0xFFFF);
            SampleRate = sampleRate;
            BitsPerSample = (short)(bitsPerSample & 0xFFFF);

            BlockAlign = (short)(NumChannels * (BitsPerSample / 8));
            ByteRate = (SampleRate * 2);
        }
    }

    public static class RIFF
    {
        public static readonly Identifier RIFFIdentifier = "RIFF";
        public static readonly Identifier WAVEIdentifier = "WAVE";

        public static readonly Identifier DATAIdentifier = "data";

        public static readonly int ChunkHeaderSize = 0x8;

        public static int ReadRIFF(this Stream stream)
        {
            if (stream.ReadInt32() == RIFFIdentifier)
            {
                var size = stream.ReadInt32();

                if (stream.ReadInt32() == WAVEIdentifier)
                    return (size - 4);
            }

            return -1;
        }
        
        public static bool FindChunk(this Stream stream, Identifier id)
        {
            while (stream.ReadInt32() != id)
            {
                // oh shit
                if ((stream.Position + 1) > stream.Length)
                    return false;

                var size = stream.ReadInt32();
                stream.Position += size;
            }
            return true;
        }

        public static byte[] ReadChunk(this Stream stream, Identifier id)
        {
            if (stream.FindChunk(id))
            {
                var size = stream.ReadInt32();
                return stream.ReadBytes(size);
            }

            // reset position
            stream.Position -= 4;
            return null;
        }

        public static bool ReadChunk<T>(this Stream stream, ref T chunk)
            where T : struct, IRIFFChunkData
        {
            var buffer = stream.ReadChunk(chunk.Identifier);

            if (buffer == null)
                return false;
            if (buffer.Length != chunk.Size)
                throw new InvalidOperationException("What in the F$@%??");

            var ptr = Marshal.AllocHGlobal(chunk.Size);

            Marshal.Copy(buffer, 0, ptr, chunk.Size);

            chunk = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);

            return true;
        }

        public static byte[] ReadData(this Stream stream)
        {
            return stream.ReadChunk(DATAIdentifier);
        }

        public static void WriteRIFF(this Stream stream, byte[] dataBuffer, params IRIFFChunkData[] chunks)
        {
            stream.WriteRIFF(dataBuffer, false, chunks);
        }

        public static void WriteRIFF(this Stream stream, byte[] dataBuffer, bool writeRawData, params IRIFFChunkData[] chunks)
        {
            var dataSize = dataBuffer.Length;
            var riffSize = (dataSize + ChunkHeaderSize + 0x4 /* + WAVE */);

            foreach (var chunk in chunks)
                riffSize += (chunk.Size + ChunkHeaderSize);
            
            stream.SetLength(riffSize + ChunkHeaderSize);

            stream.WriteChunk(RIFFIdentifier, riffSize);
            stream.Write(WAVEIdentifier);

            foreach (var chunk in chunks)
            {
                stream.WriteChunk(chunk.Identifier, chunk.Size);

                var data = new byte[chunk.Size];
                var ptr = GCHandle.Alloc(chunk, GCHandleType.Pinned);

                Marshal.Copy(ptr.AddrOfPinnedObject(), data, 0, chunk.Size);
                ptr.Free();
                
                stream.Write(data);
            }

            if (writeRawData)
            {
                stream.Write(dataBuffer, 0, dataSize);
            }
            else
            {
                stream.WriteChunk(DATAIdentifier, dataBuffer);
            }
        }
        
        public static void WriteChunk(this Stream stream, Identifier id, int chunkSize)
        {
            stream.Write(id);
            stream.Write(chunkSize);
        }

        public static void WriteChunk(this Stream stream, Identifier id, byte[] buffer)
        {
            var chunkSize = buffer.Length;

            stream.WriteChunk(id, chunkSize);
            stream.Write(buffer, 0, chunkSize);
        }

        public static void WriteChunk<T>(this Stream stream, Identifier id, T chunk)
        {
            var chunkSize = Marshal.SizeOf(typeof(T));

            stream.WriteChunk(id, chunkSize);
            stream.Write(chunk, chunkSize);
        }

        public static void WriteChunk<T>(this Stream stream, T chunk)
            where T : struct, IRIFFChunkData
        {
            stream.WriteChunk(chunk.Identifier, chunk.Size);
            stream.Write(chunk);
        }
    }
}
