using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace LuaSF
{
    sealed class ScriptPackageFile : FileChunker
    {
        static readonly string COMPrefix = "COM:luaScripts";
        static readonly char[] DirectorySeparators = { '/', '\\' };

        public bool BigEndian { get; set; }

        public int Version { get; set; } = 1;
        
        // 2 = Compiled?
        // 4 = Big-Endian?
        // PC = 2, 360 = 6
        public int Flags { get; set; } = 2;
        
        public List<CompiledScript> Scripts { get; set; }

        public CompiledScript GetCompiledScript(string filename)
        {
            // hash is lowercase'd filename
            var crc = CRC32Hasher.GetHash(filename.ToLower());

            return Scripts.FirstOrDefault((s) => s.Hash == crc);
        }
        
        public string GetScriptFilePath(string filePath)
        {
            // paths are stripped, but NOT resolved
            while (filePath.StartsWith(".") || filePath.StartsWith("\\"))
                filePath = filePath.Substring(1);

            // sanitize path
            filePath = String.Join(@"\", filePath.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries));

            return filePath;
        }

        public string GetCOMFilePath(string filePath, bool sanitizePath = false)
        {
            if (sanitizePath)
                filePath = GetScriptFilePath(filePath);
            
            return Path.Combine(COMPrefix, filePath);
        }
        
        protected override void OnSpoolerLoaded(Spooler sender, EventArgs e)
        {
            if (sender.Context == ChunkType.ScriptPackageRoot)
            {
                var scrr = sender as SpoolablePackage;

                var scrh = scrr.GetFirstChild<SpoolableBuffer>(ChunkType.ScriptPackageHeader);
                var scrc = scrr.GetFirstChild<SpoolableBuffer>(ChunkType.ScriptPackageLookup);
                var scrs = scrr.GetFirstChild<SpoolableBuffer>(ChunkType.ScriptPackageCompiledScript);

                var nScripts = 0;

                // read header
                using (var f = scrh.GetMemoryStream())
                {
                    Version = f.ReadInt32();

                    if (Version != 1)
                    {
                        if ((Version >> 24) == 1)
                            throw new InvalidOperationException("Big-Endian scripts detected! Restart with '--big' argument specified.");
                        else
                            throw new InvalidOperationException($"Invalid version ({Version}) - cannot load Script Package Header!");
                    }

                    nScripts = f.ReadInt32();

                    // we don't care about the checksum
                    f.Position += 0x4;

                    Flags = f.ReadInt32();
                }

                var lookupTable = new List<int>();

                // get lookup data
                using (var f = scrc.GetMemoryStream())
                {
                    // initialize scripts list
                    Scripts = new List<CompiledScript>(nScripts);

                    for (int i = 0; i < nScripts; i++)
                    {
                        f.Position = (i * 0xC);

                        var offset = f.ReadInt32();
                        
                        // add offset to lookup
                        lookupTable.Add(offset);

                        Scripts.Add(new CompiledScript() {
                            Hash = f.ReadInt32(),
                        });
                    }
                }

                // add the buffer size so we don't need weird hacks
                lookupTable.Add(scrs.Size);

                // this should NEVER happen!
                if (nScripts >= lookupTable.Count)
                    throw new InvalidOperationException("A fatal error occurred while building the lookup table!");
                
                // get script data
                using (var f = scrs.GetMemoryStream())
                {
                    for (int i = 0; i < nScripts; i++)
                    {
                        // NOTE: lookupTable is guaranteed to be nScripts + 1!
                        var offset = lookupTable[i];
                        var nextOffset = lookupTable[i + 1];

                        var size = (nextOffset - offset);

                        // allocate and read script buffer
                        var buffer = new byte[size];

                        f.Position = offset;
                        f.Read(buffer, 0, size);
                        
                        Scripts[i].Buffer = buffer;
                    }
                }

                // verify script data
                foreach (var script in Scripts)
                {
                    using (var fB = (BigEndian)
                        ? new BigEndianMemoryStream(script.Buffer)
                        : new MemoryStream(script.Buffer))
                    {
                        if (BigEndian)
                        {
                            fB.Position = 0x6;

                            var endianness = fB.ReadByte();

                            if (endianness == 1)
                            {
                                // why the $*#% did someone put little-endian for big endian scripts?!
                                // fix it!
                                fB.Position = 0x6;
                                fB.WriteByte(0);
                            }
                        }
                        
                        // we'll use the debug information to get the filename
                        fB.Position = 0xC;

                        var strLen = fB.ReadInt32();

                        // missing debug symbols?
                        if (strLen == 0)
                        {
                            // we can't get the filename :(
                            continue;
                        }

                        // length includes null-terminator, but we don't need it
                        var filename = fB.ReadString(strLen - 1);

                        // root directory denoted by extra parenthesis
                        //   @r:\resources\luascripts\common\\<filename>.lua
                        //-----------------------------------^ here

                        var rootIdx = filename.IndexOf(@"\\");
                        
                        if (rootIdx == -1)
                        {
                            // time for some brute forcing
                            filename = filename.Substring(1);
                            
                            var dirInfo = new DirectoryInfo(Path.GetDirectoryName(filename));
                            var filePath = Path.GetFileName(filename);

                            var resolved = false;

                            do
                            {
                                var comPath = GetCOMFilePath(filePath).ToLower();
                                var hash = CRC32Hasher.GetHash(comPath);

                                if (hash == script.Hash)
                                {
                                    resolved = true;
                                    break;
                                }

                                // continue going through directories?
                                // NOTE: this is the only way to terminate!
                                if (dirInfo == null)
                                    break;

                                filePath = Path.Combine(dirInfo.Name, filePath);

                                dirInfo = dirInfo.Parent;
                            } while (true);

                            script.Filename = (resolved) ? filePath : filename;
                        }
                        else
                        {
                            // skip parenthesis'
                            script.Filename = filename.Substring(rootIdx + 2);
                        }
                    }
                }

                Console.WriteLine($"Successfully loaded file: {FileName}");
                Console.WriteLine();
            }
        }

        protected override void OnFileSaveBegin()
        {
            if (BigEndian)
                throw new InvalidOperationException("Big-Endian scripts cannot be compiled at this time!");

            var scrr = new SpoolablePackage() {
                Alignment   = SpoolerAlignment.Align2048,
                Context     = ChunkType.ScriptPackageRoot,
                Version     = 0,
                Description = "Script Package Root"
            };

            var scrh = new SpoolableBuffer() {
                Alignment   = SpoolerAlignment.Align2048,
                Context     = ChunkType.ScriptPackageHeader,
                Version     = 1,
                Description = "Script Package Header"
            };

            scrr.Children.Add(scrh);

            var scrc = new SpoolableBuffer() {
                Alignment   = SpoolerAlignment.Align2048,
                Context     = ChunkType.ScriptPackageLookup,
                Version     = 0,
                Description = "Script Package Lookup"
            };

            scrr.Children.Add(scrc);

            var scrs = new SpoolableBuffer() {
                Alignment   = SpoolerAlignment.Align2048,
                Context     = ChunkType.ScriptPackageCompiledScript,
                Version     = 0,
                Description = "Script Package Compiled Script"
            };

            scrr.Children.Add(scrs);

            // write SCRH data
            using (var f = new MemoryStream(0x10))
            {
                f.Write(Version);
                f.Write(Scripts.Count);
                f.Write(0x99999999);
                f.Write(Flags);

                scrh.SetBuffer(f.ToArray());
            }

            var lookupSize = (Scripts.Count * 0xC);
            var offset = 0;

            // write SCRC data
            using (var f = new MemoryStream(lookupSize))
            {
                foreach (var script in Scripts)
                {
                    f.Write(offset);
                    f.Write(script.Hash);
                    f.Write(0x99999999);

                    offset += script.Buffer.Length;
                }

                scrc.SetBuffer(f.ToArray());
            }

            // write SCRS data
            using (var f = new MemoryStream(offset))
            {
                foreach (var script in Scripts)
                    f.Write(script.Buffer);

                scrs.SetBuffer(f.ToArray());
            }

            Content.Children.Clear();
            Content.Children.Add(scrr);

            base.OnFileSaveBegin();
        }

        public ScriptPackageFile() { }
        public ScriptPackageFile(string filename, bool bigEndian = false)
        {
            // set endianness before proceeding
            BigEndian = bigEndian;
            HACK_BigEndian = bigEndian;

            Load(filename);
        }
    }
}
