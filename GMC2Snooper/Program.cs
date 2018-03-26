using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using DSCript;
using DSCript.Spooling;

using GMC2Snooper.PS2;

namespace GMC2Snooper
{
    public static class StreamExtensions
    {
        public static T ReadStruct<T>(this Stream stream)
        {
            var length = Marshal.SizeOf(typeof(T));

            return stream.ReadStruct<T>(length);
        }

        public static T ReadStruct<T>(this Stream stream, int length)
        {
            var data = new byte[length];
            var ptr = Marshal.AllocHGlobal(length);

            stream.Read(data, 0, length);
            Marshal.Copy(data, 0, ptr, length);

            var t = (T)Marshal.PtrToStructure(ptr, typeof(T));

            Marshal.FreeHGlobal(ptr);
            return t;
        }
    }
    class Program
    {
        static VifParser VIF;

        static string Filename = "";

        static int StartIdx = -1;

        static bool Interactive = false;
        static bool BatchRunner = false;
        static bool ViewImages = true;

        static bool bDumpTextures = false;
        static bool bDumpMaterials = false;
        static bool bDumpModels = false;

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();

            Console.Title = "GMC2 Snooper";
            
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: gmc2snooper <file> <:index> [:-options] [:--]");
                Console.WriteLine("  Loads the first model package at an index from a chunk file.");
                Console.WriteLine("  If no index is specified, the first one will be loaded.");
                Console.WriteLine("  Additional arguments must begin with '-' and come after the index.");
                Console.WriteLine("  Append '--' at the end of your arguments to interactively load each model.");
                Console.WriteLine("  ** NOTE: File must be a valid PS2 CHNK file from Driv3r or Driver: PL! **");
                return;
            }
            else
            {
                Filename = args[0];

                for (int i = (args.Length - 1); i != 0; i--)
                {
                    var arg = args[i];

                    if (arg == "--" && !Interactive)
                    {
                        Interactive = true;
                        continue;
                    }

                    if (arg.StartsWith("-"))
                    {
                        switch (arg.TrimStart('-'))
                        {
                        case "b":
                        case "batch":
                            BatchRunner = true;
                            continue;
                        case "dV":
                        case "vifdump":
                            bDumpModels = true;
                            continue;
                        case "dM":
                        case "matdump":
                            bDumpMaterials = true;
                            continue;
                        case "dT":
                        case "texdump":
                            bDumpTextures = true;
                            continue;
                        default:
                            Console.WriteLine($"Unknown argument '{arg}'!");
                            continue;
                        }
                    }

                    if (StartIdx == -1)
                    {
                        StartIdx = int.Parse(arg);
                        continue;
                    }
                }

                // set default index
                if (StartIdx == -1)
                    StartIdx = 1;
            }

            if (!File.Exists(Filename))
            {
                Console.WriteLine("ERROR: File not found.");
                return;
            }

            if (StartIdx <= 0)
            {
                Console.WriteLine("ERROR: Index cannot be zero or negative.");
                return;
            }

            var chunker = new FileChunker();
            var modPacks = new List<SpoolableBuffer>();

            chunker.SpoolerLoaded += (s, e) => {
                if (s.Context == 0x32434D47)
                    modPacks.Add((SpoolableBuffer)s);
            };

            chunker.Load(Filename);

            if (modPacks.Count == 0)
            {
                Console.WriteLine($"ERROR: No model packages were found.");
                return;
            }

            var idx = (StartIdx - 1);

            if (idx >= modPacks.Count)
            {
                Console.WriteLine($"ERROR: Index was larger than the actual number of models available.");
                return;
            }

            if (BatchRunner && Interactive)
            {
                Console.WriteLine("WARNING: Interactive mode disabled due to batch mode being specified.");
                Interactive = false;
            }

            // disable image viewer for batched runs
            if (BatchRunner)
                ViewImages = false;
            
