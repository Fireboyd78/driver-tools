using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using DSCript;
using DSCript.Spooling;

namespace Audiose
{
    enum ParseResult
    {
        Success,
        Failure,
    }

    class Program
    {
        static readonly string DefaultOutput = "SoundData";
        static readonly string GSDName = "SOUND.GSD";

        static void Abort(int exitCode)
        {
            Console.WriteLine(">> Aborting...");
            Environment.Exit(exitCode);
        }
        
        static ParseResult ParseData()
        {
            var gsdFile = new GSDFile();
            
            if (Config.Extract)
            {
                var chunkFile = new FileChunker();

                var spoolers = new List<SpoolableBuffer>();

                chunkFile.SpoolerLoaded += (s, e) => {
                    // ignore chunks
                    if (!(s is SpoolableBuffer))
                        return;

                    var spooler = s as SpoolableBuffer;

                    switch ((ChunkType)spooler.Context)
                    {
                    case ChunkType.SpooledSoundBank:
                    case ChunkType.SpooledGameSoundBank:
                        spoolers.Add(spooler);
                        break;
                    }
                };

                chunkFile.Load(Config.Input);

                if (spoolers.Count > 0)
                {
                    var soundBanks = new List<SoundBank>();

                    foreach (var spooler in spoolers)
                    {
                        using (var ms = spooler.GetMemoryStream())
                        {
                            var id = ms.ReadInt32();

                            var gsdOffset = 0;
                            var gsdSize = 0;

                            switch (id)
                            {
                            // [VS11] - vehicle sound data
                            case 0x31315356:
                                {
                                    var vsbOffset = ms.ReadInt32();

                                    gsdOffset = ms.ReadInt32();
                                    gsdSize = ms.ReadInt32();
                                }
                                break;
                            // [SS12] - character sound data
                            case 0x32315353:
                                {
                                    // I forgot what this is
                                    ms.Position += 4;

                                    gsdOffset = ms.ReadInt32();
                                    gsdSize = ms.ReadInt32();

                                    var locOffset = ms.ReadInt32();
                                }
                                break;
                            // when in doubt, leave it out
                            default:
                                continue;
                            }
                            
                            ms.Position = gsdOffset;

                            var bank = new SoundBank();

                            var bankInfo = ms.Read<SoundBankInfo3>();
                            bankInfo.DataOffset += gsdOffset;

                            var bankDetail = (ISoundBankInfoDetail)bankInfo;

                            bankInfo.CopyTo(bank);

                            for (int s = 0; s < bank.Samples.Capacity; s++)
                            {
                                ms.Position = gsdOffset + ((s * bankDetail.SampleSize) + bankDetail.HeaderSize);

                                var sampleInfo = ms.Read<SoundSampleInfo>(bankDetail.SampleSize);
                                sampleInfo.Offset += bankDetail.DataOffset;

                                var sample = new SoundSample() {
                                    FileName = $"{s:D2}.wav",

                                    NumChannels = ((sampleInfo.Flags & bankDetail.SampleChannelFlags) != 0) ? 2 : 1,
                                    SampleRate = sampleInfo.SampleRate,

                                    Flags = (sampleInfo.Flags & ~bankDetail.SampleChannelFlags),

                                    Unknown1 = sampleInfo.Unk_0B,
                                    Unknown2 = sampleInfo.Unk_0C,
                                };
                                
                                bank.Samples.Add(sample);

                                // retrieve the buffer
                                var buffer = new byte[sampleInfo.Size];

                                ms.Position = sampleInfo.Offset;
                                ms.Read(buffer, 0, buffer.Length);
                                
                                sample.Buffer = buffer;
                            }

                            soundBanks.Add(bank);
                        }
                    }

                    if (soundBanks.Count > 0)
                    {
                        // make sure we have enough slots
                        var numBanks = 0;

                        foreach (var bank in soundBanks)
                        {
                            if (bank == null)
                                continue;

                            if (bank.Index > numBanks)
                                numBanks = bank.Index;
                        }

                        // set the correct banks so we don't use references
                        var gsdBanks = new SoundBank[numBanks + 1];
                        
                        foreach (var bank in soundBanks)
                            gsdBanks[bank.Index] = bank;
                        
                        var curRef = gsdBanks.First((b) => b != null);

                        // now make sure empty ones are created
                        for (int i = 0; i < gsdBanks.Length; i++)
                        {
                            if (gsdBanks[i] != null)
                            {
                                curRef = gsdBanks[i];
                                continue;
                            }
                            
                            var index = -1;

                            if (i < curRef.Index)
                                index = curRef.Index;
                            
                            gsdBanks[i] = new SoundBank() {
                                Index = index,
                                Samples = new List<SoundSample>(),
                            };
                        }

                        // always assume DPL's format
                        gsdFile.Type = GSDFormatType.BK31;
                        gsdFile.Banks = new List<SoundBank>(gsdBanks);

                        if (Config.Compile)
                        {
                            if (String.IsNullOrEmpty(Config.OutDir))
                                Config.OutDir = Path.GetDirectoryName(Config.Input);

                            gsdFile.SaveBinary(Path.Combine(Config.OutDir, String.Format("{0}.gsd", Path.GetFileName(Config.Input))));
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(Config.OutDir))
                            {
                                Config.OutDir = Path.Combine(Path.GetDirectoryName(Config.Input), DefaultOutput,
                                    Path.GetFileNameWithoutExtension(Config.Input));
                            }

                            gsdFile.DumpAllBanks(Config.OutDir);
                        }
                    }
                    else
                    {
                        Console.WriteLine("WARNING: No sound banks were extracted.");
                    }
                }
                else
                {
                    Console.WriteLine("WARNING: No compatible formats were available for extraction.");
                }
            }
            else
            {
                switch (Config.InputType)
                {
                case FileType.BinaryData:
                    {
                        gsdFile.LoadBinary(Config.Input);

                        if (Config.Compile)
                        {
                            if (String.IsNullOrEmpty(Config.OutDir))
                                Config.OutDir = Path.GetDirectoryName(Config.Input);

                            gsdFile.SaveBinary(Path.Combine(Config.OutDir, GSDName));
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(Config.OutDir))
                                Config.OutDir = Path.Combine(Path.GetDirectoryName(Config.Input), DefaultOutput);

                            gsdFile.DumpAllBanks(Config.OutDir);
                        }
                    }
                    break;
                case FileType.Xml:
                    {
                        if (Config.Extract)
                        {
                            Console.WriteLine($"ERROR: Invalid usage of 'extract' parameter, cannot use an XML file!");
                            return ParseResult.Failure;
                        }

                        gsdFile.LoadXml(Config.Input);

                        if (String.IsNullOrEmpty(Config.OutDir))
                            Config.OutDir = Path.GetDirectoryName(Config.Input);

                        gsdFile.SaveBinary(Path.Combine(Config.OutDir, GSDName));
                    }
                    break;
                default:
                    {
                        Console.WriteLine($"Couldn't determine the input file type of '{Config.Input}'!");
                        return ParseResult.Failure;
                    }
                }
            }

