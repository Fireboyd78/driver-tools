using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Audiose
{
    [StructLayout(LayoutKind.Sequential, Size = 0x18)]
    public struct SampleInfoData
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(SampleInfoData));

        public int Manufacturer;
        public int Product;

        public int SamplePeriod;

        public int MIDI_UnityNote;
        public int MIDI_PitchFraction;

        public int SMPTE_Format;
        public int SMPTE_Offset;

        public int NumSampleLoops;

        public int SamplerData;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x24)]
    public struct SampleLoopData
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(SampleLoopData));

        public int CuePointID;

        public int Type;

        public int Start;
        public int End;

        public int Fraction;

        public int PlayCount;
    }

    public class SamplerData
    {
        public SampleInfoData Info;
        public SampleLoopData Loop;
        
        public byte[] Buffer { get; set; }

        public int Size
        {
            get { return SampleInfoData.SizeOf + ((Info.NumSampleLoops * SampleLoopData.SizeOf) + Info.SamplerData); }
        }

        public void SetSampleRate(int sampleRate)
        {
            Info.SamplePeriod = (int)((1.0 / sampleRate) * 1000000000);
        }

        private void WriteSamplerChunk(Stream stream)
        {
            using (var ms = new MemoryStream(Size))
            {
                ms.Write(Info);
                ms.Write(Loop);

                stream.WriteChunk("smpl", ms.ToArray());
            }
        }

        public byte[] Compile()
        {
            using (var ms = new MemoryStream(Buffer.Length + Size + 16))
            {
                ms.WriteChunk("data", Buffer);

                WriteSamplerChunk(ms);

                return ms.ToArray();
            }
        }
        
        public SamplerData()
        {
            Info.NumSampleLoops = 1;
            Info.MIDI_UnityNote = 60; // middle-c
        }
    }
}