            while (idx < modPacks.Count)
            {
                var gmc2 = new ModelPackagePS2();
                var spooler = modPacks[idx];

                var parent = spooler.Parent;

                Console.WriteLine($">> ModelPackage index: {StartIdx}");
                Console.WriteLine($">> ModelPackage offset: 0x{spooler.BaseOffset:X}");

                if (parent != null)
                    Console.WriteLine($">> ModelPackage parent: 0x{parent.Context:X8}");

                using (var ms = spooler.GetMemoryStream())
                {
                    gmc2.LoadBinary(ms);
                    Console.WriteLine($">> Processed {gmc2.Models.Count} models / {gmc2.Materials.Count} materials.");
                }

                VIF = new VifParser();

                _buffer1 = new VBuffer();
                _buffer2 = new VBuffer();

                if (bDumpModels)
                {
                    Console.WriteLine(">> Dumping model info...");
                    DumpModelInfo(gmc2);
                }

                if (bDumpMaterials)
                {
                    Console.WriteLine(">> Dumping material info...");
                    DumpMaterials(gmc2);
                }

                ProcessTextures(gmc2, idx);

                if (Interactive)
                {
                    if ((idx + 1) < modPacks.Count)
                    {
                        Console.WriteLine("Press 'SPACE' to load the next model, or press any key to exit.");

                        if (Console.ReadKey().Key == ConsoleKey.Spacebar)
                        {
                            ++idx;
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Operation completed -- no more models left to process.");
                        Console.WriteLine("Press any key to exit.");
                        Console.ReadKey();
                    }
                }

                if (BatchRunner)
                {
                    if ((idx + 1) < modPacks.Count)
                    {
                        ++idx;
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("Operation completed successfully.");
                    }
                }

                // that's all, folks!
                break;
            }
        }

        public class Vertex
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        public class VertexStrip : Vertex
        {
            public int Flags { get; set; }
        }

        public class MeshStrip
        {
            public int BaseVertexIndex { get; set; }
            public int MinIndex { get; set; }

            public int NumVertices { get; set; }
        }

        public class VBuffer
        {
            public int Top { get; set; }

            public List<VertexStrip> Vertices { get; set; }

            public List<Vertex> Normals { get; set; }

            public List<Vertex> UV1s { get; set; }
            public List<Vertex> UV2s { get; set; }

            public VBuffer()
            {
                Vertices = new List<VertexStrip>();
                Normals = new List<Vertex>();
                UV1s = new List<Vertex>();
                UV2s = new List<Vertex>();
            }
        }

        private static VBuffer _buffer1 = new VBuffer();
        private static VBuffer _buffer2 = new VBuffer();

        public static VBuffer GetBuffer(VifParser parser)
        {
            return (parser.DoubleBuffered) ? _buffer2 : _buffer1;
        }

        public static void CollectVertices(VBuffer vBuf, ref int top, List<VertexStrip> vertices)
        {
            var numVertices = (vBuf.Vertices.Count - top);

            for (int v = 0; v < numVertices; v++)
            {
                var vert = vBuf.Vertices[top + v];
                vertices.Add(vert);
            }

            top += (numVertices - vBuf.Top);
        }

        public static StringBuilder SBU = new StringBuilder();

        public static void UnpackValues(VifParser parser, VifUnpackType packType, bool flag, bool masked, long[][] values)
        {
            var nextPack = (values.Length / parser.Cycle.WriteLength);
            var doublePacked = (nextPack != values.Length);

            var buffer = GetBuffer(parser);

            // shit
            switch (packType)
            {
            case VifUnpackType.S_16:
                if (!doublePacked)
                    throw new InvalidOperationException("can't do S_16 with only 1 value :(");

                for (int i = 0; i < values.Length; i++)
                {
                    var val = values[i][0];

                    var u = (val >> 0) & 0xFF;
                    var v = (val >> 8) & 0xFF;

                    if (u > 127)
                        u -= 128;
                    if (v > 127)
                        v -= 128;

                    var vt = new Vertex() {
                        X = (u / 128.0f),
                        Y = (v / 128.0f),
                        Z = 1.0f,
                    };

                    var uvs = (i < nextPack) ? buffer.UV1s : buffer.UV2s;

                    uvs.Add(vt);

                    SBU.AppendLine($"-> {vt.X:F4}, {vt.Y:F4}");
                }

                break;
            case VifUnpackType.V3_8:
                if (!masked)
                    throw new InvalidOperationException("can't do V3_8 non-masked :(");

                for (int i = 0; i < values.Length; i++)
                {
                    var vn = new Vertex() {
                        X = (values[i][0] / 256.0f),
                        Y = (values[i][1] / 256.0f),
                        Z = (values[i][2] / 256.0f),
                    };

                    buffer.Normals.Add(vn);

                    SBU.AppendLine($"-> {vn.X:F4}, {vn.Y:F4}, {vn.Z:F4}");
                }

                break;
            case VifUnpackType.V4_8:
                for (int i = 0; i < values.Length; i++)
                {
                    var vx = new VertexStrip() {
                        X = (values[i][0] / 128.0f),
                        Y = (values[i][1] / 128.0f),
                        Z = ((values[i][2] / 128.0f)),
                        Flags = (int)(values[i][3] & 0xFF),
                    };

                    buffer.Vertices.Add(vx);

                    SBU.AppendLine($"-> {vx.X:F4}, {vx.Y:F4}, {vx.Z:F4}, {vx.Flags}");
                }

                break;
            }
        }

