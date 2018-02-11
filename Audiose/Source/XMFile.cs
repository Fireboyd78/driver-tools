using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Audiose
{
    // implies the data is prefaced by a length specifier (which includes itself)
    public interface IXMHeaderDetail { }

    public static class XMStreamExtensions
    {
        public static T ReadXMHeader<T>(this Stream stream)
            where T : struct, IXMHeaderDetail
        {
            var length = stream.ReadInt32();

            return stream.ReadXMStruct<T>(length - 4);
        }

        public static T ReadXMStruct<T>(this Stream stream)
            where T : struct
        {
            var length = Marshal.SizeOf(typeof(T));

            return stream.ReadXMStruct<T>(length);
        }

        public static T ReadXMStruct<T>(this Stream stream, int length)
            where T : struct
        {
            var obj = new T();
            var objSize = Marshal.SizeOf(obj);

            if (length > objSize)
                throw new InvalidOperationException("Length exceeds size of type!");

            var ptr = Marshal.AllocHGlobal(objSize);

            Marshal.StructureToPtr(obj, ptr, false);

            var buffer = new byte[length];
            stream.Read(buffer, 0, length);

            Marshal.Copy(buffer, 0, ptr, length);
            obj = (T)Marshal.PtrToStructure(ptr, typeof(T));

            Marshal.FreeHGlobal(ptr);

            return obj;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct XMHeader
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 17)]
        public string IDText;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string ModuleName;

        public byte Reserved; // 0x1A

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string TrackerName;

        public short Version;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XMDetail : IXMHeaderDetail
    {
        public short SongLength;
        public short RestartPosition;
        public short NumChannels;
        public short NumPatterns;
        public short NumInstruments;
        public short Flags;
        public short Tempo;
        public short BPM;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] PatternOrderTable;
        
        public XMPattern[] Patterns;
        public XMInstrument[] Instruments;

        public void ReadData(Stream stream)
        {
            Patterns = new XMPattern[NumPatterns];

            for (int i = 0; i < NumPatterns; i++)
            {
                var pattern = stream.ReadXMHeader<XMPattern>();
                pattern.ReadData(stream);

                Patterns[i] = pattern;
            }

            Instruments = new XMInstrument[NumInstruments];

            for (int i = 0; i < NumInstruments; i++)
            {
                var instrument = stream.ReadXMHeader<XMInstrument>();
                instrument.ReadData(stream);

                Instruments[i] = instrument;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XMPattern : IXMHeaderDetail
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Data
        {
            public byte Note;
            public byte Instrument;
            public byte VolumeColumn;
            public byte EffectType;
            public byte EffectParameter;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Row
        {
            public Data[] Notes;
        }
        
        public byte PackingType;

        public short NumRows;
        public short PackedSize;

        // needs to be filled in manually
        public Row[] Rows;

        public void ReadData(Stream stream)
        {
            Rows = new Row[NumRows];

            // TODO: Process the data?
            stream.Position += PackedSize;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct XMInstrument : IXMHeaderDetail
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 22)]
        public string Name;

        public byte Type;

        public short NumSamples;

        /*
            Following is present if NumSamples > 0
        */

        public int SampleHeaderSize;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public byte[] SampleNotes; // Sample number for all notes

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public byte[] VolumePoints;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public byte[] PanningPoints;

        public byte NumVolumePoints;
        public byte NumPanningPoints;

        public byte VolumeSustainPoint;
        public byte VolumeLoopStartPoint;
        public byte VolumeLoopEndPoint;

        public byte PanningSustainPoint;
        public byte PanningLoopStartPoint;
        public byte PanningLoopEndPoint;

        public byte VolumeType;
        public byte PanningType;

        public byte VibratoType;
        public byte VibratoSweep;
        public byte VibratoDepth;
        public byte VibratoRate;

        public short VolumeFadeout;

        public short Reserved;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string ExtraInfo; // not part of spec

        public XMSample[] Samples;

        public void ReadData(Stream stream)
        {
            Samples = new XMSample[NumSamples];

            for (int i = 0; i < NumSamples; i++)
            {
                var sampleHeader = stream.ReadXMStruct<XMSample>(SampleHeaderSize);
                sampleHeader.ReadData(stream);

                Samples[i] = sampleHeader;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct XMSample
    {
        public int SampleLength;
        public int SampleLoopStart;
        public int SampleLoopLength;

        public byte Volume;
        public byte FineTune;
        public byte Type;
        public byte Panning;

        public sbyte RelativeNote;

        public byte Reserved;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 22)]
        public string SampleName;

        public short[] SampleData;

        public void ReadData(Stream stream)
        {
            var is16Bit = ((Type & 8) != 0);
            var count = SampleLength;

            if (is16Bit)
                count >>= 1;

            SampleData = new short[count];
            
            for (int i = 0; i < count; i++)
                SampleData[i] = (short)((is16Bit) ? stream.ReadInt16() : stream.ReadByte());
        }
    }

    public class XMFile
    {
        public XMDetail Detail;
        
        public string ModuleName { get; set; }
        public string TrackerName { get; set; }

        public int Version { get; set; }

        public void LoadBinary(string filename)
        {
            using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var header = fs.ReadXMStruct<XMHeader>();

                if (header.Reserved != 0x1A)
                    throw new InvalidOperationException("Not an XM file!");

                ModuleName = header.ModuleName;
                TrackerName = header.TrackerName;

                Version = (header.Version & 0xFFFF);

                Detail = fs.ReadXMHeader<XMDetail>();
                Detail.ReadData(fs);
            }
        }
    }

    public static class SampleLoader
    {
        public unsafe static byte[] LoadSample16Bit(byte[] buffer)
        {
            var length = buffer.Length;
            var result = new byte[length];

            Buffer.BlockCopy(buffer, 0, result, 0, length);

            fixed (byte* r = result)
            fixed (byte* buf = buffer)
            {
                short delta = 0;

                for (int i = 0; i < (length >> 1); i++)
                {
                    var tmp = ((short*)buf)[i];
                    ((short*)r)[i] = (short)(tmp - delta);
                    delta = tmp;

                }
            }

            return result;
        }
    }
}
