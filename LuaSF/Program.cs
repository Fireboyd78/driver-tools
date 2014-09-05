using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSCript;
using DSCript.Spooling;

namespace LuaSF
{
    class ScriptData
    {
        public uint Checksum { get; set; }

        public string Filename { get; set; }

        public byte[] Buffer { get; set; }
    }

    sealed class ScriptPackageFile : FileChunker
    {
        public static readonly ChunkType ScriptPackageHeader         = (ChunkType)0x48524353;
        public static readonly ChunkType ScriptPackageLookup         = (ChunkType)0x43524353;
        public static readonly ChunkType ScriptPackageRoot           = (ChunkType)0x52524353;
        public static readonly ChunkType ScriptPackageCompiledScript = (ChunkType)0x53524353;

        public uint UID { get; set; }

        public List<ScriptData> Scripts { get; set; }

        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            if ((ChunkType)sender.Magic == ScriptPackageRoot)
            {
                var scrr = sender as SpoolablePackage;

                var scrh = scrr.GetFirstChild(ScriptPackageHeader) as SpoolableBuffer;
                var scrc = scrr.GetFirstChild(ScriptPackageLookup) as SpoolableBuffer;
                var scrs = scrr.GetFirstChild(ScriptPackageCompiledScript) as SpoolableBuffer;

                var nScripts = 0;

                // read header
                using (var f = scrh.GetMemoryStream())
                {
                    if (f.ReadInt32() != 0x1)
                    {
                        throw new Exception("ERROR - Bad magic! Cannot load Script Package Header!");
                    }

                    nScripts = f.ReadInt32();

                    UID = f.ReadUInt32();

                    if (f.ReadInt32() != 0x2)
                    {
                        Console.WriteLine("WARNING: The script header is possibly corrupted. Unknown errors may occur during data extraction.");
                        Console.WriteLine("Press enter to continue.");
                        
                        Console.ReadKey();
                    }
                }

                var lookupTable = new Dictionary<int, ScriptData>(nScripts);

                // get lookup data
                using (var f = scrc.GetMemoryStream())
                {
                    for (int i = 0; i < nScripts; i++)
                    {
                        f.Position = (i * 0xC);

                        lookupTable.Add(
                            f.ReadInt32(),
                            new ScriptData() {
                                Checksum = f.ReadUInt32()
                        });
                    }
                }
                
                var scriptChunker = Path.GetFileNameWithoutExtension(this.FileName);
                var scriptFolder = String.Format(@"{0}\Resources\{1}", Environment.CurrentDirectory, scriptChunker);

                var lookupFile = String.Format(@"{0}\{1}.slk", scriptFolder, scriptChunker);

                if (!Directory.Exists(scriptFolder))
                    Directory.CreateDirectory(scriptFolder);

                // get script data
                using (var slk = File.Create(lookupFile))
                using (var f = scrs.GetMemoryStream())
                {
                    // calculate sizes
                    for (int i = 0; i < lookupTable.Count; i++)
                    {
                        var kv = lookupTable.ElementAt(i);

                        int size = 0;

                        if ((i + 1) < lookupTable.Count)
                        {
                            var nxt = lookupTable.ElementAt(i + 1);
                            size = (nxt.Key - kv.Key);
                        }
                        else
                        {
                            size = (scrs.Size - kv.Key);
                        }

                        ((ScriptData)kv.Value).Buffer = new byte[size];
                    }

                    slk.Write(0x35395453);
                    slk.Write(nScripts);
                    slk.Write(0x10);
                    slk.Write(0x2);

                    Console.WriteLine("Decompiling scripts...please wait");

                    var idx = 0;

                    var cL = Console.CursorLeft;
                    var cT = Console.CursorTop;

                    foreach (var kv in lookupTable)
                    {
                        var scriptData = kv.Value as ScriptData;

                        idx += 1;

                        // key is offset
                        f.Position = kv.Key;

                        // the buffer has already been set up
                        // now we just need to populate it
                        var buf = scriptData.Buffer;

                        f.Read(buf, 0, buf.Length);

                        using (var fB = new MemoryStream(buf))
                        {
                            // since we don't know the original file names,
                            // we'll use some debug information from the LuaQ data
                            fB.Position = 0xC;

                            var strLen = fB.ReadInt32();
                            var filename = fB.ReadString(strLen);

                            var lIdx = filename.IndexOf("luascripts");

                            // we can't extract this file :(
                            if (lIdx == -1)
                                continue;

                            filename = filename.Substring(lIdx).Split('\0')[0];

                            // write to the .slk file
                            slk.Write(scriptData.Checksum);
                            slk.Write(filename + "\0");

                            filename = Path.Combine(scriptFolder, filename);

                            var fDir = Path.GetDirectoryName(filename);

                            if (!Directory.Exists(fDir))
                                Directory.CreateDirectory(fDir);

                            // we need to use a temp file in order for 'unluac' to decompile the data
                            var tmp = Path.GetTempFileName();

                            File.WriteAllBytes(tmp, buf);

                            Console.SetCursorPosition(cL, cT);
                            Console.Write("Progress: {0} / {1}", idx, nScripts);

                            // start up the process
                            using (var cmd = new System.Diagnostics.Process() {
                                StartInfo = new System.Diagnostics.ProcessStartInfo() {
                                    FileName                = "cmd.exe",
                                    Arguments               = String.Format("/C java -jar unluac.jar {0}", tmp),
                                    WorkingDirectory        = Environment.CurrentDirectory,
                                    RedirectStandardError   = true,
                                    RedirectStandardInput   = true,
                                    RedirectStandardOutput  = true,
                                    UseShellExecute         = false
                                },
                                EnableRaisingEvents = true
                            })
                            {
                                var sb = new StringBuilder();
                                
                                cmd.OutputDataReceived += (o, ev) => {
                                    sb.AppendLine(ev.Data);
                                };

                                cmd.Start();
                                cmd.BeginOutputReadLine();

                                cmd.WaitForExit();

                                using (var fs = File.Create(filename))
                                {
                                    fs.Write(sb.ToString());
                                }

                                cmd.CancelOutputRead();
                            }

                            // we're done with the temp file, delete it
                            File.Delete(tmp);
                        }
                    }

                    slk.Write(Path.GetFileName(this.FileName) + "\0");

                    Console.WriteLine();
                    Console.WriteLine("Operation complete.");
                }
            }
        }

