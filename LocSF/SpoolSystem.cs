using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace LocSF
{
    public interface ISpoolSystemBlock
    {
        void LoadASCII(string buffer);
        void LoadBinary(byte[] buffer);

        string ToASCII();
        byte[] ToBinary();
    }

    public class SpoolSystem
    {
        public class LookupEntry : ISpoolSystemBlock
        {
            public int Id { get; set; }
            public int Index { get; set; }

            public string FileName { get; set; }
            public string Description { get; set; }

            void ISpoolSystemBlock.LoadASCII(string buffer)
            {
                var name_begin = buffer.IndexOf('"') + 1;
                var name_end = buffer.Substring(name_begin).IndexOf('"') - 1;

                var desc_begin = buffer.Substring(name_end + 1).IndexOf('"') + 1;
                var desc_end = buffer.Substring(name_begin).IndexOf('"') - 1;

                FileName = buffer.Substring(name_begin, name_end);
                Description = buffer.Substring(desc_begin, desc_end);

                var vals = buffer.Substring(0, name_begin - 1).Split(' ');

                Id = int.Parse(vals[0], NumberStyles.Integer);
                Index = int.Parse(vals[1], NumberStyles.Integer);
            }

            void ISpoolSystemBlock.LoadBinary(byte[] buffer)
            {
                if (buffer.Length != 6)
                    throw new InvalidOperationException("Cannot load binary lookup entry -- invalid size!");

                Id = BitConverter.ToInt16(buffer, 0);
                Index = BitConverter.ToInt32(buffer, 2);
            }

            string ISpoolSystemBlock.ToASCII()
            {
                return $"{Id} {Index} \"{FileName}\" \"{Description}\"";
            }

            byte[] ISpoolSystemBlock.ToBinary()
            {
                using (var ms = new MemoryStream(6))
                {
                    ms.Write((short)Id);
                    ms.Write(Index);

                    return ms.ToArray();
                }
            }
        }

        public class LookupData : ISpoolSystemBlock
        {
            public int LookupType { get; set; }

            public List<LookupEntry> Entries { get; set; } = new List<LookupEntry>();

            public int Field1 { get; set; }
            public int Field2 { get; set; }
            public int Field3 { get; set; }

            public int Field4 { get; set; }
            public int Field5 { get; set; }

            public int Reserved { get; set; }

            public bool IsValid
            {
                get { return (Entries != null && Entries.Count > 0); }
            }

            private int ParseInt(string str)
            {
                var isHex = (str.StartsWith("0x"));
                var val = (isHex) ? str.Substring(2) : str;

                return int.Parse(val, (isHex) ? NumberStyles.HexNumber : NumberStyles.Integer);
            }

            void ISpoolSystemBlock.LoadASCII(string buffer)
            {
                using (var sr = new StringReader(buffer))
                {
                    var _readLine = new Func<string>(() => {
                        return sr.ReadLine()?.TrimStart(' ', '\t');
                    });

                    var top = _readLine().Split(':');

                    if (top.Length != 2 || !top[1].EndsWith("("))
                        throw new InvalidOperationException("Cannot load ASCII lookup data -- invalid lookup table!");

                    LookupType = int.Parse(top[0]);

                    string line = "";

                    while ((line = _readLine()) != null && !line.StartsWith(")"))
                    {
                        var entry = new LookupEntry();

                        ((ISpoolSystemBlock)entry).LoadASCII(line);

                        Entries.Add(entry);
                    }

                    Field1 = ParseInt(_readLine());
                    Field2 = ParseInt(_readLine());
                    Field3 = ParseInt(_readLine());

                    Field4 = ParseInt(_readLine());
                    Field5 = ParseInt(_readLine());

                    Reserved = ParseInt(_readLine());
                }
            }

            void ISpoolSystemBlock.LoadBinary(byte[] buffer)
            {
                using (var ms = new MemoryStream(buffer))
                {
                    LookupType = ms.ReadInt16();

                    var nEntries = ms.ReadInt16();

                    Field1 = ms.ReadInt32();
                    Field2 = ms.ReadInt32();
                    Field3 = ms.ReadInt32();

                    Field4 = ms.ReadInt32();
                    Field5 = ms.ReadInt32();

                    Reserved = ms.ReadInt32();

                    if (nEntries > 0)
                    {
                        for (int i = 0; i < nEntries; i++)
                        {
                            var entry = new LookupEntry();

                            ((ISpoolSystemBlock)entry).LoadBinary(ms.ReadBytes(6));

                            Entries.Add(entry);
                        }
                    }
                }
            }

            string ISpoolSystemBlock.ToASCII()
            {
                var sb = new StringBuilder();

                sb.AppendLine("SSLP:1");
                sb.AppendLine("{");

                sb.AppendLine($"\t{LookupType} : (");

                if (IsValid)
                {
                    foreach (var entry in Entries)
                        sb.Append("\t\t").AppendLine(((ISpoolSystemBlock)entry).ToASCII());
                }
                else
                {
                    sb.AppendLine("\t\t# empty");
                }

                sb.AppendLine("\t)");

                sb.AppendLine($"\t0x{Field1:X}");
                sb.AppendLine($"\t0x{Field2:X}");
                sb.AppendLine($"\t0x{Field3:X}");
                sb.AppendLine($"\t0x{Field4:X}");
                sb.AppendLine($"\t0x{Field5:X}");

                sb.AppendLine($"\t0x{Reserved:X}");

                sb.AppendLine("}");

                return sb.ToString();
            }

            byte[] ISpoolSystemBlock.ToBinary()
            {
                var bufferSize = 0x1C;

                if (IsValid)
                    bufferSize += (Entries.Count * 6);

                using (var ms = new MemoryStream(bufferSize))
                {
                    ms.Write((short)LookupType);
                    ms.Write((short)Entries.Count);
                    
                    ms.Write(Field1);
                    ms.Write(Field2);
                    ms.Write(Field3);

                    ms.Write(Field4);
                    ms.Write(Field5);

                    ms.Write(Reserved);

                    if (IsValid)
                    {
                        foreach (var entry in Entries)
                            ms.Write(((ISpoolSystemBlock)entry).ToBinary());
                    }

                    return ms.ToArray();
                }
            }
        }

        static readonly SpoolerContext SpoolSystemInitChunker = "SSIC";
        static readonly SpoolerContext SpoolSystemLookup = "SSLP";

        LookupData SpoolLookup { get; set; }

        public void Load(string filename)
        {
            using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read))
            {
                if ((SpoolerContext)fs.ReadInt32() == SpoolSystemInitChunker)
                {
                    fs.Position = 0;

                    using (var sr = new StreamReader(fs))
                    {
                        var _readLine = new Func<string>(() => {
                            return sr.ReadLine()?.TrimStart(' ', '\t');
                        });

                        var _readBlock = new Func<Tuple<string,int,string>>(() => {
                            var top = _readLine().Split(':');

                            if (top.Length != 2)
                                throw new InvalidOperationException("Cannot load SpoolSystem file -- bad block!");

                            var block_type = top[0];
                            var block_version = top[1];

                            if (block_type.Length > 4)
                                throw new InvalidOperationException("Cannot load SpoolSystem file -- invalid block type!");

                            var sb = new StringBuilder();

                            string l = "";
                            int depth = 0;

                            long start = sr.BaseStream.Position;

                            while ((l = _readLine()) != null)
                            {
                                if (l == "{")
                                {
                                    if (depth == 0)
                                        start = sr.BaseStream.Position;

                                    depth++;
                                    continue;
                                }

                                if (l == "}")
                                {
                                    depth--;

                                    if (depth == 0)
                                    {
                                        // move back to beginning of block (more nested blocks inside)
                                        sr.BaseStream.Position = start;
                                        break;
                                    }
                                }

                                sb.AppendLine(l);
                            }

                            return new Tuple<string,int,string>(block_type, int.Parse(block_version), sb.ToString());
                        });

                        var chunkerLoaded = false;

                        while (true)
                        {
                            var block = _readBlock();

                            switch (block.Item1)
                            {
                            case "SSIC":
                                {
                                    if (!chunkerLoaded)
                                    {
                                        if (block.Item2 != 1)
                                            throw new InvalidOperationException("Cannot load SpoolSystem file -- invalid version!");
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException("Cannot load SpoolSystem file -- malformed file!");
                                    }
                                } break;
                            case "SSLP":
                                {
                                    if (chunkerLoaded)
                                    {
                                        if (block.Item2 != 1)
                                            throw new InvalidOperationException("Cannot load SpoolSystem file -- bad lookup version!");

                                        SpoolLookup = new LookupData();

                                        ((ISpoolSystemBlock)SpoolLookup).LoadASCII(block.Item3);
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException("Cannot load SpoolSystem file -- malformed file!");
                                    }
                                } break;
                            default:
                                {
                                    Console.WriteLine($"Unknown SpoolSystem block '{block.Item1}:{block.Item2}'");
                                } break;
                            }
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("Cannot load SpoolSystem file -- unsupported file!");
                }
            }
        }

        public void Load(FileChunker chunker)
        {
            var chunk = chunker.Content;

            SpoolablePackage ssic = null;
            SpoolableBuffer sslp = null;
            
            foreach (var s in chunk.Children)
            {
                if (s.Context == SpoolSystemInitChunker && s is SpoolablePackage)
                {
                    ssic = s as SpoolablePackage;

                    if (ssic != null)
                        break;

                    throw new InvalidOperationException("Invalid spool system -- bad init chunker!");
                }
            }

            SpoolablePackage ssic_data = (ssic.Version == 1) ? ssic.GetFirstChild<SpoolablePackage>(ChunkType.NonRendererData) : ssic;

            foreach (var ss in ssic_data.Children)
            {
                if (ss.Context == SpoolSystemLookup && ss is SpoolableBuffer)
                {
                    sslp = ss as SpoolableBuffer;

                    if (sslp != null)
                        break;

                    throw new InvalidOperationException("Invalid spool system -- bad lookup data!");
                }
            }

            SpoolLookup = new LookupData();
            ((ISpoolSystemBlock)SpoolLookup).LoadBinary(sslp.GetBuffer());
        }

        public void Save(string filename)
        {
            var name = Path.GetFileNameWithoutExtension(filename);

            for (int i = 0; i < SpoolLookup.Entries.Count; i++)
            {
                var entry = SpoolLookup.Entries[i];

                if (String.IsNullOrEmpty(entry.FileName))
                    entry.FileName = $"{name}_{i:D4}.dat";
            }

            var sb = new StringBuilder();

            sb.AppendLine("SSIC:1");
            sb.AppendLine("{");

            var buf = ((ISpoolSystemBlock)SpoolLookup).ToASCII();

            using (var sr = new StringReader(buf))
            {
                string line = "";

                while ((line = sr.ReadLine()) != null)
                    sb.Append('\t').AppendLine(line);
            }

            sb.AppendLine("}");


            File.WriteAllText(filename, sb.ToString());
        }
    }
}