            return ParseResult.Success;
        }

        static ParseResult ParseDataPS1()
        {
            var ps1 = new PS1BankFile();

            if (Config.Extract)
            {
                Console.WriteLine("'Extract' argument is invalid for PS1 sound data.");
                return ParseResult.Failure;
            }

            if (Config.Compile)
            {
                Console.WriteLine("Cannot compile PS1 sound data, operation unsupported.");
                return ParseResult.Failure;
            }

            ps1.Type = (Config.InputType == FileType.Sbk) ? PS1BankType.Single : PS1BankType.Multiple;

            using (var fs = File.Open(Config.Input, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ps1.LoadBinary(fs);

                if (String.IsNullOrEmpty(Config.OutDir))
                    Config.OutDir = Path.Combine(Path.GetDirectoryName(Config.Input), $"{Path.GetFileNameWithoutExtension(Config.Input)}_Data");

                ps1.DumpAllBanks(Config.OutDir);
            }

            return ParseResult.Success;
        }

        static ParseResult Parse()
        {
            switch (Config.InputType)
            {
            case FileType.Blk:
            case FileType.Sbk:
                return ParseDataPS1();
            }

            return ParseData();
        }

        static void Run()
        {
            switch (Parse())
            {
            case ParseResult.Success:
                Console.WriteLine(Config.GetSuccessMessage());
                break;
            case ParseResult.Failure:
                Abort(2);
                break;
            }
        }

        static void Initialize()
        {
            // make sure the user's culture won't F$#% up anything!
            Thread.CurrentThread.CurrentCulture = Config.Culture;
            Thread.CurrentThread.CurrentUICulture = Config.Culture;

            Console.WriteLine($"Audiose Sound Editor ({Config.VersionString})");
        }

        static void Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                Initialize();

                Config.ProcessArgs(args);
                Run();
            }
            else
            {
                try
                {
                    if (args.Length > 0)
                    {
                        if (Config.ProcessArgs(args))
                        {
                            Run();
                        }
                        else
                        {
                            Abort(1);
                        }
                    }
                    else
                    {
                        Console.WriteLine(Config.UsageString);
                        Environment.Exit(0);
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERROR: {e.Message}\r\n");

                    Console.Error.WriteLine("==================== Stack Trace ====================");
                    Console.Error.WriteLine($"<{e.GetType().FullName}>:");
                    Console.Error.WriteLine(e.StackTrace);
                }
            }
        }
    }
}