        protected override void OnFileSaveBegin()
        {
            var scrr = new SpoolablePackage() {
                Alignment   = SpoolerAlignment.Align2048,
                Magic       = (int)ScriptPackageRoot,
                Reserved    = 0,
                Description = "Script Package Root"
            };

            var scrh = new SpoolableBuffer() {
                Alignment   = SpoolerAlignment.Align2048,
                Magic       = (int)ScriptPackageHeader,
                Reserved    = 1,
                Description = "Script Package Header"
            };

            scrr.Children.Add(scrh);

            var scrc = new SpoolableBuffer() {
                Alignment   = SpoolerAlignment.Align2048,
                Magic       = (int)ScriptPackageLookup,
                Reserved    = 0,
                Description = "Script Package Lookup"
            };

            scrr.Children.Add(scrc);

            var scrs = new SpoolableBuffer() {
                Alignment   = SpoolerAlignment.Align2048,
                Magic       = (int)ScriptPackageCompiledScript,
                Reserved    = 0,
                Description = "Script Package Compiled Script"
            };

            scrr.Children.Add(scrs);

            // write SCRH data
            using (var f = new MemoryStream(0x10))
            {
                f.Write(0x1);
                f.Write(Scripts.Count);
                f.Write(0xCDCDCDCD);
                f.Write(0x2);

                scrh.SetBuffer(f.ToArray());
            }

            var lookupSize = (Scripts.Count * 0xC);
            var curOffset = 0;

            // write SCRC data
            using (var f = new MemoryStream(lookupSize))
            {
                foreach (var script in Scripts)
                {
                    f.Write(curOffset);
                    f.Write(script.Checksum);
                    f.Write(0xCDCDCDCD);

                    curOffset += script.Buffer.Length;
                }

                scrc.SetBuffer(f.ToArray());
            }

            // write SCRS data
            using (var f = new MemoryStream(curOffset))
            {
                foreach (var script in Scripts)
                {
                    f.Write(script.Buffer);
                }

                scrs.SetBuffer(f.ToArray());
            }

            Content.Children.Clear();
            Content.Children.Add(scrr);
            
            base.OnFileSaveBegin();
        }

