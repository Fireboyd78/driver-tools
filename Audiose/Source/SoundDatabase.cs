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
        public const int ID_CS11 = 0x31315343; // 'CS11'
        public const int ID_CS12 = 0x32315343; // 'CS12'

        public int Identifier;
        public int NumBanks;

        public int Alignment
        {
            get { return 2048; }
        }
        
        public int ListOffset
        {
            get { return (FormatType >= SoundBankFormat.CS11) ? 16 : 8; }
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
                case ID_CS11: return SoundBankFormat.CS11;
                case ID_CS12: return SoundBankFormat.CS12;
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
            case SoundBankFormat.CS11: return ID_CS11;
            case SoundBankFormat.CS12: return ID_CS12;
            }

            return -1;
        }

        public Type GetSoundBankType(SoundBankFormat type)
        {
            switch (type)
            {
            case SoundBankFormat.BK01: return typeof(SoundBankInfo1);
            case SoundBankFormat.BK31: return typeof(SoundBankInfo3);
            case SoundBankFormat.CS11: return typeof(CharacterSoundBankInfo);
            case SoundBankFormat.CS12: return typeof(CharacterSoundBankInfo);
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
        public Dictionary<int, int> BankMap { get; set; }

        public List<SoundBank> Banks { get; set; }

        public SoundBankFormat Type { get; set; }

        public void UnpackBanks(string outDir)
        {
            var characters = (Type >= SoundBankFormat.CS11);

            if (characters)
            {
                outDir = Path.Combine(Path.GetDirectoryName(outDir), "CharacterSoundData");

                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
            }

            var xmlDoc = new XmlDocument();
            var xmlRoot = xmlDoc.CreateElement("GameSoundDatabase");

            xmlRoot.SetAttribute("Type", $"{Type:D}");

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
                var bankElem = xmlDoc.CreateElement("SoundBank");

                bankElem.SetAttribute("Index", $"{i:D}");
                bankElem.SetAttribute("File", Path.Combine(bank.SubDirectory, "bank.xml"));

                xmlRoot.AppendChild(bankElem);
            }

            if (BankMap != null)
            {
                var bankMapElem = xmlDoc.CreateElement("BankMap");
                foreach (var bank in BankMap)
                {
                    var bankElem = xmlDoc.CreateElement("Bank");

                    bankElem.SetAttribute("Id", $"{bank.Key}");
                    bankElem.SetAttribute("Index", $"{bank.Value}");

                    bankMapElem.AppendChild(bankElem);
                }
                xmlRoot.AppendChild(bankMapElem);
            }

            xmlDoc.AppendChild(xmlRoot);
            xmlDoc.Save(Path.Combine(outDir, "config.xml"));
        }

        public void LoadXml(string filename)
        {
            var root = Path.GetDirectoryName(filename);

            var xml = new XmlDocument();
            xml.Load(filename);

            var xmlRoot = xml.DocumentElement;

            if ((xmlRoot == null) || (xmlRoot.Name != "GameSoundDatabase"))
                throw new InvalidOperationException("Not a GameSoundDatabase node!");

            var type = int.Parse(xmlRoot.GetAttribute("Type"));

            if (!Enum.IsDefined(typeof(SoundBankFormat), type))
                throw new InvalidOperationException($"Unknown sound database type '{type}'!");

            Type = (SoundBankFormat)type;

            if (Type >= SoundBankFormat.CS11)
                throw new NotImplementedException("CharacterSoundDatabase saving not quite ready yet, sorry!");

            var banks = new List<SoundBank>();
            var bankRefs = new Dictionary<int, SoundBank>();

            foreach (var node in xmlRoot.ChildNodes.OfType<XmlElement>())
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

                banks.Insert(bankIdx, bank);
            }

            Banks = new List<SoundBank>(banks);

            // resolve bank references
            foreach (var bankRef in bankRefs)
            {
                var bank = bankRef.Value;
                var copyBank = Banks[bank.Index];

                Banks[bankRef.Key] = copyBank;
            }
        }

        private void LoadSoundBankSamples(Stream fs, SoundBank bank, int offset, ISoundBankInfoDetail bankDetail)
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

                    Priority = sampleInfo.Priority,
                    LoopPoint = sampleInfo.LoopPoint,

                    IsXBoxFormat = Config.XBox,
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

        public void LoadBinary(string filename)
        {
            using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var header = fs.Read<SoundDatabaseHeader>();

                if (header.FormatType == SoundBankFormat.Invalid)
                    throw new InvalidOperationException("Not a GSD/DAT file!");

                Type = header.FormatType;

                var luBankMap = new Dictionary<int, int>();
                var luBankOffsets = new Dictionary<int, int>();
                var luBankSubOffsets = new Dictionary<int, List<int>>();

                var bMultiBankSamples = false;

                if (Type >= SoundBankFormat.CS11)
                {
                    var unk_08 = fs.ReadInt32(); // 2048
                    var subBankSize = fs.ReadInt32(); // 0x4800

                    // real number of banks located in separate list
                    fs.Position = header.NumBanks;

                    var numBanks = fs.ReadInt32();
                    var bankList = fs.Position;

                    // collect offsets
                    for (int i = 0; i < numBanks; i++)
                    {
                        fs.Position = bankList + (i * 4);

                        var bankIndex = fs.ReadInt32();

                        if (!luBankOffsets.ContainsKey(bankIndex))
                        {
                            fs.Position = header.ListOffset + (bankIndex * 4);
                            var bankOffset = fs.ReadInt32();

                            luBankOffsets.Add(bankIndex, bankOffset);
                        }

                        luBankMap.Add(i, bankIndex);
                    }

                    // add a fake final entry
                    luBankMap.Add(numBanks, -1);
                    fs.Seek(0, SeekOrigin.End);
                    luBankOffsets.Add(-1, (int)fs.Position);

                    BankMap = new Dictionary<int, int>();

                    // determine the true number of banks
                    for (int i = 0; i < numBanks; i++)
                    {
                        var bankIndex = luBankMap[i];
                        var nextIndex = bankIndex + 1;

                        if (!luBankOffsets.ContainsKey(nextIndex))
                        {
                            var bankOffset = header.ListOffset + (nextIndex * 4);

                            fs.Position = bankOffset;
                            var offset = fs.ReadInt32();

                            if (offset != 0)
                            {
                                Debug.WriteLine($"bank {i}({bankIndex}) neighbor({nextIndex}) is unreferenced!");
                                luBankOffsets.Add(nextIndex, offset);
                            }
                        }

                        BankMap.Add(i, bankIndex);
                    }

                    header.NumBanks = luBankOffsets.OrderBy((kv) => kv.Key).Last().Key;

                    if (Type == SoundBankFormat.CS11)
                    {
                        // count number of banks/multisamples
                        for (int i = 0; i < header.NumBanks; i++)
                        {
                            if (luBankSubOffsets.ContainsKey(i))
                                continue;

                            var bankOffset = luBankOffsets[i];

                            var bankCount = 0;
                            var totalCount = 0;

                            var subOffsets = new List<int>();

                            // figure out how many banks/multisamples there are
                            do
                            {
                                var subOffset = (totalCount * subBankSize);
                                fs.Position = bankOffset + subOffset;

                                var reserved = fs.ReadInt32();

                                if (reserved != 0)
                                    throw new Exception("STRANGE CHARACTER SOUND DATA - maybe that wasn't supposed to be zero!");

                                var info = fs.Read<CharacterSoundBankInfo>();
                                info.DataOffset += bankOffset;

                                if (info.NumSamples > 1)
                                    throw new Exception("STRANGE CHARACTER SOUND DATA - more than one sample!");

                                if (info.NumSamples != 0)
                                {
                                    subOffsets.Add(subOffset);
                                    bankCount++;

                                    if (subOffsets.Count != bankCount)
                                        throw new Exception("STRANGE CHARACTER SOUND DATA - empty sub bank in-between non-empty ones!");
                                }

                                totalCount++;
                            } while (fs.Position + subBankSize < luBankOffsets[i + 1]);

                            if (totalCount > 48)
                                throw new Exception("STRANGE CHARACTER SOUND DATA - more than expected sub banks!");

                            luBankSubOffsets.Add(i, subOffsets);
                        }
                    }
                    else
                    {
                        // count number of banks/multisamples
                        for (int i = 0; i < header.NumBanks; i++)
                        {
                            if (luBankSubOffsets.ContainsKey(i))
                                continue;

                            var bankOffset = luBankOffsets[i];

                            var bankCount = 0;
                            var totalCount = 0;

                            var subOffsets = new List<int>();

                            // figure out how many banks/multisamples there are
                            do
                            {
                                fs.Position = bankOffset + (totalCount * 4);

                                var subOffset = fs.ReadInt32();

                                if (subOffset == 0)
                                    break;

                                fs.Position = subOffset;
                                subOffset -= bankOffset;

                                var reserved = fs.ReadInt32();

                                if (reserved != 0)
                                    throw new Exception("STRANGE CHARACTER SOUND DATA - maybe that wasn't supposed to be zero!");

                                var info = fs.Read<CharacterSoundBankInfo>();
                                info.DataOffset += bankOffset;

                                if (info.NumSamples > 1)
                                    throw new Exception("STRANGE CHARACTER SOUND DATA - more than one sample!");

                                if (info.NumSamples != 0)
                                {
                                    subOffsets.Add(subOffset);
                                    bankCount++;

                                    if (subOffsets.Count != bankCount)
                                        throw new Exception("STRANGE CHARACTER SOUND DATA - empty sub bank in-between non-empty ones!");
                                }

                                totalCount++;
                            } while (fs.Position < luBankOffsets[i + 1]);

                            if (totalCount > 56)
                                throw new Exception("STRANGE CHARACTER SOUND DATA - more than expected sub banks!");

                            luBankSubOffsets.Add(i, subOffsets);
                        }
                    }

                    bMultiBankSamples = true;
                }
                else
                {
                    fs.Position = header.ListOffset;

                    for (int i = 0; i < header.NumBanks; i++)
                    {
                        var bankOffset = fs.ReadInt32();

                        luBankOffsets.Add(i, bankOffset);
                        luBankMap.Add(i, i);
                    }
                }

                Banks = new List<SoundBank>(header.NumBanks);

                for (int i = 0; i < header.NumBanks; i++)
                {
                    var bankOffset = luBankOffsets[i];
                    fs.Position = bankOffset;

                    ISoundBankInfoDetail bankDetail = null;

                    if (bMultiBankSamples)
                    {
                        // read CHRSOUND.DAT format
                        var subOffsets = luBankSubOffsets[i];
                        
                        var bank = new SoundBank()
                        {
                            Index = i,
                            SubDirectory = Path.Combine("Banks", $"{i:D2}"),
                        };
                        
                        var subBanks = new List<SoundBank>();
                        
                        // load in all sub-banks
                        foreach (var subOffset in subOffsets)
                        {
                            var subBankOffset = bankOffset + subOffset;
                        
                            fs.Position = subBankOffset + 4;
                        
                            var info = fs.Read<CharacterSoundBankInfo>();
                            info.DataOffset += subBankOffset;
                        
                            var subBank = new SoundBank();
                        
                            info.CopyTo(subBank);
                        
                            LoadSoundBankSamples(fs, subBank, subBankOffset, info);
                        
                            subBanks.Add(subBank);
                        }

                        bank.Samples = new List<SoundSample>();
                        
                        // merge the sub banks into the top-level one
                        foreach (var subBank in subBanks)
                        {
                            bank.Samples.AddRange(subBank.Samples);
                            subBank.Samples.Clear();
                        }

                        // fixup samples
                        for (int s = 0; s < bank.Samples.Count; s++)
                        {
                            var sample = bank.Samples[s];

                            sample.FileName = $"{s:D2}.wav";

                            if (Type == SoundBankFormat.CS11)
                                sample.IsXBoxFormat = true;
                        }

                        Banks.Add(bank);
                    }
                    else
                    {
                        // read GSD format
                        if (fs.Position == fs.Length)
                        {
                            // completely empty soundbank!
                            var emptyBank = new SoundBank()
                            {
                                Index = -1,
                                SubDirectory = Path.Combine("Banks", $"{i:D2}"),
                            };

                            Banks.Add(emptyBank);
                            continue;
                        }

                        switch (Type)
                        {
                        case SoundBankFormat.BK01:
                            var info1 = fs.Read<SoundBankInfo1>();
                            info1.DataOffset += bankOffset;

                            bankDetail = info1;
                            break;
                        case SoundBankFormat.BK31:
                            var info3 = fs.Read<SoundBankInfo3>();
                            info3.DataOffset += bankOffset;

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
                            LoadSoundBankSamples(fs, bank, bankOffset, bankDetail);

                        Banks.Add(bank);
                    }
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
                                Priority = (byte)sample.Priority,
                                LoopPoint = sample.LoopPoint,
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
