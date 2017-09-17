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

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();

            Console.Title = "GMC2 Snooper";

            var filename = "";
            var startIdx = -1;
            var interactive = false;

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: gmc2snooper <file> <:index> <:-->");
                Console.WriteLine("  Loads the first model package at an index from a chunk file.");
                Console.WriteLine("  If no index is specified, the first one will be loaded.");
                Console.WriteLine("  Append '--' at the end of your arguments to interactively load each model.");
                Console.WriteLine("  ** NOTE: File must be a valid PS2 CHNK file from Driv3r or Driver: PL! **");
                return;
            }
            else
            {
                filename = args[0];

                for (int i = (args.Length - 1); i != 0; i--)
                {
                    var arg = args[i];

                    if (arg == "--" && !interactive)
                    {
                        interactive = true;
                        continue;
                    }
                    if (startIdx == -1)
                    {
                        startIdx = int.Parse(arg);
                        continue;
                    }
                }

                // set default index
                if (startIdx == -1)
                    startIdx = 1;
            }

            if (!File.Exists(filename))
            {
                Console.WriteLine("ERROR: File not found.");
                return;
            }

            if (startIdx <= 0)
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

            chunker.Load(filename);

            if (modPacks.Count == 0)
            {
                Console.WriteLine($"ERROR: No model packages were found.");
                return;
            }

            var idx = (startIdx - 1);

            if (idx >= modPacks.Count)
            {
                Console.WriteLine($"ERROR: Index was larger than the actual number of models available.");
                return;
            }
            
            while (idx < modPacks.Count)
            {
                var gmc2 = new ModelPackagePS2();
                var spooler = modPacks[idx];

                var parent = spooler.Parent;

                Console.WriteLine($">> ModelPackage index: {startIdx}");
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
                
                Console.WriteLine(">> Dumping model info...");
                DumpModelInfo(gmc2);

                Console.WriteLine(">> Dumping material info...");
                DumpMaterials(gmc2);

                TestNewImageViewer(gmc2);

                if (interactive)
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
                
                sb.AppendLine($"# ----- Model {i + 1} ----- #");
                sb.AppendLine($"# type: ({model.Type & 0xF}, {(model.Type & 0xF) >> 4})");
                sb.AppendLine($"# unknown: ({model.Unknown1:X4},{model.Unknown2:X4})");

                sb.AppendLine($"o model{i+1:D4}");

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
                }

                Console.WriteLine();
            }


            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "dump.obj"), sb.ToString());
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
                sb.AppendLine($"  animated =  {((mat.Animated) ? 1 : 0)};");
                sb.AppendLine($"  anim_speed = {mat.AnimationSpeed};"); 
                sb.AppendLine($"  substances[{mat.Substances.Count}] = [");

                for (int s = 0; s < mat.Substances.Count; s++)
                {
                    var sub = mat.Substances[s];

                    sb.AppendLine($"    substance[{s + 1}] {{");
                    sb.AppendLine($"      flags = {sub.Flags:X4};");
                    sb.AppendLine($"      mode = {sub.Mode:X4};");
                    sb.AppendLine($"      type = {sub.Type:X4};");
                    sb.AppendLine($"      textures[{sub.Textures.Count}] = [");

                    for (int t = 0; t < sub.Textures.Count; t++)
                    {
                        var tex = sub.Textures[t];

                        sb.AppendLine($"        texture[{t + 1}] : {tex.Reserved:X16} {{");

                        sb.AppendLine($"          type = {tex.Type};");
                        sb.AppendLine($"          flags = 0x{tex.Flags:X};");
                        sb.AppendLine($"          width = {tex.Width};");
                        sb.AppendLine($"          height = {tex.Height};");
                        sb.AppendLine($"          unknown1 = 0x{tex.Unknown1:X};");
                        sb.AppendLine($"          dataOffset = 0x{tex.DataOffset:X};");
                        sb.AppendLine($"          unknown2 = 0x{tex.Unknown2:X};");

                        sb.AppendLine($"          cluts[{tex.Modes}] = [");

                        foreach (var mode in tex.CLUTs)
                            sb.AppendLine($"            0x{mode:X},");

                        sb.AppendLine("          ];");
                        sb.AppendLine("        }");
                    }

                    sb.AppendLine("      ];");
                }
                sb.AppendLine("  ];");
            }
            
            Console.WriteLine(sb.ToString());
        }

        public static void TestNewImageViewer(ModelPackagePS2 gmc2)
        {
            BMPViewer viewer = new BMPViewer();

            var texOffset = 0;

            // add all supported textures
            for (int t = 0; t < gmc2.Textures.Count; t++)
            {
                var tex = gmc2.Textures[t];
                var texName = $"texture #{t + 1}";


                var numCLUTs = tex.CLUTs.Count;

                if (numCLUTs < 2)
                {
                    Debug.WriteLine($"{texName} only has {numCLUTs} CLUTs?? (type: {tex.Type})");
                    continue;
                }

                if (tex.CLUTs[1] != tex.CLUTs[0])
                    texOffset = tex.CLUTs[1];

                switch (tex.Type)
                {
                case 1:
                    {
                        if (tex.CLUTs[1] != tex.CLUTs[0])
                            texOffset = tex.CLUTs[1];

                        var img = new BitmapHelper(gmc2.TextureDataBuffer, tex.Width, tex.Height, texOffset, PixelFormat.Format8bppIndexed);

                        img.Unswizzle(tex.Width, tex.Height, SwizzleType.Swizzle8bit);
                        img.Read8bppCLUT(gmc2.TextureDataBuffer, tex.CLUTs[0]);

                        viewer.AddImageByName(img, $"{texName}");

                        //if (numCLUTs >= 2)
                        //{
                        //    img.CLUTFromAlpha(gmc2.TextureDataBuffer, tex.CLUTs[0]);
                        //    viewer.AddImageByName(img, $"{texName} [A]");
                        //}

                        //for (int i = 2; i < numCLUTs; i++)
                        //{
                        //    img.CLUTFromAlpha(gmc2.TextureDataBuffer, tex.CLUTs[i]);
                        //    viewer.AddImageByName(img, $"{texName} [{i - 1}]");
                        //}
                    } break;
                case 2:
                    {
                        // can't read 4-bit textures properly :(

                        //var texBuf = Swizzlers.UnSwizzle4(gmc2.TextureDataBuffer, tex.Width, tex.Height, texOffset);
                        //var img = new BitmapHelper(texBuf, tex.Width, tex.Height, PixelFormat.Format4bppIndexed);
                        //
                        ////img.Read4bppCLUT(gmc2.TextureDataBuffer, tex.CLUTs[1]);
                        //
                        //viewer.AddImageByName(img, $"{texName}");
                    }
                    break;
                }
            }

            if (viewer.HasImages)
            {
                viewer.Init();
                Application.Run(viewer);
            }
        }

        public static void TestImageViewer()
        {
            byte[] TSC2Data;
            byte[] TSC2Data2;

            using (Stream f = new FileStream(@"C:\Users\Tech\Desktop\Swizzling\dsPS2_17", FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Stream f2 = new FileStream(@"C:\Users\Tech\Desktop\Swizzling\d3SP2", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                TSC2Data = new byte[(int)f.Length];
                TSC2Data2 = new byte[(int)f2.Length];

                f.Read(TSC2Data, 0, (int)f.Length);
                f2.Read(TSC2Data2, 0, (int)f2.Length);
            }

            // BitmapHelper img2 = new BitmapHelper(TSC2Data, 128, 256, 0x980, PixelFormat.Format8bppIndexed);
            // BitmapHelper img3 = new BitmapHelper(TSC2Data, 128, 256, 0xA980, PixelFormat.Format8bppIndexed);

            BitmapHelper img2 = new BitmapHelper(TSC2Data, 128, 128, 0xF00, PixelFormat.Format8bppIndexed);
            BitmapHelper img3 = new BitmapHelper(TSC2Data, 128, 256, 0x2D80, PixelFormat.Format8bppIndexed);

            BitmapHelper img4 = new BitmapHelper(TSC2Data2, 128, 256, 0x980, PixelFormat.Format8bppIndexed);
            BitmapHelper img5 = new BitmapHelper(TSC2Data2, 128, 256, 0xA980, PixelFormat.Format8bppIndexed);

            // swizzle testing from TSC2
            img2.Unswizzle(128, 128, SwizzleType.Swizzle8bit);
            img3.Unswizzle(128, 256, SwizzleType.Swizzle8bit);
            img4.Unswizzle(128, 256, SwizzleType.Swizzle8bit);
            img5.Unswizzle(128, 256, SwizzleType.Swizzle8bit);

            img3.Read8bppCLUT(TSC2Data, 0x2980);

            BMPViewer viewer = new BMPViewer();

            img3.CLUTFromRGB(TSC2Data, 0x2980, 0xAD80, 0xB180);
            viewer.AddImage(img3);

            img3.Bitmap.Save(@"C:\Users\Tech\Desktop\Swizzling\d3PS2_van1.bmp", ImageFormat.Bmp);

            img3.CLUTFromRGB(TSC2Data, 0xB980, 0xBD80, 0xC180);
            viewer.AddImage(img3);

            img4.CLUTFromRGB(TSC2Data2, 0x580, 0x8980, 0x8D80);
            viewer.AddImage(img4);
            img4.CLUTFromRGB(TSC2Data2, 0x9580, 0x9980, 0x9D80);
            viewer.AddImage(img4);

            img5.CLUTFromRGB(TSC2Data2, 0xA580, 0x12980, 0x12D80);
            viewer.AddImage(img5);

            img5.Bitmap.Save(@"C:\Users\Tech\Desktop\Swizzling\d3PS2_chally1.bmp", ImageFormat.Bmp);

            img5.CLUTFromRGB(TSC2Data2, 0x13580, 0x13980, 0x13D80);
            viewer.AddImage(img5);

            viewer.Init();

            Application.Run(viewer);

            // img2.Bitmap.Save(@"C:\Users\Tech\Desktop\Swizzling\d3PS2_unswizzled.bmp", ImageFormat.Bmp);


            Console.ReadKey();
        }
    }
}