        public static void DumpModelInfo(ModelPackagePS2 gmc2)
        {
            var sb = new StringBuilder();

            var minIndex = 0;

            var top1 = 0;
            var top2 = 0;

            // vif tag info :)
            for (int i = 0; i < gmc2.Models.Count; i++)
            {
                var model = gmc2.Models[i];

                Console.WriteLine($"**** Model {i + 1} / {gmc2.Models.Count} *****");
                Console.WriteLine($"Type: ({model.Type & 0xF}, {(model.Type & 0xF) >> 4})");
                Console.WriteLine($"UID: {model.UID:X8}");
                Console.WriteLine($"Handle: {model.Handle:X8}");
                Console.WriteLine($"Unknown: ({model.Unknown1:X4},{model.Unknown2:X4})");
                Console.WriteLine($"Transform1: ({model.Transform1.X:F4},{model.Transform1.Y:F4},{model.Transform1.Z:F4})");
                Console.WriteLine($"Transform2: ({model.Transform2.X:F4},{model.Transform2.Y:F4},{model.Transform2.Z:F4})");
#           if DUMP_MODELS
                sb.AppendLine($"# ----- Model {i + 1} ----- #");
                sb.AppendLine($"# type: ({model.Type & 0xF}, {(model.Type & 0xF) >> 4})");
                sb.AppendLine($"# unknown: ({model.Unknown1:X4},{model.Unknown2:X4})");

                sb.AppendLine($"o model{i+1:D4}");
#           endif
                var meshes = new List<MeshStrip>();

                for (int ii = 0; ii < model.SubModels.Count; ii++)
                {
                    var subModel = model.SubModels[ii];

                    Console.WriteLine($"******** Sub model {ii + 1} / {model.SubModels.Count} *********");
                    Console.WriteLine($"Type: {subModel.Type}");
                    Console.WriteLine($"Flags: {subModel.Flags}");
                    Console.WriteLine($"Unknown: ({subModel.Unknown1},{subModel.Unknown2})");
                    Console.WriteLine($"TexId: {subModel.TextureId}");
                    Console.WriteLine($"TexSource: {subModel.TextureSource:X4}");

                    if (subModel.HasVectorData)
                    {
                        var v1 = subModel.V1;
                        var v2 = subModel.V2;
                        Console.WriteLine($"V1: ({v1.X:F4},{v1.Y:F4},{v1.Z:F4})");
                        Console.WriteLine($"V2: ({v2.X:F4},{v2.Y:F4},{v2.Z:F4})");
                    }

                    if (subModel.HasTransform)
                    {
                        var transform = subModel.Transform;
                        Console.WriteLine($"Transform X: ({transform.X.X:F4},{transform.X.Y:F4},{transform.X.Z:F4},{transform.X.W:F4})");
                        Console.WriteLine($"Transform Y: ({transform.Y.X:F4},{transform.Y.Y:F4},{transform.Y.Z:F4},{transform.Y.W:F4})");
                        Console.WriteLine($"Transform Z: ({transform.Z.X:F4},{transform.Z.Y:F4},{transform.Z.Z:F4},{transform.Z.W:F4})");
                    }

                    var dbf = VIF.DoubleBuffered;

                    VBuffer vBuf = GetBuffer(VIF);

                    using (var ms = new MemoryStream(subModel.ModelDataBuffer))
                    {
                        while (ms.Position < ms.Length)
                        {
                            // check alignment
                            if ((ms.Position & 0x3) != 0)
                                ms.Align(4);

                            try
                            {
                                VIF.ReadTag(ms, UnpackValues);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($">> VIFcode read error: '{e.Message}', terminating...");
                                Environment.Exit(1);
                            }

                            DumpVIFTag();

                            switch ((VifCommandType)VIF.Code.CMD)
                            {
                            case VifCommandType.ITop:
                                vBuf.Top = VIF.ITops;
                                break;

                            case VifCommandType.MsCal:
                            case VifCommandType.MsCalf:
                            case VifCommandType.MsCnt:
                                // swap buffers
                                vBuf = GetBuffer(VIF);
                                break;
                            }
                        }
                    }

#               if DUMP_MODELS
                    var vertices = new List<VertexStrip>();

                    CollectVertices(_buffer1, ref top1, vertices);
                    CollectVertices(_buffer2, ref top2, vertices);

                    var numVertices = vertices.Count;

                    var name = $"model{i + 1:D4}_{ii + 1:D4}";

                    sb.AppendLine($"# ----- SubModel {ii + 1} ----- #");
                    sb.AppendLine($"# type: {subModel.Type}");
                    sb.AppendLine($"# flags: {subModel.Flags}");
                    sb.AppendLine($"# unknown: ({subModel.Unknown1},{subModel.Unknown2})");
                    sb.AppendLine($"# index: {minIndex + 1}");
                    sb.AppendLine($"# vertices: {numVertices}");
                    
                    for (int v = 0; v < numVertices; v++)
                    {
                        var vert = vertices[v];
                    
                        var vx = (vert.X * model.Transform2.X);
                        var vy = (vert.Y * model.Transform2.Y);
                        var vz = (vert.Z * model.Transform2.Z);

                        sb.AppendLine($"v {vx:F4} {vy:F4} {vz:F4}");
                    }
                    
                    sb.AppendLine($"g {name}");
                    sb.AppendLine($"s off");
                    
                    for (int t = 0; t < numVertices - 2; t += 3)
                    {
                        int i0, i1, i2;
                    
                        i0 = (minIndex + t) + 1;
                        i1 = (minIndex + t + 1) + 1;
                        i2 = (minIndex + t + 2) + 1;
                    
                        sb.AppendLine($"f {i0} {i1} {i2}");
                    }

                    minIndex += numVertices;
                    Console.WriteLine();
#               endif
                }

                Console.WriteLine();
            }
#       if DUMP_MODELS
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "dump.obj"), sb.ToString());
#       endif
        }



        public static string[] VIFModeTypes = new[] {
            "NORMAL",
            "OFFSET",
            "DIFFERENCE",
        };

        public static string[] VIFMaskTypes = new[] {
            "DATA",
            "MASK_ROW",
            "MASK_COL",
            "WRITE_PROTECT",
        };

        public static void DumpVIFTag()
        {
            var vif = VIF.Code;

            var imdt = new VifImmediate(vif.IMDT);
            var cmd = new VifCommand(vif.CMD);

            var cmdName = "";
            var cmdInfo = "";

            var sb = new StringBuilder();

            switch ((VifCommandType)vif.CMD)
            {
            case VifCommandType.Nop:
                cmdName = "NOP";
                break;
            case VifCommandType.StCycl:
                cmdName = "STCYCL";
                cmdInfo = String.Format("{0,-10}{1,-10}",
                    $"CL:{VIF.Cycle.Length},",
                    $"WL:{VIF.Cycle.WriteLength}");
                break;
            case VifCommandType.Offset:
                cmdName = "OFFSET";
                cmdInfo = String.Format("{0,-10}", $"OFFSET:{imdt.IMDT_OFFSET:X}");
                break;
            case VifCommandType.ITop:
                var itop = imdt.IMDT_ITOP;
                cmdName = "ITOP";
                cmdInfo = String.Format("{0,-10}", $"ADDR:{itop:X} ({itop} vertices)");
                break;
            case VifCommandType.StMod:
                var mode = (int)VIF.Mode;
                cmdName = "STMOD";
                cmdInfo = String.Format("{0,-10}", $"MODE:{mode} ({VIFModeTypes[mode]})");
                break;
            case VifCommandType.MsCal:
                cmdName = "MSCAL";
                cmdInfo = String.Format("{0,-10}", $"EXECADDR:{imdt.IMDT_MSCAL:X}");
                break;
            case VifCommandType.MsCnt:
                cmdName = "MSCNT";
                sb.AppendLine($"( {VIF.DumpRegisters()} )");
                break;
            case VifCommandType.StMask:
                var mask = VIF.Mask;

                cmdName = "STMASK";
                cmdInfo = String.Format("{0,-10}", $"MASK:{(int)mask:X8}");

                sb.AppendFormat("-> {0,-16}{1,-16}{2,-16}{3,-16}",
                    "MASK_X",
                    "MASK_Y",
                    "MASK_Z",
                    "MASK_W");

                sb.AppendLine();

                for (int m = 0; m < 4; m++)
                {
                    sb.Append("-> ");

                    for (int mI = 0; mI < 4; mI++)
                        sb.AppendFormat("{0,-16}", VIFMaskTypes[(int)mask[(m * 4) + mI]]);

                    sb.AppendLine($"; V{m + 1}");
                }
                break;
            case VifCommandType.Flush:
                cmdName = "FLUSH";
                break;
            case VifCommandType.Direct:
                cmdName = "DIRECT";
                cmdInfo = String.Format("{0,-10}", $"SIZE:{imdt.IMDT_DIRECT:X}");
                break;
            default:
                if (Enum.IsDefined(typeof(VifCommandType), (int)vif.CMD))
                {
                    Console.WriteLine($">> Unhandled VIF command '{(VifCommandType)vif.CMD}', I might crash!");
                }
                else
                {
                    var addr = (imdt.ADDR * 16);

                    if (imdt.FLG)
                        addr += VIF.Tops;

                    if (cmd.P == 3)
                    {
                        cmdName = cmd.ToString();
                        cmdInfo = String.Format("{0,-10}{1,-10}",
                            $"ADDR:{addr:X},",
                            $"NUM:{vif.NUM}");
                    }
                    else
                    {
                        cmdName = $"$$CMD_{vif.CMD:X2}$$";
                        cmdInfo = String.Format("{0,-10}{1,-10}{2,-10}",
                            $"ADDR:{imdt.ADDR * 16:X} ({addr:X}),",
                            $"NUM:{vif.NUM},",
                            $"IRQ:{vif.IRQ}");
                    }
                }
                break;
            }

            var props = "";

            if (imdt.FLG)
                props += "+FLAG ";
            if (imdt.USN)
                props += "+UNSIGNED ";

            Console.WriteLine($"  {cmdName,-16}{" : ",4}{props,-16}{": ",4}{cmdInfo,-8}");

            if (sb.Length > 0)
                Console.Write(sb.ToString());

            // dump unpacked values?
            if (SBU.Length > 0)
            {
                //Console.Write(SBU.ToString());
                SBU = new StringBuilder();
            }
        }

        public static void DumpMaterials(ModelPackagePS2 gmc2)
        {
            var sb = new StringBuilder();

            for (int m = 0; m < gmc2.Materials.Count; m++)
            {
                var mat = gmc2.Materials[m];

                sb.AppendLine($"material[{m + 1}] {{");
                sb.AppendLine($"  type = {mat.Type.ToString()};");
                sb.AppendLine($"  anim_speed = {mat.AnimationSpeed};");
                sb.AppendLine($"  substances[{mat.Substances.Count}] = [");

                for (int s = 0; s < mat.Substances.Count; s++)
                {
                    var sub = mat.Substances[s];
                    
                    sb.AppendLine($"    substance[{s + 1}] {{");
                    sb.AppendLine($"      type = {sub.Type.ToString()};");
                    sb.AppendLine($"      bin = {sub.Bin:X4};");
                    sb.AppendLine($"      flags = {sub.Flags:X4};");
                    sb.AppendLine($"      textures[{sub.Textures.Count}] = [");

                    for (int t = 0; t < sub.Textures.Count; t++)
                    {
                        var tex = sub.Textures[t];

                        sb.AppendLine($"        texture[{t + 1}] : {tex.GUID:X16} {{");

                        sb.AppendLine($"          comptype = {tex.CompType.ToString()};");
                        sb.AppendLine($"          mipmaps = {tex.MipMaps};");
                        sb.AppendLine($"          regs = {tex.Regs:X2};");
                        sb.AppendLine($"          width = {tex.Width};");
                        sb.AppendLine($"          height = {tex.Height};");
                        sb.AppendLine($"          k = {tex.K:X4};");
                        sb.AppendLine($"          dataoffset = {tex.DataOffset:X};");
                        
                        sb.AppendLine($"          pixmaps[{tex.CLUTs.Count}] = [");

                        foreach (var clut in tex.CLUTs)
                            sb.AppendLine($"            0x{clut:X},");

                        sb.AppendLine("          ];");
                        sb.AppendLine("        },");
                    }

                    sb.AppendLine("      ];");
                    sb.AppendLine("    },");
                }

                sb.AppendLine("  ];");
                sb.AppendLine("};");
            }

            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "materials.txt"), sb.ToString());
            Console.WriteLine(sb.ToString());
        }

        private static byte[][] ReadCLUT(byte[] buffer, int count, int where, bool useAlpha)
        {
            byte[][] clut = new byte[count][];

            for (int i = 0; i < clut.Length; i++)
            {
                var pal = BitConverter.ToUInt32(buffer, where + (i * 4));

                clut[i] = new byte[4];

                byte r = 0x00;
                byte g = 0x00;
                byte b = 0x00;
                byte a = 0xFF;

                if (useAlpha)
                {
                    var alpha = (byte)((pal >> 24) & 0xFF);

                    if (alpha != 0x80)
                        a = (byte)((alpha & 0x7F) << 1);
                }

                r = (byte)(pal & 0xFF);
                g = (byte)((pal >> 8) & 0xFF);
                b = (byte)((pal >> 16) & 0xFF);

                clut[i][0] = a;
                clut[i][1] = r;
                clut[i][2] = g;
                clut[i][3] = b;
            }

            return clut;
        }

        private static Color[] Read4bppCLUT(byte[] buffer, TextureDataPS2 texture, int idx, bool useAlpha = false)
        {
            var where = texture.CLUTs[idx];
            var clut = ReadCLUT(buffer, 16, where, useAlpha);

            Color[] palette = new Color[16];

            for (int i = 0; i < 16; i++)
            {
                var color = clut[i];

                palette[i] =
                    Color.FromArgb(
                        color[0],
                        color[1],
                        color[2],
                        color[3]
                    );
            }

            return palette;
        }

        private static Color[] Read8bppCLUT(byte[] buffer, TextureDataPS2 texture, int idx, bool useAlpha = false)
        {
            var where = texture.CLUTs[idx];
            var clut = ReadCLUT(buffer, 256, where, useAlpha);

            Color[] palette = new Color[256];

            for (int i = 0; i < 256; i++)
            {
                var entry = (i & 0xE7);

                entry |= ((i >> 4) & 0x1) << 3;
                entry |= ((i >> 3) & 0x1) << 4;

                var color = clut[entry];

                palette[i] =
                    Color.FromArgb(
                        color[0],
                        color[1],
                        color[2],
                        color[3]
                    );
            }
            
            return palette;
        }
        
        private static BitmapHelper GetTextureBitmap(byte[] texBuffer, TextureDataPS2 tex, int clutIdx = -1, TextureDataPS2 clutTex = null)
        {
            if (clutIdx == -1)
                clutIdx = 0;

            // use same texture for clut lookup
            if (clutTex == null)
                clutTex = tex;
            
            switch (tex.CompType)
            {
            case TextureCompType.RGBA:
                {
                    return new BitmapHelper(texBuffer, tex.Width, tex.Height, tex.CLUTs[0], PixelFormat.Format32bppArgb);
                }
            case TextureCompType.PAL8:
                {
                    var img = new BitmapHelper(texBuffer, tex.Width, tex.Height, tex.CLUTs[1], PixelFormat.Format8bppIndexed);
                    var clut = Read8bppCLUT(texBuffer, clutTex, clutIdx);

                    img.Unswizzle(tex.Width, tex.Height, SwizzleType.Swizzle8bit);
                    img.SetColorPalette(clut);

                    return img;
                }
            case TextureCompType.PAL4:
                {
                    var img = new BitmapHelper(texBuffer, tex.Width, tex.Height, tex.CLUTs[1], PixelFormat.Format8bppIndexed);
                    var clut = Read4bppCLUT(texBuffer, clutTex, clutIdx);

                    img.Unswizzle(tex.Width, tex.Height, SwizzleType.Swizzle4bit);
                    img.SetColorPalette(clut);

                    return img;
                }
            case TextureCompType.VQ2:
                {

                } break;
            case TextureCompType.VQ4:
                {

                } break;
            case TextureCompType.HY2:
            case TextureCompType.HY2f:
                {

                } break;
            case TextureCompType.VQ4f:
                {

                } break;
            }
            return null;
        }

        private static Color[] CombineCLUTs(Color[] clutR, Color[] clutG, Color[] clutB, Color[] clutA, int blendMode)
        {
            var clut = new Color[256];

            var closeMatch = new Func<int, int, bool>((a, b) => {
                var max = Math.Max(a, b);
                var min = Math.Min(a, b);
                return (max - min) < 2;
            });

            for (int i = 0; i < 256; i++)
            {
                var a = clutA[i].A;
                var r = clutR[i].R;
                var g = clutG[i].G;
                var b = clutB[i].B;

                switch (blendMode)
                {
                case 1:
                    {
                        r = clutR[i].A;
                        g = clutG[i].A;
                        b = clutB[i].A;
                        a = 0xFF;
                    } break;
                case 2:
                    {
                        r = (byte)(0xFF - (r - clutA[i].R));
                        g = (byte)(0xFF - (g - clutA[i].G));
                        b = (byte)(0xFF - (b - clutA[i].B));
                        a = 0xFF;
                    } break;
                }

                clut[i] = Color.FromArgb(a, r, g, b);
            }

            return clut;
        }

        private static BitmapHelper GetSubstanceBitmap(byte[] texBuffer, SubstanceDataPS2 substance, int blendMode, int idx = 0)
        {
            var tex = substance.Textures[idx];
            var bmap = GetTextureBitmap(texBuffer, tex);
            
            var texList = substance.Textures.GetRange(idx, 4);
            var cluts = new Color[4][];

            var alphaMask = (blendMode >= 1);

            for (int c = 0; c < 4; c++)
                cluts[c] = Read8bppCLUT(texBuffer, texList[c], 0, alphaMask);

            var palette = CombineCLUTs(cluts[0], cluts[1], cluts[2], cluts[3], blendMode);
            
            bmap.SetColorPalette(palette);
            return bmap;
        }
        
        public static void ProcessTextures(ModelPackagePS2 gmc2, int modIdx)
        {
            BMPViewer viewer = new BMPViewer();
            
            if (bDumpTextures)
            {
                var dumpDir = Path.Combine(Environment.CurrentDirectory, "texture_dump");

                if (!Directory.Exists(dumpDir))
                    Directory.CreateDirectory(dumpDir);

                File.WriteAllBytes(Path.Combine(dumpDir, $"{gmc2.UID:D4}[{modIdx:D4}]_buffer.dat"), gmc2.TextureDataBuffer ?? new byte[0]);
            }

            string[] typeNames = {
                "LOD",
                "CLEAN",
                "DAMAGE",
            };

            for (int m = 0; m < gmc2.Materials.Count; m++)
            {
                var material = gmc2.Materials[m];

                for (int s = 0; s < material.Substances.Count; s++)
                {
                    var substance = material.Substances[s];
                    var processAll = true;

                    var texName = $"{m + 1:D4}_{s + 1:D2}";

                    var addToViewer = new Action<BitmapHelper, string>((bmap, name) => {
                        if (bmap == null)
                            return;

                        viewer.AddImageByName(bmap, name);
                    });

                    var dumpTex = new Action<BitmapHelper>((bmap) => {
                        if (bmap == null)
                            return;

                        var outDir = Path.Combine(Environment.CurrentDirectory, "textures");

                        if (!Directory.Exists(outDir))
                            Directory.CreateDirectory(outDir);

                        // so apparently I can't get the real pixel data if the clut was changed?!
                        // this makes no #$%^ing sense!
                        using (var bitmap = new Bitmap(bmap.Bitmap))
                        {
                            var pixels = bitmap.ToByteArray(PixelFormat.Format8bppIndexed);
                            var filename = Path.Combine(outDir, $"{Memory.GetCRC32(pixels):X8}.bmp");

                            bitmap.Save(filename, ImageFormat.Bmp);
                        }
                    });

                    var processTex = new Action<TextureDataPS2, string>((t, name) => {
                        var img = GetTextureBitmap(gmc2.TextureDataBuffer, t);

                        if (bDumpTextures)
                            dumpTex(img);

                        addToViewer(img, name);
                    });

                    var processVTex = new Action<int, SubstanceDataPS2>((type, subst) => {
                        BitmapHelper tex = null;
                        var name = $"{texName}_{typeNames[type]}";

                        switch (type)
                        {
                        case 0:
                        case 1:
                            tex = GetTextureBitmap(gmc2.TextureDataBuffer, substance.Textures[0]);
                            break;
                        case 2:
                            tex = GetTextureBitmap(gmc2.TextureDataBuffer, substance.Textures[0], 0, substance.Textures[1]);
                            break;
                        }

                        if (bDumpTextures)
                            dumpTex(tex);

                        if (ViewImages)
                            addToViewer(tex, name);
                    });

                    var processVBlendTex = new Action<int, SubstanceDataPS2, int>((type, subst, startIdx) => {
                        var name = $"{texName}_{typeNames[type]}";

                        var texD = GetSubstanceBitmap(gmc2.TextureDataBuffer, subst, 0, startIdx);
                        var texA = GetSubstanceBitmap(gmc2.TextureDataBuffer, subst, 1, startIdx);
                        var texM = GetSubstanceBitmap(gmc2.TextureDataBuffer, subst, 2, startIdx);

                        if (bDumpTextures)
                        {
                            dumpTex(texD);
                            dumpTex(texA);
                            dumpTex(texM);
                        }

                        if (ViewImages)
                        {
                            addToViewer(texD, $"{name}");
                            addToViewer(texA, $"{name}[A]");
                            addToViewer(texM, $"{name}[M]");
                        }
                    });

                    if (substance.Type == SubstanceType.Blended)
                    {
                        processAll = false;

                        // process vehicle textures
                        switch (substance.Flags)
                        {
                        case 0:
                            {
                                // lod texture
                                processVBlendTex(0, substance, 0);
                            }
                            break;
                        case 5:
                            {
                                // clean & damage textures
                                if (substance.Textures.Count > 2)
                                {
                                    processVBlendTex(1, substance, 0);
                                    
                                    // damage textures?
                                    if (substance.Textures.Count > 4)
                                        processVBlendTex(2, substance, 4);
                                }
                                else
                                {
                                    // no color mask
                                    processVTex(1, substance);
                                    processVTex(2, substance);
                                }
                            } break;
                        }
                    }

                    // do we process all textures normally?
                    if (processAll)
                    {
                        for (int t = 0; t < substance.Textures.Count; t++)
                        {
                            var texture = substance.Textures[t];
                            processTex(texture, $"{texName}_{t:D2} : {texture.GUID:X16}");
                        }
                    }
                }
            }
            
            if (ViewImages && viewer.HasImages)
            {
                viewer.Init();
                Console.WriteLine("The texture viewer is now ready. Please close it to continue.");

                Application.Run(viewer);
            }
        }
    }
}
