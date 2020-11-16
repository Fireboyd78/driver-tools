using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using System.Xml;

namespace Audiose
{
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
