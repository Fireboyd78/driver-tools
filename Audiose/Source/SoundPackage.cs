using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using System.Xml;
using System.Xml.Linq;

namespace Audiose
{
    public static class VAG
    {
        // PSX ADPCM coefficients
        private static readonly double[] K0 = { 0, 0.9375, 1.796875, 1.53125, 1.90625 };
        private static readonly double[] K1 = { 0, 0, -0.8125, -0.859375, -0.9375 };

        // PSX ADPCM decoding routine - decodes a single sample
        public static short VagToPCM(byte soundParameter, int soundData, ref double vagPrev1, ref double vagPrev2)
        {
            if (soundData > 7)
                soundData -= 16;

            var sp1 = (soundParameter >> 0) & 0xF;
            var sp2 = (soundParameter >> 4) & 0xF;

            var dTmp1 = soundData * Math.Pow(2.0, (12.0 - sp1));

            var dTmp2 = vagPrev1 * K0[sp2];
            var dTmp3 = vagPrev2 * K1[sp2];

            vagPrev2 = vagPrev1;
            vagPrev1 = dTmp1 + dTmp2 + dTmp3;

            var result = (int)Math.Round(vagPrev1);

            return (short)Math.Min(32767, Math.Max(-32768, result));
        }
        
        public static unsafe byte[] DecodeSound(byte[] buffer, SamplerData sampler = null)
        {
            int numSamples = (buffer.Length >> 4) * 28; // PSX ADPCM data is stored in blocks of 16 bytes each containing 28 samples.

            int loopStart = 0;
            int loopLength = 0;

            var result = new byte[numSamples * 2];

            byte sp = 0;

            double vagPrev1 = 0.0;
            double vagPrev2 = 0.0;
            
            int k = 0;

            fixed (byte* r = result)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (i % 16 == 0)
                    {
                        var ld1 = buffer[i];
                        var ld2 = buffer[i + 1];

                        sp = ld1;

                        if ((ld2 & 0xE) == 6)
                            loopStart = k;

                        if ((ld2 & 0xF) == 3 || (ld2 & 0xF) == 7)
                            loopLength = (k + 28) - loopStart;

                        i += 2;
                    }
                    
                    for (int s = 0; s < 2; s++)
                    {
                        var sd = (buffer[i] >> (s * 4)) & 0xF;

                        ((short*)r)[k++] = VagToPCM(sp, sd, ref vagPrev1, ref vagPrev2);
                    }
                }
            }

            if (sampler != null)
            {
                sampler.Buffer = result;

                if (loopLength > 0)
                {
                    sampler.Loop.Start = loopStart;
                    sampler.Loop.End = (loopStart + loopLength);
                }

                result = sampler.Compile();
            }