        public ScriptPackageFile() { }
        public ScriptPackageFile(string filename)
        {
            Load(filename);
        }
    }

    class Program
    {
        static readonly string DefaultOutput = @".\Compiled\";
        
        static string InputFile { get; set; }
        static string OutputDir { get; set; }

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                bool error = false;
                bool repack = false;

                foreach (var arg in args)
                {
                    if (arg.StartsWith("/") || arg.StartsWith("-"))
                    {
                        switch (arg.ToLower().TrimStart('/', '-'))
                        {
                        case "repack":
                            repack = true;
                            continue;
                        default:
                            Console.WriteLine("WARNING: Unknown argument '{0}'", arg);
                            continue;
                        }
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(InputFile))
                            InputFile = arg;
                        else if (String.IsNullOrEmpty(OutputDir))
                            OutputDir = arg;
                        else
                        {
                            Console.WriteLine("ERROR: Too many arguments specified.");
                            error = true;
                            break;
                        }
                    }
                }

                if (error)
                {
                    Console.WriteLine("Terminating....");
                    return;
                }

                if (!String.IsNullOrEmpty(InputFile))
                {
                    if (!repack)
                    {
                        // Unpack an fchunk
                        if (File.Exists(InputFile))
                        {
                            var scriptFile = new ScriptPackageFile(InputFile);
                        }
                        else
                        {
                            Console.WriteLine("ERROR: The specified file does not exist.");
                        }
                    }
                    else
                    {
                        // repack an fchunk
                        if (File.Exists(InputFile))
                        {
                            if (String.IsNullOrEmpty(OutputDir))
                                OutputDir = Path.Combine(Path.GetFullPath(DefaultOutput), Path.GetFileNameWithoutExtension(InputFile));

                            if (!Directory.Exists(OutputDir))
                                Directory.CreateDirectory(OutputDir);

                            using (var f = File.Open(InputFile, FileMode.Open, FileAccess.Read))
                            {
                                if (f.ReadInt32() != 0x35395453)
                                {
                                    Console.WriteLine("Invalid lookup table file.");
                                    return;
                                }

                                var count = f.ReadInt32();
                                var offset = f.ReadInt32();

                                if (f.ReadInt32() != 0x2)
                                {
                                    Console.WriteLine("Invalid lookup table file.");
                                    return;
                                }

                                f.Position = offset;

                                var scripts = new List<ScriptData>(count);

                                for (int i = 0; i < count; i++)
                                {
                                    var checksum = f.ReadUInt32();
                                    var filename = f.ReadString();

                                    f.Position += 1;

                                    var script = new ScriptData() {
                                        Checksum = checksum,
                                        Filename = filename
                                    };

                                    scripts.Add(script);

                                    var dir = String.Format(@"{0}\Resources\{1}", Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(InputFile));

                                    var luaFile = String.Format(@"{0}\{1}", dir, filename);

                                    if (!File.Exists(luaFile))
                                    {
                                        Console.WriteLine("ERROR: File '{0}' was not found!!!", luaFile);
                                        return;
                                    }

                                    var tmpFile = Path.GetTempFileName();

                                    var cmd = new System.Diagnostics.Process() {
                                        StartInfo = new System.Diagnostics.ProcessStartInfo() {
                                            FileName                = "cmd.exe",
                                            Arguments               = String.Format("/c start \"\" /b /d \"{0}\" luac -o \"{1}\" \"{2}\" ",
                                                Environment.CurrentDirectory, tmpFile, luaFile),
                                            WorkingDirectory        = Environment.CurrentDirectory,
                                            RedirectStandardError   = true,
                                            RedirectStandardInput   = true,
                                            RedirectStandardOutput  = true,
                                            UseShellExecute         = false,
                                            WindowStyle             = System.Diagnostics.ProcessWindowStyle.Hidden
                                        }
                                    };

                                    cmd.Start();

                                    var output = cmd.StandardOutput;
                                    var stdError = cmd.StandardError;

                                    while (!output.EndOfStream)
                                        Console.WriteLine(output.ReadLine());
                                    while (!stdError.EndOfStream)
                                        Console.WriteLine(stdError.ReadLine());

                                    cmd.WaitForExit();

                                    script.Buffer = File.ReadAllBytes(tmpFile);

                                    File.Delete(tmpFile);
                                }

                                var scriptFile = new ScriptPackageFile();

                                scriptFile.Scripts = scripts;
                                scriptFile.Save(String.Format(@"{0}\{1}.fchunk", OutputDir, Path.GetFileNameWithoutExtension(InputFile)));
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: No input file was specified.");
                }
            }
            else
            {
                Console.WriteLine(
@"Usage: LuaSF --[options] [input file] [:output folder]

If no output folder is specified, a default folder is used.
This folder is usually located where LuaSF resides.

Options:
    --repack    Compile the specified file (usually a .slk file)");
            }
        }
    }
}
