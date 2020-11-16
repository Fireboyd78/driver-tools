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
    public struct SoundDatabaseHeader
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
        
        public SoundBankFormat FormatType
        {
            get
            {
                switch (Identifier)
                {
                case ID_BK01: return SoundBankFormat.BK01;
                case ID_BK31: return SoundBankFormat.BK31;
                }

                return SoundBankFormat.Invalid;
            }
        }

        private static int GetIdentifier(SoundBankFormat type)
        {
            switch (type)
            {
            case SoundBankFormat.BK01: return ID_BK01;               
            case SoundBankFormat.BK31: return ID_BK31;
            }

            return -1;
        }

        public Type GetSoundBankType(SoundBankFormat type)
        {
            switch (type)
            {
            case SoundBankFormat.BK01: return typeof(SoundBankInfo1);
            case SoundBankFormat.BK31: return typeof(SoundBankInfo3);
            }
            
            throw new InvalidOperationException("Cannot determine sound bank type.");
        }

        public ISoundBankInfoDetail CreateSoundBank(SoundBankFormat type)
        {
            var bankType = GetSoundBankType(type);
            return (ISoundBankInfoDetail)Activator.CreateInstance(bankType);
        }

        public ISoundBankInfoDetail CreateSoundBank(SoundBankFormat type, SoundBank bank)
        {
            var result = CreateSoundBank(type);
            result.Copy(bank);

            return result;
        }

        public SoundDatabaseHeader(SoundBankFormat type, int numBanks)
        {
            Identifier = GetIdentifier(type);
            NumBanks = numBanks;
        }
    }

    public class SoundDatabase
    {
        public List<SoundBank> Banks { get; set; }

        public SoundBankFormat Type { get; set; }

        public void UnpackBanks(string outDir)
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

            if (!Enum.IsDefined(typeof(SoundBankFormat), type))
                throw new InvalidOperationException($"Unknown sound database type '{type}'!");

            Type = (SoundBankFormat)type;

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
                var header = fs.Read<SoundDatabaseHeader>();

                if (header.FormatType == SoundBankFormat.Invalid)
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
                        var emptyBank = new SoundBank()
                        {
                            Index = -1,
                        };

                        Banks.Add(emptyBank);
                        continue;
                    }

                    switch (Type)
                    {
                    case SoundBankFormat.BK01:
                        var info1 = fs.Read<SoundBankInfo1>();
                        info1.DataOffset += offset;

                        bankDetail = info1;
                        break;
                    case SoundBankFormat.BK31:
                        var info3 = fs.Read<SoundBankInfo3>();
                        info3.DataOffset += offset;

                        bankDetail = info3;
                        break;
                    }

                    var bank = new SoundBank()
                    {
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

                            var sample = new SoundSample()
                            {
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
                var header = new SoundDatabaseHeader(Type, nBanks);

                fs.Write(header);

                var bankOffsets = new int[nBanks];

                var dataSize = header.FileSizeOffset;

                for (int i = 0; i < nBanks; i++)
                {
                    var bank = Banks[i];

                    if (bank.Index != i)
                    {
                        if (Type != SoundBankFormat.BK31)
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

                            var sampleInfo = new SoundSampleInfo()
                            {
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
}