            return result;
        }
    }

    public enum GSDFormatType
    {
        Invalid = -1,

        BK01,
        BK31,
    }
    
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

    public interface ISoundBankInfoDetail
    {
        GSDFormatType FormatType { get; }

        int HeaderSize { get; }
        int SampleSize { get; }

        int SampleChannelFlags { get; }

        void SetDataInfo(int offset, int size);

        int DataOffset { get; }
        int DataSize { get; }
        
        void Copy(SoundBank bank);
        void CopyTo(SoundBank bank);
    }

    public struct SoundBankInfo1 : ISoundBankInfoDetail
    {
        GSDFormatType ISoundBankInfoDetail.FormatType
        {
            get { return GSDFormatType.BK01; }
        }

        int ISoundBankInfoDetail.HeaderSize
        {
            get { return 0xC; }
        }

        int ISoundBankInfoDetail.SampleSize
        {
            get { return 0x14; }
        }

        int ISoundBankInfoDetail.SampleChannelFlags
        {
            get { return 0x80; }
        }

        int ISoundBankInfoDetail.DataOffset
        {
            get { return DataOffset; }
        }

        int ISoundBankInfoDetail.DataSize
        {
            get { return DataSize; }
        }

        public int NumSamples;

        public int DataOffset;
        public int DataSize;

        void ISoundBankInfoDetail.SetDataInfo(int offset, int size)
        {
            DataOffset = offset;
            DataSize = size;
        }

        public void Copy(SoundBank bank)
        {
            NumSamples = bank.Samples.Count;
        }

        public void CopyTo(SoundBank bank)
        {
            bank.Samples = new List<SoundSample>(NumSamples);
        }
    }

    public struct SoundBankInfo3 : ISoundBankInfoDetail
    {
        GSDFormatType ISoundBankInfoDetail.FormatType
        {
            get { return GSDFormatType.BK31; }
        }

        int ISoundBankInfoDetail.HeaderSize
        {
            get { return 0x10; }
        }

        int ISoundBankInfoDetail.SampleSize
        {
            get { return 0x10; }
        }

        int ISoundBankInfoDetail.SampleChannelFlags
        {
            get { return 0x10; }
        }

        int ISoundBankInfoDetail.DataOffset
        {
            get { return DataOffset; }
        }

        int ISoundBankInfoDetail.DataSize
        {
            get { return DataSize; }
        }

        public int Index;
        public int NumSamples;

        public int DataOffset;
        public int DataSize;

        void ISoundBankInfoDetail.SetDataInfo(int offset, int size)
        {
            DataOffset = offset;
            DataSize = size;
        }

        public void Copy(SoundBank bank)
        {
            Index = bank.Index;
            NumSamples = bank.Samples.Count;
        }

        public void CopyTo(SoundBank bank)
        {
            bank.Index = Index;
            bank.Samples = new List<SoundSample>(NumSamples);
        }
    }

    public struct SoundSampleInfo
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(SoundSampleInfo));

        public int Offset;
        public int Size;

        public ushort SampleRate;

        public byte Flags;
        public byte Unk_0B;

        public int Unk_0C;
    }

    public struct GSDHeader
    {
        public const int ID_BK01 = 0x424B3130; // '01KB'
        public const int ID_BK31 = 0x31334B42; // 'BK31'

        public int Identifier;
        public int NumBanks;

        public int Alignment
        {
            get { return 2048; }
        }
        
        public int ListOffset
        {
            get { return 8; }
        }

        public int ListSize
        {
            get { return NumBanks * 4; }
        }

        public int FileSizeOffset
        {
            get { return ListOffset + ListSize; }
        }

        public int Size
        {
            get { return FileSizeOffset + 4; }
        }
        
        public GSDFormatType FormatType
        {
            get
            {
                switch (Identifier)
                {
                case ID_BK01: return GSDFormatType.BK01;
                case ID_BK31: return GSDFormatType.BK31;
                }

                return GSDFormatType.Invalid;
            }
        }

        private static int GetIdentifier(GSDFormatType type)
        {
            switch (type)
            {
            case GSDFormatType.BK01: return ID_BK01;               
            case GSDFormatType.BK31: return ID_BK31;
            }

            return -1;
        }

        public Type GetSoundBankType(GSDFormatType type)
        {
            switch (type)
            {
            case GSDFormatType.BK01: return typeof(SoundBankInfo1);
            case GSDFormatType.BK31: return typeof(SoundBankInfo3);
            }
            
            throw new InvalidOperationException("Cannot determine sound bank type.");
        }

        public ISoundBankInfoDetail CreateSoundBank(GSDFormatType type)
        {
            var bankType = GetSoundBankType(type);
            return (ISoundBankInfoDetail)Activator.CreateInstance(bankType);
        }

        public ISoundBankInfoDetail CreateSoundBank(GSDFormatType type, SoundBank bank)
        {
            var result = CreateSoundBank(type);
            result.Copy(bank);

            return result;
        }

        public GSDHeader(GSDFormatType type, int numBanks)
        {
            Identifier = GetIdentifier(type);
            NumBanks = numBanks;
        }
    }

    public interface ISerializer<T>
    {
        void Serialize(T input);
        void Deserialize(T output);
    }

    public class SoundBank : ISerializer<XmlNode>
    {
        public string SubDirectory { get; set; }

        public int Index { get; set; }

        public bool IsNull
        {
            get { return Index == -1; }
        }
        
        public List<SoundSample> Samples { get; set; }

        public void Serialize(XmlNode xml)
        {
            var xmlDoc = (xml as XmlDocument) ?? xml.OwnerDocument;
            var elem = (xml as XmlElement);

            if (elem == null)
            {
                var bankXml = xmlDoc.CreateElement("SoundBank");

                bankXml.SetAttribute("Index", $"{Index:D}");

                Serialize(bankXml);
                xml.AppendChild(bankXml);
            }
            else if (!IsNull)
            {
                foreach (var sample in Samples)
                {
                    var smpXml = xmlDoc.CreateElement("Sample");
                    var smpFile = sample.FileName;
                    
                    smpXml.SetAttribute("File", smpFile);

                    sample.Serialize(smpXml);
                    elem.AppendChild(smpXml);
                }
            }
        }

        public void Deserialize(XmlNode xml)
        {
            foreach (XmlAttribute attr in xml.Attributes)
            {
                var value = attr.Value;

                switch (attr.Name)
                {
                case "Index":
                    Index = int.Parse(value);
                    break;
                }
            }
            
            Samples = new List<SoundSample>();

            foreach (var node in xml.ChildNodes.OfType<XmlElement>())
            {
                var sample = new SoundSample();
                sample.Deserialize(node);
                
                Samples.Add(sample);
            }
        }
    }

    public class SoundSample : ISerializer<XmlNode>
    {
        // relative path (e.g. '00.wav' and NOT 'c:\path\to\file\00.wav')
        public string FileName { get; set; }

        public int NumChannels { get; set; }
        public int SampleRate { get; set; }

        public int Flags { get; set; }

        public int ClearAfter { get; set; }
        public int Unknown2 { get; set; }

        public byte[] Buffer { get; set; }

        public bool IsPS1Format { get; set; }

        public static explicit operator AudioFormatChunk(SoundSample sample)
        {
            return new AudioFormatChunk(sample.NumChannels, sample.SampleRate);
        }

        public void Serialize(XmlNode xml)
        {
            var xmlDoc = (xml as XmlDocument) ?? xml.OwnerDocument;
            var elem = (xml as XmlElement);

            if (elem == null)
            {
                elem = xmlDoc.CreateElement("Sample");
                elem.SetAttribute("File", FileName);

                Serialize(elem);
                xml.AppendChild(elem);
            }
            else
            {
                elem.SetAttribute("NumChannels", $"{NumChannels:D}");
                elem.SetAttribute("SampleRate", $"{SampleRate:D}");

                if (!IsPS1Format)
                {
                    elem.SetAttribute("Flags", $"{Flags:D}");
                    elem.SetAttribute("ClearAfter", $"{ClearAfter:D}");
                    elem.SetAttribute("Unk2", $"{Unknown2:D}");
                }
            }
        }

        public void Deserialize(XmlNode xml)
        {
            foreach (XmlAttribute attr in xml.Attributes)
            {
                var value = attr.Value;

                switch (attr.Name)
                {
                case "File":
                    FileName = value;
                    break;
                case "NumChannels":
                    NumChannels = int.Parse(value);
                    break;
                case "SampleRate":
                    SampleRate = int.Parse(value);
                    break;
                case "Flags":
                    Flags = int.Parse(value);
                    break;
                case "Unk1": // backwards compat
                case "ClearAfter":
                    ClearAfter = int.Parse(value);
                    break;
                case "Unk2":
                    Unknown2 = int.Parse(value);
                    break;
                }
            }

            if (String.IsNullOrEmpty(FileName))
                throw new InvalidOperationException("Empty samples are NOT allowed!");
        }
    }

    public class GSDFile
    {
        public List<SoundBank> Banks { get; set; }

        public GSDFormatType Type { get; set; }

        public void DumpAllBanks(string outDir)
        {
            var gsdXml = new XmlDocument();
            var gsdElem = gsdXml.CreateElement("GameSoundDatabase");

            gsdElem.SetAttribute("Type", $"{Type:D}");

            for (int i = 0; i < Banks.Count; i++)
            {
                var bank = Banks[i];
                var bankDir = Path.Combine(outDir, bank.SubDirectory);
                
                if (!Directory.Exists(bankDir))
                    Directory.CreateDirectory(bankDir);

                var xmlFile = Path.Combine(bankDir, "bank.xml");

                // write bank xml
                var bankXml = new XmlDocument();
                bank.Serialize(bankXml);

                bankXml.Save(xmlFile);

                if (!bank.IsNull)
                {
                    for (int s = 0; s < bank.Samples.Count; s++)
                    {
                        var sample = bank.Samples[s];
                        var sampleFile = Path.Combine(bankDir, sample.FileName);

                        var sampleDir = Path.GetDirectoryName(sampleFile);

                        if (!Directory.Exists(sampleDir))
                            Directory.CreateDirectory(sampleDir);

                        using (var fs = File.Open(sampleFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            var fmtChunk = (AudioFormatChunk)sample;

                            RIFF.WriteRIFF(fs, sample.Buffer, fmtChunk);
                        }
                    }
                }

                // append to GSD xml
                var bankElem = gsdXml.CreateElement("SoundBank");
                
                bankElem.SetAttribute("Index", $"{i:D}");
                bankElem.SetAttribute("File", Path.Combine(bank.SubDirectory, "bank.xml"));

                gsdElem.AppendChild(bankElem);
            }

            gsdXml.AppendChild(gsdElem);
            gsdXml.Save(Path.Combine(outDir, "config.xml"));
        }

        public void LoadXml(string filename)
        {
            var root = Path.GetDirectoryName(filename);

            var xml = new XmlDocument();
            xml.Load(filename);

            var gsdElem = xml.DocumentElement;

            if ((gsdElem == null) || (gsdElem.Name != "GameSoundDatabase"))
                throw new InvalidOperationException("Not a GameSoundDatabase node!");

            var type = int.Parse(gsdElem.GetAttribute("Type"));

            if (!Enum.IsDefined(typeof(GSDFormatType), type))
                throw new InvalidOperationException($"Unknown sound database type '{type}'!");
            
            Type = (GSDFormatType)type;
            
            var banks = new List<SoundBank>();
            var bankRefs = new Dictionary<int, SoundBank>();
            
            foreach (var node in gsdElem.ChildNodes.OfType<XmlElement>())
            {
                var bank = new SoundBank();
                
                if (node.Name != "SoundBank")
                    throw new InvalidOperationException($"What the hell do I do with a '{node.Name}' element?!");

                var index = node.GetAttribute("Index");
                var file = node.GetAttribute("File");

                if (String.IsNullOrEmpty(index))
                    throw new InvalidOperationException("Cannot process a SoundBank node without an index!");

                var bankIdx = int.Parse(index);
                var isRef = false;

                if (!String.IsNullOrEmpty(file))
                {
                    var bankDir = Path.GetDirectoryName(file);
                    var bankFile = Path.Combine(root, file);

                    if (!File.Exists(bankFile))
                        throw new InvalidOperationException($"SoundBank file '{bankFile}' is missing!");

                    var bankDoc = new XmlDocument();
                    bankDoc.Load(bankFile);

                    var bankXml = bankDoc.DocumentElement;

                    bank.SubDirectory = bankDir;
                    bank.Deserialize(bankXml);

                    if (bank.Index != bankIdx)
                        isRef = true;
                }
                else
                {
                    bank.Index = bankIdx;
                    bank.Deserialize(node);
                }

                if (isRef)
                {
                    if (!bank.IsNull)
                    {
                        // reset samples!
                        bank.Samples = new List<SoundSample>();
                        bankRefs.Add(bankIdx, bank);
                    }
                }
                else
                {
                    // fill sample buffers
                    foreach (var sample in bank.Samples)
                    {
                        var sampleFile = Path.Combine(root, bank.SubDirectory, sample.FileName);

                        using (var fs = File.Open(sampleFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            if (fs.ReadRIFF() == -1)
                                throw new InvalidOperationException("RICKY! WHAT HAVE YOU DONE!");

                            var fmtChunk = new AudioFormatChunk();

                            if (fs.ReadChunk(ref fmtChunk))
                            {
                                var data = fs.ReadData();

                                if (data == null)
                                    throw new InvalidOperationException("Empty WAV files are NOT allowed!");

                                sample.NumChannels = fmtChunk.NumChannels;
                                sample.SampleRate = fmtChunk.SampleRate;
                                sample.Buffer = data;
                            }
                            else
                            {
                                throw new InvalidOperationException("Holy shit, what have you done to this WAV file?!");
                            }
                        }
                    }
                }

                banks.Add(bank);
            }

            var gsdBanks = new SoundBank[banks.Count];

            for (int i = 0; i < gsdBanks.Length; i++)
            {
                var bank = banks[i];
                gsdBanks[bank.Index] = bank;
            }

            for (int i = 0; i < gsdBanks.Length; i++)
            {
                if (gsdBanks[i] == null)
                    throw new NullReferenceException($"Bank {i} / {gsdBanks.Length} is MISSING! Double check bank index numbers and try again.");
            }

            Banks = new List<SoundBank>(gsdBanks);
            
            // resolve bank references
            foreach (var bankRef in bankRefs)
            {
                var bank = bankRef.Value;
                var copyBank = Banks[bank.Index];
            
                Banks[bankRef.Key] = copyBank;
            }
        }

        public void LoadBinary(string filename)
        {
            using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var header = fs.Read<GSDHeader>();

                if (header.FormatType == GSDFormatType.Invalid)
                    throw new InvalidOperationException("Not a GSD file!");

                Type = header.FormatType;
                
                Banks = new List<SoundBank>(header.NumBanks);
                
                for (int i = 0; i < header.NumBanks; i++)
                {
                    fs.Position = (header.ListOffset + (i * 4));

                    var offset = fs.ReadInt32();
                    fs.Position = offset;

                    ISoundBankInfoDetail bankDetail = null;

                    if (fs.Position == fs.Length)
                    {
                        // completely empty soundbank!
                        var emptyBank = new SoundBank() {
                            Index = -1,
                        };

                        Banks.Add(emptyBank);
                        continue;
                    }

                    switch (Type)
                    {
                    case GSDFormatType.BK01:
                        var info1 = fs.Read<SoundBankInfo1>();
                        info1.DataOffset += offset;

                        bankDetail = info1;
                        break;
                    case GSDFormatType.BK31:
                        var info3 = fs.Read<SoundBankInfo3>();
                        info3.DataOffset += offset;

                        bankDetail = info3;
                        break;
                    }

                    var bank = new SoundBank() {
                        Index = i, // may be overridden by copy
                        SubDirectory = Path.Combine("Banks", $"{i:D2}"),
                    };

                    bankDetail.CopyTo(bank);
                    
                    // don't read in duplicates -.-
                    if (bank.Index == i)
                    {
                        for (int s = 0; s < bank.Samples.Capacity; s++)
                        {
                            fs.Position = offset + ((s * bankDetail.SampleSize) + bankDetail.HeaderSize);

                            var sampleInfo = fs.Read<SoundSampleInfo>(bankDetail.SampleSize);
                            sampleInfo.Offset += bankDetail.DataOffset;

                            var sample = new SoundSample() {
                                FileName = $"{s:D2}.wav",

                                NumChannels = ((sampleInfo.Flags & bankDetail.SampleChannelFlags) != 0) ? 2 : 1,
                                SampleRate = sampleInfo.SampleRate,

                                Flags = (sampleInfo.Flags & ~bankDetail.SampleChannelFlags),

                                ClearAfter = sampleInfo.Unk_0B,
                                Unknown2 = sampleInfo.Unk_0C,
                            };
                            

                            bank.Samples.Add(sample);

                            // retrieve the buffer
                            var buffer = new byte[sampleInfo.Size];

                            fs.Position = sampleInfo.Offset;
                            fs.Read(buffer, 0, buffer.Length);

                            if (Config.VAG)
                                buffer = VAG.DecodeSound(buffer);
                            
                            // make sure we apply it
                            sample.Buffer = buffer;
                        }
                    }
                    
                    Banks.Add(bank);
                }
            }
        }
        
        public void SaveBinary(string filename)
        {
            using (var fs = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var nBanks = Banks.Count;
                var header = new GSDHeader(Type, nBanks);

                fs.Write(header);
                
                var bankOffsets = new int[nBanks];
                
                var dataSize = header.FileSizeOffset;
                
                for (int i = 0; i < nBanks; i++)
                {
                    var bank = Banks[i];
                    
                    if (bank.Index != i)
                    {
                        if (Type != GSDFormatType.BK31)
                            throw new InvalidOperationException("Driv3r GSD's don't support referenced SoundBank's!");

                        bankOffsets[i] = (bank.IsNull) ? -1 : 0;
                    }
                    else
                    {
                        dataSize = Memory.Align(dataSize, 2048);

                        var bankOffset = dataSize;
                        bankOffsets[i] = bankOffset;
                        
                        var sndBank = header.CreateSoundBank(Type, bank);
                        var samples = bank.Samples;
                        var nSamples = samples.Count;

                        var sampleDataOffset = Memory.Align((sndBank.HeaderSize + (nSamples * sndBank.SampleSize)), 64);
                        var sampleDataSize = 0;

                        var sampleOffsets = new int[nSamples];
                        var sampleSizes = new int[nSamples];

                        for (int s = 0; s < nSamples; s++)
                        {
                            var sample = samples[s];

                            var sampleOffset = sampleDataSize;
                            var sampleSize = sample.Buffer.Length;

                            sampleOffsets[s] = sampleOffset;
                            sampleSizes[s] = sampleSize;

                            sampleDataSize += Memory.Align(sampleSize, 4);
                        }
                        
                        sampleDataSize = Memory.Align(sampleDataSize, 4);
                        sndBank.SetDataInfo(sampleDataOffset, sampleDataSize);

                        var sndBankData = Memory.Copy(sndBank, sndBank.HeaderSize);

                        fs.Position = dataSize;
                        fs.Write(sndBankData);

                        for (int s = 0; s < nSamples; s++)
                        {
                            fs.Position = bankOffset + (sndBank.HeaderSize + (s * sndBank.SampleSize));

                            var sample = samples[s];

                            var sampleInfo = new SoundSampleInfo() {
                                Offset = sampleOffsets[s],
                                Size = sampleSizes[s],
                                SampleRate = (ushort)sample.SampleRate,
                                Flags = (byte)sample.Flags,
                                Unk_0B = (byte)sample.ClearAfter,
                                Unk_0C = sample.Unknown2,
                            };

                            if (sample.NumChannels == 2)
                                sampleInfo.Flags |= (byte)sndBank.SampleChannelFlags;

                            var sampleData = new byte[sndBank.SampleSize];

                            Memory.Fill(MagicNumber.FIREBIRD, sampleData); // ;)
                            Memory.Copy(sampleInfo, sampleData, SoundSampleInfo.SizeOf);

                            fs.Write(sampleData);

                            fs.Position = bankOffset + (sampleDataOffset + sampleInfo.Offset);
                            fs.Write(sample.Buffer);
                        }

                        dataSize += (sampleDataOffset + sampleDataSize);
                    }
                }

                dataSize = Memory.Align(dataSize, header.Alignment);
                fs.SetLength(dataSize);

                // write bank offsets
                for (int i = 0; i < nBanks; i++)
                {
                    var bank = Banks[i];
                    var bankOffset = bankOffsets[i];
                    
                    if (bankOffset == 0)
                        bankOffset = bankOffsets[bank.Index];
                    if (bankOffset == -1)
                        bankOffset = (int)fs.Length; // force to end of file

                    fs.Position = header.ListOffset + (i * 4);
                    fs.Write(bankOffset);
                }

                // write file size
                fs.Position = header.FileSizeOffset;
                fs.Write(dataSize);
            }
        }
    }

    public struct PS1SoundSampleInfo
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(PS1SoundSampleInfo));

        public int Offset;
        public int Size;

        public int Loop;

        public int SampleRate;
    }

    public enum PS1BankType
    {
        Invalid = -1,

        Single,     // SBK
        Multiple,   // BLK
    }
    
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

    // HUGE thank you to TecFox for supplying the PS1 audio decoding stuff!
    // Without him, this wouldn't have been possible! :)
    public class PS1BankFile
    {
        public List<SoundBank> Banks { get; set; }

        public PS1BankType Type { get; set; }
        
        private SoundBank ReadSoundBank(Stream stream, int index)
        {
            // read from a list of sound bank offsets
            var offset = stream.ReadInt32();
            stream.Position = offset;

            return ReadSoundBank(stream, index, offset);
        }

        private SoundBank ReadSoundBank(Stream stream, int index, int baseOffset)
        {
            var bank = new SoundBank() {
                Index = index,
            };
            
            var numSamples = stream.ReadInt32();
            var dataOffset = baseOffset + (numSamples * PS1SoundSampleInfo.SizeOf) + 4;

            var listOffset = (int)stream.Position;

            bank.Samples = new List<SoundSample>(numSamples);

            for (int i = 0; i < numSamples; i++)
            {
                stream.Position = listOffset + (i * PS1SoundSampleInfo.SizeOf);

                var sampleInfo = stream.Read<PS1SoundSampleInfo>();

                if ((i == 0) && (sampleInfo.Offset != 0))
                    throw new InvalidOperationException("Probably not sound data!");

                sampleInfo.Offset += dataOffset;

                if (sampleInfo.Loop == 0)
                    sampleInfo.Size -= 16;  // One-shot sounds have a "silent"  loop block at the end which should be discarded.
                                            // (By definition PSX ADPCM encoded data should also have a 16-byte zero padding at the beginning
                                            //  which doesn't exist in some cases)

                var sample = new SoundSample() {
                    FileName = Path.Combine("Sounds", $"{index:D2}_{i:D2}.wav"),

                    NumChannels = 1,
                    SampleRate = sampleInfo.SampleRate,

                    IsPS1Format = true,
                };

                bank.Samples.Add(sample);

                // retrieve the buffer
                var buffer = new byte[sampleInfo.Size];

                stream.Position = sampleInfo.Offset;
                stream.Read(buffer, 0, buffer.Length);

                sample.Buffer = VAG.DecodeSound(buffer);
            }

            return bank;
        }

        public void LoadBinary(Stream stream, int bankIndex)
        {
            if (Type == PS1BankType.Invalid)
                throw new InvalidOperationException("Bank type must be specified before loading binary data.");
            if (Type == PS1BankType.Multiple)
                throw new InvalidOperationException("What are you doing?!");
            if ((Banks == null) || (bankIndex > Banks.Count))
                throw new ArgumentOutOfRangeException("Not enough space to load a new bank!");

            var baseOffset = (int)stream.Position;
            
            var bank = ReadSoundBank(stream, bankIndex, baseOffset);
            Banks.Insert(bankIndex, bank);
        }

        public void LoadBinary(Stream stream)
        {
            if (Type == PS1BankType.Invalid)
                throw new InvalidOperationException("Bank type must be specified before loading binary data.");

            var baseOffset = (int)stream.Position;

            Banks = new List<SoundBank>();

            switch (Type)
            {
            case PS1BankType.Single:
                {
                    var bank = ReadSoundBank(stream, 0, baseOffset);
                    Banks.Add(bank);
                } break;
            case PS1BankType.Multiple:
                {
                    // get number of banks
                    var numBanks = (stream.ReadInt32() >> 2) - 1;

                    // last entry = buffer size
                    stream.Position = (numBanks * 4);
                    var size = stream.ReadInt32();

                    if (size > stream.Length)
                        throw new OverflowException("Probably not sound data!");

                    Banks = new List<SoundBank>(numBanks);

                    // read banks list
                    for (int i = 0; i < numBanks; i++)
                    {
                        stream.Position = (i * 4);

                        var bank = ReadSoundBank(stream, i);
                        Banks.Add(bank);
                    }
                } break;
            }
        }

        public void SaveXml(XmlNode xml)
        {
            var xmlDoc = (xml as XmlDocument) ?? xml.OwnerDocument;
            var elem = (xml as XmlElement);

            if (elem == null)
            {
                var xmlRoot = xmlDoc.CreateElement("GameSoundDatabase_PS1");

                xmlRoot.SetAttribute("Type", $"{(int)Type}");
                SaveXml(xmlRoot);

                xmlDoc.AppendChild(xmlRoot);
            }
            else
            {
                for (int i = 0; i < Banks.Count; i++)
                {
                    var bank = Banks[i];
                    var bankXml = xmlDoc.CreateElement("SoundBank");

                    bankXml.SetAttribute("Index", $"{i:D}");

                    bank.Serialize(bankXml);

                    xml.AppendChild(bankXml);
                }
            }
        }

        public void SaveSounds(string outDir)
        {
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            for (int i = 0; i < Banks.Count; i++)
            {
                var bank = Banks[i];

                if (!bank.IsNull)
                {
                    for (int s = 0; s < bank.Samples.Count; s++)
                    {
                        var sample = bank.Samples[s];
                        var sampleFile = Path.Combine(outDir, sample.FileName);

                        var sampleDir = Path.GetDirectoryName(sampleFile);

                        if (!Directory.Exists(sampleDir))
                            Directory.CreateDirectory(sampleDir);

                        using (var fs = File.Open(sampleFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            var fmtChunk = (AudioFormatChunk)sample;

                            RIFF.WriteRIFF(fs, sample.Buffer, false, fmtChunk);
                        }
                    }
                }
            }
        }

        public void DumpBanks(string outDir)
        {
            var xml = new XmlDocument();
            
            SaveXml(xml);
            xml.Save(Path.Combine(outDir, "config.xml"));

            SaveSounds(outDir);
        }
    }
}
