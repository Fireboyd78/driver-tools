using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IMGRipper
{
    enum IMGType
    {
        IMG2 = 0x32474D49,
        IMG3 = 0x33474D49,
        IMG4 = 0x34474D49
    }

    sealed class IMGFile
    {
        public class DataEntry
        {
            public int Offset { get; set; }

            public long FileOffset
            {
                get { return ((long)Offset * 2048L); }
            }

            public uint Size { get; set; }

            public string Name { get; set; }

            public bool HasFileName { get; set; }
        }

        private byte[] Buffer { get; set; }

        public List<DataEntry> Entries { get; set; }

        public IMGType Type { get; set; }

        public uint Reserved { get; set; }

        private FileInfo FileInfo { get; set; }

        private static readonly Dictionary<uint, string> LookupTable =
            new Dictionary<uint, string>();

        public string DirectoryName
        {
            get { return FileInfo.DirectoryName; }
        }

        public string FileName
        {
            get { return FileInfo.FullName; }
        }

        public string OutputDirectory { get; set; }

        private void VerifyOutput()
        {
            if (!Directory.Exists(OutputDirectory))
                Directory.CreateDirectory(OutputDirectory);
        }

        public void Unpack()
        {
            VerifyOutput();

            int maxSize = 0x2C000000;

            using (var fs = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (FileInfo.Length < 28672)
                {
                    Console.WriteLine("Sorry - XBox version is not supported (yet).");
                    return;
                }

                Console.WriteLine("Unpacking files...");

                int i = 1;

                var nSkip = 0;

                if (!Program.VerboseLog)
                    Console.Write("Progress: ");

                Console.SetBufferSize(Console.BufferWidth, 2500);

                var cL = Console.CursorLeft;
                var cT = Console.CursorTop;

                foreach (var entry in Entries)
                {
                    if (!Program.VerboseLog)
                    {
                        Console.SetCursorPosition(cL, cT);
                        Console.Write("{0} / {1}", i, Entries.Count);
                    }

                    fs.Seek(entry.FileOffset, SeekOrigin.Begin);

                    string name = "";
                    long length = entry.Size;

                    //bool skip = false;
                    
                    if (!entry.HasFileName)
                    {
                        string ext = "bin";

                        var magic32 = fs.PeekUInt32();
                        var magic16 = (magic32 & 0xFFFF);

                        if (magic16 != 0xFEFF)
                        {
                            if (LookupTable.ContainsKey(magic32))
                                ext = LookupTable[magic32];
                        }
                        else
                        {
                            // assume unicode text file
                            ext = "txt";
                        }

                        /*
                        switch (magic16)
                        {
                        case 0x5048:
                            ext = "ab3";
                            break;
                        case 0x3130:
                            {
                                if (magic32 == 0x4D4D3130)
                                    ext = "dat";
                                else
                                    ext = "gsd";
                            } break;
                        case 0x3465:
                            ext = "xa";
                            break;
                        case 0x5852:
                            ext = "xmv";
                            skip = true;
                            break;
                        case 0x4843:
                            ext = "chunk";
                            break;
                        case 0x4B53:
                            {
                                // hackish way of finding mood files
                                // if it doesn't begin with "SKYDOME_NAME" this won't work
                                if (magic32 == 0x44594B53)
                                    ext = "txt";
                            } break;
                        case 0xFEFF:
                            ext = "txt";
                            break;
                        }
                        */

                        if (ext == "bin")
                        {
                            var holdPos = fs.Position;

                            // check if text file
                            fs.Seek(length - 1, SeekOrigin.Current);

                            if (fs.ReadByte() == 0xA)
                                ext = "txt";

                            fs.Position = holdPos;
                        }

                        //name = String.Format("{0:D4}_{1}.{2}", i, entry.Name, ext);
                        name = String.Format("{0}.{1}", entry.Name, ext);

                        if (Program.NoFMV && (ext == "xmv"))
                        {
                            Program.WriteVerbose("{0}: SKIPPING -> {1}", i++, name);
                            nSkip++;

                            continue;
                        }
                    }
                    else
                    {
                        name = entry.Name;

                        var nDir = Path.GetDirectoryName(name);

                        if (Path.GetExtension(name).ToLower() == ".xav")
                        {
                            Program.WriteVerbose("{0}: SKIPPING -> {1}", i++, name);
                            nSkip++;

                            continue;
                        }

                        if (!String.IsNullOrEmpty(nDir))
                        {
                            var eDir = Path.Combine(OutputDirectory, Path.GetDirectoryName(name));

                            if (!Directory.Exists(eDir))
                                Directory.CreateDirectory(eDir);
                        }
                    }

                    var filename = Path.Combine(OutputDirectory, name);

                    Program.WriteVerbose("{0}: {1}", i++, name);

                    if (length < maxSize)
                    {
                        var buffer = new byte[length];
                        fs.Read(buffer, 0, buffer.Length);

                        File.WriteAllBytes(filename, buffer);
                    }
                    else if (length > maxSize)
                    {
                        int splitSize = (int)(length - maxSize);

                        using (var file = File.Create(filename))
                        {
                            for (int w = 0; w < maxSize; w += 0x100000)
                                file.Write(fs.ReadBytes(0x100000));

                            file.Write(fs.ReadBytes(splitSize));
                        }
                    }

                    GC.Collect();
                }

                Console.WriteLine((!Program.VerboseLog) ? "\r\n" : "");
                Console.WriteLine("Unpacked {0} files.", (Entries.Count - nSkip));

                if (nSkip > 0)
                    Console.WriteLine("{0} files skipped.", nSkip);
            }
        }

    #if DEBUG
        public void SaveTableBinary()
        {
            bool img2 = (Type == IMGType.IMG2);
            bool img4 = (Type == IMGType.IMG4);

            int len = (!img2) ? 0x10 : 0xC;

            var buffer = new byte[Buffer.Length + len];

            using (var ms = new MemoryStream(buffer))
            {
                ms.Write((int)Type);
                ms.Write(Entries.Count);
                ms.Write(Reserved);

                if (!img2)
                    ms.Write(Buffer.Length);
            }

            System.Buffer.BlockCopy(Buffer, 0, buffer, len, Buffer.Length);

            var bufferTest = new byte[buffer.Length / 4];

            if (img4)
            {
                for (int i = 0; i < bufferTest.Length / 4; i++)
                    System.Buffer.BlockCopy(buffer, (len + (i * 12)), bufferTest, (i * 4), 4);
            }

            //var path = Path.Combine(Environment.CurrentDirectory, Path.GetFileName(Path.ChangeExtension(FileName, ".dir")));

            var path = Path.Combine(OutputDirectory, Path.GetFileName(Path.ChangeExtension(FileName, ".dir")));
            var testPath = Path.ChangeExtension(path, ".dir.test");

            File.WriteAllBytes(path, buffer);

            if (img4)
                File.WriteAllBytes(testPath, bufferTest);
        }
    #endif

        public void SaveTableASCII()
        {
            bool img4 = (Type == IMGType.IMG4);

            var sb = new StringBuilder();

            if (!img4)
                sb.AppendLine("ID----Offset------Size--------Filename----------------------------------------");
            else
                sb.AppendLine("ID----Offset------Size--------Filename Hash (?)-------------------------------");

            int count = Entries.Count;

            for (int i = 0; i < count; i++)
            {
                var entry = Entries[i];

                sb.AppendFormat("{0:D4}  ", (i + 1)); // ID
                sb.AppendFormat("0x{0:X8}  ", entry.FileOffset); // Offset
                sb.AppendFormat("0x{0:X8}  ", entry.Size); // Size
                sb.AppendFormat("{0}", entry.Name); // Filename

                if (i < count - 1)
                    sb.AppendLine();
            }

            //var path = Path.Combine(Environment.CurrentDirectory, Path.GetFileName(Path.ChangeExtension(FileName, ".dir.txt")));

            var path = Path.Combine(OutputDirectory, Path.GetFileName(Path.ChangeExtension(FileName, ".dir.txt")));

            File.WriteAllText(path, sb.ToString());
        }

        private unsafe void Decrypt()
        {
            using (var fs = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int magic = fs.ReadInt32();
                int version = (magic >> 24) & 0xF;

                if (((magic & 0xFFFFFF) == 0x474D49) && (version > 1 && version <= 4))
                    Type = (IMGType)magic;
                else
                    throw new Exception("Cannot read IMG file - bad magic!");

                bool img2 = (Type == IMGType.IMG2);
                bool img3 = (Type == IMGType.IMG3);
                bool img4 = (Type == IMGType.IMG4);

                int count = fs.ReadInt32();
                
                Reserved = fs.ReadUInt32();

                int size = (!img2) ? fs.ReadInt32() : (count * 0x38);

                Buffer = new byte[size];

                fs.Read(Buffer, 0, size);

                int offset  = 0;

                byte decKey  = 27;
                byte key     = (byte)((!img2) ? 21 : 11);

                Entries = new List<DataEntry>(count);

                fixed (byte* p = Buffer)
                {
                    for (int i = 0; i < size; i++)
                    {
                        (*(byte*)(p + i)) -= decKey;

                        decKey += key;

                        if (!img2)
                        {
                            ++offset;

                            if (offset > 6 && key == 21)
                            {
                                offset = 0;
                                key = 11;
                            }
                            if (key == 11 && offset > 24)
                            {
                                offset = 0;
                                key = 21;
                            }
                        }
                    }

                    byte* ptr = p;

                    for (int i = 0; i < count; i++)
                    {
                        var entry = new DataEntry() {
                            HasFileName = (!img4) ? true : false
                        };

                        ptr = (p + (i * ((!img2) ? 0xC : 0x38)));

                        if (img2)
                        {
                            entry.Name = new string((sbyte*)ptr);
                            ptr += 0x2C;
                        }
                        else
                        {
                            entry.Name = (img3) ? new string((sbyte*)(p + (*(int*)ptr))) : (*(uint*)ptr).ToString();
                        }

                        entry.Offset = (*(int*)(ptr + 4));
                        entry.Size = (*(uint*)(ptr + 8));

                        Entries.Add(entry);
                    }
                }
            }
        }

        public static bool LoadLookupTable(string lookupTable)
        {
            if (!File.Exists(lookupTable))
                Console.WriteLine("WARNING: The lookup table was not found. All files will have the extension '.bin'!");

            using (var sr = new StreamReader(File.Open(lookupTable, FileMode.Open, FileAccess.Read)))
            {
                var splitStr = new[] { "0x", "[", "]", "=", "\""};

                if (sr.ReadLine() == "# Magic number lookup file")
                {
                    int lineNum = 1;
                    string line = "";

                    while (!sr.EndOfStream)
                    {
                        ++lineNum;

                        if (String.IsNullOrEmpty((line = sr.ReadLine())))
                            continue;

                        // Skip comments
                        if (line.StartsWith("#!"))
                        {
                            // multi-line comments
                            while (!sr.EndOfStream)
                            {
                                line = sr.ReadLine();

                                ++lineNum;

                                if (line.StartsWith("!#"))
                                    break;
                            }

                            continue;
                        }
                        else if (line.StartsWith("#"))
                            continue;

                        var strAry = line.Split(splitStr, StringSplitOptions.RemoveEmptyEntries);

                        // key, val, comment
                        if (strAry.Length < 2)
                        {
                            Console.WriteLine("ERROR: An error occurred while parsing the lookup table.");
                            Console.WriteLine("Line {0}: {1}", lineNum, line);
                            return false;
                        }

                        uint key = 0;
                        string val = strAry[1];

                        // add little-endian lookup
                        if (line.StartsWith("0x", true, null))
                        {
                            key = uint.Parse(strAry[0], NumberStyles.AllowHexSpecifier);
                        }
                        else if (line.StartsWith("["))
                        {
                            key = BitConverter.ToUInt32(Encoding.UTF8.GetBytes(strAry[0]), 0);
                        }
                        else
                        {
                            Console.WriteLine("ERROR: An error occurred while parsing the lookup table.");
                            Console.WriteLine("Line {0}: {1}", lineNum, line);
                            return false;
                        }

                        if (!LookupTable.ContainsKey(key))
                        {
                            Program.WriteVerbose("Adding [0x{0:X8}, {1}] to lookup table.", key, val);
                            LookupTable.Add(key, val);
                        }
                        else
                        {
                            Console.WriteLine("WARNING: Duplicate entry in lookup table. Skipping.");
                            Console.WriteLine("Line {0}: {1}\r\n", lineNum, line);
                        }
                    }

                    return true;
                }
                else
                {
                    Console.WriteLine("ERROR: The specified lookup table cannot be used.");
                    return false;
                }
            }
        }

        public static void Unpack(string inputFile, string outputDir)
        {
            Unpack(inputFile, outputDir, true);
        }

        public static void Unpack(string path, string outputDir, bool exportTables)
        {
            var img = new IMGFile(path) {
                OutputDirectory = outputDir
            };

            img.VerifyOutput();

            if (exportTables)
            {
            #if DEBUG
                img.SaveTableBinary();
            #endif
                img.SaveTableASCII();
            }

            img.Unpack();
        }

        public IMGFile(string path)
        {
            FileInfo = new FileInfo(path);

            Decrypt();
        }
    }
}
