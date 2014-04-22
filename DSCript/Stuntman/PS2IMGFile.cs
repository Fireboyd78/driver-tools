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

namespace DSCript
{
    public enum IMGType
    {
        IMG2 = 0x32474D49,
        IMG3 = 0x33474D49,
        IMG4 = 0x34474D49
    }

    public sealed class PS2IMG
    {
        public sealed class DataEntry
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

        public string DirectoryName
        {
            get { return FileInfo.DirectoryName; }
        }

        public string FileName
        {
            get { return FileInfo.FullName; }
        }

        public void Unpack()
        {
            var dir = Path.Combine(DirectoryName, "Unpack");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            int maxSize = 0x2C000000;

            using (var fs = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                DSC.Log("Unpacking {0} files...", Entries.Count);

                int i = 1;

                foreach (var entry in Entries)
                {
                    fs.Seek(entry.FileOffset, SeekOrigin.Begin);

                    string name = "";

                    bool skip = false;
                    
                    if (!entry.HasFileName)
                    {
                        string ext = "bin";

                        var magic = fs.PeekUInt32();

                        switch (fs.PeekUInt16())
                        {
                        case 0x3130:
                            {
                                if (fs.PeekInt32() == 0x4D4D3130)
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
                        case 0xFEFF:
                            ext = "txt";
                            break;
                        }

                        name = String.Format("{0}.{1}", entry.Name, ext);

                        if (skip)
                        {
                            DSC.Log("{0}: SKIPPING -> {1}", i++, name);
                            continue;
                        }
                    }
                    else
                    {
                        name = entry.Name;

                        var nDir = Path.GetDirectoryName(entry.Name);

                        if (!String.IsNullOrEmpty(nDir))
                        {
                            var eDir = Path.Combine(dir, Path.GetDirectoryName(entry.Name));

                            if (!Directory.Exists(eDir))
                                Directory.CreateDirectory(eDir);
                        }
                    }

                    var filename = Path.Combine(dir, name);

                    long length = entry.Size;

                    if (length < maxSize)
                    {
                        var buffer = new byte[length];
                        fs.Read(buffer, 0, buffer.Length);

                        if (buffer[length - 1] == 0xA)
                            filename = Path.ChangeExtension(filename, ".txt");

                        DSC.Log("{0}: {1}", i++, filename);
                        
                        File.WriteAllBytes(filename, buffer);
                    }
                    else if (length > maxSize)
                    {
                        int splitSize = (int)(length - maxSize);

                        DSC.Log("{0}: {1}", i++, filename);

                        using (var file = File.Create(filename))
                        {
                            for (int w = 0; w < maxSize; w += 0x100000)
                                file.Write(fs.ReadBytes(0x100000));

                            file.Write(fs.ReadBytes(splitSize));
                        }
                    }

                    GC.Collect();
                }

                DSC.Log("Finished!");
            }
        }

        public void SaveTableBinary()
        {
            bool img2 = (Type == IMGType.IMG2);
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

            File.WriteAllBytes(Path.ChangeExtension(FileName, ".dir"), buffer);
        }

        public void SaveTableASCII()
        {
            bool img2 = (Type == IMGType.IMG2);
            bool img3 = (Type == IMGType.IMG3);
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
            
            var path = Path.ChangeExtension(FileName, ".dir.txt");

            File.WriteAllText(path, sb.ToString());
        }

        private unsafe void Decrypt()
        {
            using (var fs = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int magic = fs.ReadInt32();
                int version = (magic >> 24) & 0xF;

                if (version > 1 && version <= 4)
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
                            ptr += 30;
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

        public static void Unpack(string path)
        {
            Unpack(path, true);
        }

        public static void Unpack(string path, bool exportTables)
        {
            var img = new PS2IMG(path);

            if (exportTables)
            {
                img.SaveTableBinary();
                img.SaveTableASCII();
            }

            img.Unpack();
        }

        public PS2IMG(string path)
        {
            FileInfo = new FileInfo(path);

            Decrypt();
        }
    }
}
