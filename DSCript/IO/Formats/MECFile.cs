using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DSCript.Models;

namespace DSCript.Spooling
{
    internal class MenuPackageData : SpoolableResource<SpoolablePackage>
    {
        protected override void Load()
        {
            throw new NotImplementedException();
        }

        protected override void Save()
        {
            throw new NotImplementedException();
        }
    }

    internal class MECFile : FileChunker
    {
        /*
         foreach (var fi in Directory.GetFiles(@"C:\Dev\Research\Driv3r\Territory\Europe\GUI\backup", "*.MEC"))
                {
                    var mecFile = new SpoolableChunk(fi);
                    var locFile = new LocaleReader(
                        String.Format(@"C:\Dev\Research\Driv3r\Territory\Europe\Locale\English\GUI\{0}.txt", Path.GetFileNameWithoutExtension(fi)));

                    var RMDL = ((SpoolableChunk)mecFile.Spoolers[0]).Spoolers[0] as SpoolableData;

                    var log = new StringBuilder();
                    var logFile = Path.ChangeExtension(fi, "log");

                    var colSize = 20;

                    if (RMDL != null && RMDL.Magic == (int)ChunkType.ReflectionsMenuDataChunk)
                    {
                        log.AppendLine(
@"MEC Reader Log File
File: {0}
----", fi);

                        using (var ms = new MemoryStream(RMDL.Buffer))
                        {
                            var type = ms.ReadInt32();
                            var count = ms.ReadInt32();

                            Debug.Assert((type == 0x191), "Type mismatch!");

                            var unk_08 = ms.ReadInt32(); // 0x0C
                            var unk_0C = ms.ReadInt32(); // 0x54
                            var unk_10 = ms.ReadInt32(); // 0x20
                            var unk_14 = ms.ReadInt32(); // 0x54
                            var unk_18 = ms.ReadInt32(); // 0x14
                            var unk_1C = ms.ReadInt32(); // 0x144
                            var unk_20 = ms.ReadInt32(); // 0x20

                            var readFloats1 = new Action(() => {
                                //for (int ki = 0; ki < (unk_10 / 4); ki++)
                                //    log.AppendLine(ms.ReadSingle().ToString());

                                log.AppendColumn("Position", colSize).AppendLine("{0}, {1}",
                                    ms.ReadSingle(),
                                    ms.ReadSingle());

                                log.AppendColumn("Size", colSize).AppendLine("{0}, {1}",
                                    ms.ReadSingle(),
                                    ms.ReadSingle());

                                log.AppendColumn("Color", colSize).AppendLine("{0:N1}, {1:N1}, {2:N1}, {3:N1}",
                                    ms.ReadSingle(),
                                    ms.ReadSingle(),
                                    ms.ReadSingle(),
                                    ms.ReadSingle()).AppendLine();

                            });

                            var readFloats2 = new Action(() => {
                                log.AppendColumn("Offset", colSize).AppendLine("0x{0:X}", ms.Position).AppendLine();

                                var locId = ms.ReadInt32();

                                log.AppendColumn("LocaleId", colSize).AppendLine("{0}\t; \"{1}\"", locId, locFile[locId]);
                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadInt32());
                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadInt32());

                                log.AppendColumn("Scale X", colSize).AppendLine(ms.ReadSingle());
                                log.AppendColumn("Scale Y", colSize).AppendLine(ms.ReadSingle());

                                var nxt = ms.Position + 0x20;

                                var pUnk1 = ms.ReadString();

                                log.AppendColumn("Unknown", colSize).AppendLine((!String.IsNullOrEmpty(pUnk1)) ? pUnk1 : "<NULL>");

                                ms.Position = nxt;

                                nxt += 0x20;

                                var pUnk2 = ms.ReadString();

                                log.AppendColumn("Unknown", colSize).AppendLine((!String.IsNullOrEmpty(pUnk2)) ? pUnk2 : "<NULL>");

                                ms.Position = nxt;
                            });

                            var readFloats3 = new Action(() => {
                                log.AppendColumn("Offset", colSize).AppendLine("0x{0:X}", ms.Position);

                                //log.AppendLine("Unk1: {0}", ms.ReadSingle());
                                //log.AppendLine("Unk2: {0}", ms.ReadSingle());
                                //log.AppendLine("Unk3: {0}", ms.ReadSingle());
                                //log.AppendLine("Unk4: {0}", ms.ReadSingle());

                                ms.Position += 0x10;

                                log.AppendColumn("TexId", colSize).AppendLine(ms.ReadInt32());
                            });

                            var printHex = new Action<byte[], int, int, int>((bytes, offset, length, stride) => {

                                for (int i = 0; i < length; i += stride)
                                {
                                    // hex column
                                    for (int h = 0; h < stride; h++)
                                    {
                                        var idx = (offset + h + i);

                                        if (idx < length)
                                            log.AppendFormat("{0:X2} ", bytes[idx]);
                                        else
                                            log.Append("   ");
                                    }

                                    log.Append(" ");

                                    // ascii column
                                    for (int d = 0; d < stride; d++)
                                    {
                                        var idx = (offset + d + i);

                                        if (!(idx < bytes.Length))
                                            break;

                                        var val = bytes[idx];

                                        log.Append((val >= 0x20) ? (char)val : '.');
                                    }

                                    log.AppendLine();
                                }

                                log.AppendLine();

                            });

                            var printActionsCallbacks = new Action(() => {

                                var infoTable = new string[8] {
                                    "OnArrowKeyUp",
                                    "OnArrowKeyDown",
                                    "OnArrowKeyLeft",
                                    "OnArrowKeyRight",
                                    "OnPressed",
                                    "Unknown",
                                    "Unknown",
                                    "Unknown"
                                };

                                log.AppendLine("----- Actions -----");

                                //printHex(ms.ReadBytes(0x44), 0, 0x44, 16);

                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadByte()).AppendLine();

                                ms.Position += 3;

                                for (int j = 0; j < 8; j++)
                                {
                                    var unk1 = ms.ReadByte();

                                    ms.Position += 3;

                                    var mVal = ms.ReadInt32();

                                    log.AppendColumn(infoTable[j], 20).AppendFormat("{0}", (unk1 != 0xFF) ? String.Format("{0}, {1}", unk1, mVal) : "<NULL>");

                                    if (unk1 != 0xFF)
                                        log.AppendFormat("\t; {0} {1}", (unk1 >= 1) ? "open menu" : "highlight object", mVal);

                                    log.AppendLine();
                                }

                                log.AppendLine();
                                log.AppendLine("----- Callbacks -----");

                                var baseOffset = ms.Position;

                                for (int i = 0; i < 8; i++)
                                {
                                    ms.Position = baseOffset + (i * 0x20);

                                    var val = ms.ReadString();

                                    if (String.IsNullOrEmpty(val))
                                        val = "<NoAction>";

                                    log.AppendColumn(infoTable[i], 20).AppendLine(val);
                                }

                                ms.Position = baseOffset + 0x100;
                            });

                            for (int i = 0; i < count; i++)
                            {
                                log.AppendLine("==================== Menu {0} ====================", i + 1);

                                var baseOffset = ms.Position;

                                log.AppendColumn("Name", colSize).AppendLine(ms.ReadString(8).Trim('\0'));
                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadInt32());

                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadByte());

                                var m_offset1 = (ms.Position += 4);

                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadString());

                                ms.Position = m_offset1 + 0x20;

                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadString());

                                ms.Position = m_offset1 + 0x40;

                                log.AppendColumn("Unknown", colSize).AppendLine(ms.ReadByte()).AppendLine();

                                ms.Position = baseOffset + unk_0C;

                                var m_count = ms.ReadInt32();

                                log.AppendColumn("Entries", colSize).AppendLine(m_count).AppendLine();

                                for (int m = 0; m < m_count; m++)
                                {
                                    log.AppendLine("--------------- Entry {0} ---------------", m + 1);

                                    var mm_count = ms.ReadInt32();

                                    log.AppendColumn("Objects", colSize).AppendLine(mm_count).AppendLine();

                                    for (int k = 0; k < mm_count; k++)
                                    {
                                        log.AppendLine("---------- Object {0} ----------", k + 1);

                                        var k_type = ms.ReadByte();

                                        log.AppendColumn("Type", colSize).AppendLine(k_type);

                                        switch (k_type)
                                        {
                                        case 4:
                                            {
                                                readFloats1();
                                            } break;
                                        case 3:
                                            {
                                                readFloats1();

                                                var k_count = ms.ReadInt32();

                                                if (k_count != 0)
                                                {
                                                    log.AppendColumn("States", colSize).AppendLine(k_count).AppendLine();

                                                    for (int kk = 0; kk < k_count; kk++)
                                                    {
                                                        log.AppendLine("----- State {0} -----", kk + 1);

                                                        var kk_count = ms.ReadInt32();

                                                        log.AppendColumn("Elements", colSize).AppendLine(kk_count).AppendLine();

                                                        if (kk_count == 0)
                                                            continue;

                                                        for (int kki = 0; kki < kk_count; kki++)
                                                        {
                                                            log.AppendLine("--- Element {0} ---", kki + 1);

                                                            var j = ms.ReadByte();

                                                            log.AppendColumn("Type", colSize).AppendLine(j);

                                                            switch (j)
                                                            {
                                                            case 2:
                                                                {
                                                                    readFloats1();
                                                                    readFloats2();

                                                                    //log.AppendLine("Skipping 0x{0:X} of data @ 0x{1:X}...", unk_14, ms.Position);
                                                                    //ms.Position += unk_14;
                                                                } break;
                                                            case 1:
                                                                {
                                                                    readFloats1();
                                                                    readFloats3();

                                                                    //log.AppendLine("Skipping 0x{0:X} of data @ 0x{1:X}...", unk_18, ms.Position);
                                                                    //ms.Position += unk_18;
                                                                } break;
                                                            default:
                                                                log.AppendLine("Unrecognized type.");
                                                                break;
                                                            }

                                                            log.AppendLine();
                                                        }
                                                    }
                                                }

                                                printActionsCallbacks();

                                                //log.AppendLine("Skipping 0x{0:X} of data @ 0x{1:X}...", unk_1C, ms.Position);
                                                //ms.Position += unk_1C;
                                            } break;
                                        case 2:
                                            {
                                                readFloats1();
                                                readFloats2();

                                                //var locId = ms.ReadInt32();

                                                //log.AppendLine("LocaleId: {0}", locId);
                                                //
                                                //log.AppendLine("Skipping 0x{0:X} of data @ 0x{1:X}...", (unk_0C - 4), ms.Position);
                                                //ms.Position += (unk_0C - 4);
                                            } break;
                                        case 1:
                                            {
                                                readFloats1();
                                                readFloats3();
                                                //log.AppendLine("Skipping 0x{0:X} of data @ 0x{1:X}...", unk_18, ms.Position);
                                                //ms.Position += unk_18;
                                            } break;
                                        default:
                                            Debug.Fail(String.Format("Unknown type @ offset 0x{0:X}!!!", ms.Position - 1));
                                            Environment.Exit(1);
                                            break;
                                        }

                                        log.AppendLine();
                                    }
                                }
                            }
                        }

                        File.WriteAllText(logFile, log.ToString());
                    }

                    mecFile.Dispose();
                }
         */

        public MECFile(string file) : base(file) { }
    }
}
