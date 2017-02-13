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
                
                Console.WriteLine(">> Dumping model info...");
                DumpModelInfo(gmc2);

                Console.WriteLine(">> Dumping texture info...");
                DumpTextures(gmc2);

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
        
        public static void DumpModelInfo(ModelPackagePS2 gmc2)
        {
            var sb = new StringBuilder();
            
            Vertices = new List<VertexStrip>();

            Normals = new List<Vertex>();
            UV1s = new List<Vertex>();
            UV2s = new List<Vertex>();

            var minIndex = 0;
            
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

                    sb.AppendLine($"# ----- SubModel {ii + 1} ----- #");

                    using (var ms = new MemoryStream(subModel.ModelDataBuffer))
                    {
                        while (ms.Position < ms.Length)
                        {
                            // check alignment
                            if ((ms.Position & 0x3) != 0)
                                ms.Align(4);

                            DumpVIFTag(ms);
                        }
                    }

                    var numVertices = 0;

                    for (int v = minIndex; v < VIFITop; v++)
                    {
                        var vx = Vertices[v];
                        
                        sb.AppendLine($"v {vx.X:F4} {vx.Y:F4} {vx.Z:F4}");
                        ++numVertices;
                    }

                    sb.AppendLine($"g model{i}_{ii}");
                    sb.AppendLine($"s off");
                    
                    
                    for (int t = 0; t < numVertices; t += 2)
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
        
        public static int VIFITop               = 0;
        
        public static int VIFCycle              = 0; // 'CL'
        public static int VIFCycleWriteLen      = 0; // 'WL'

        public static int VIFMode               = 0;

        public static string[] VIFModeTypes     = new[] {
            "NORMAL",
            "OFFSET",
            "DIFFERENCE",
        };
        
        public static uint[][] VIFMasks         = new uint[4][];

        public static string[] VIFMaskTypes     = new[] {
            "DATA",
            "MASK_ROW",
            "MASK_COL",
            "WRITE_PROTECT",
        };

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
        
        public static List<VertexStrip> Vertices = new List<VertexStrip>();

        public static List<Vertex> Normals = new List<Vertex>();

        public static List<Vertex> UV1s = new List<Vertex>();
        public static List<Vertex> UV2s = new List<Vertex>();
        
        public static void UnpackValues(VifUnpackType packType, int numVals, bool masked, long[][] values)
        {
            var nextPack = (values.Length / numVals);
            var doublePacked = (nextPack != values.Length);

            var sb = new StringBuilder();

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

                    var uvs = (i < nextPack) ? UV1s : UV2s;

                    uvs.Add(vt);

                    sb.AppendLine($"-> {vt.X:F4}, {vt.Y:F4}");
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

                    Normals.Add(vn);
                    
                    sb.AppendLine($"-> {vn.X:F4}, {vn.Y:F4}, {vn.Z:F4}");
                }

                break;
            case VifUnpackType.V4_8:
                for (int i = 0; i < values.Length; i++)
                {
                    var vx = new VertexStrip() {
                        X = (values[i][0] / 128.0f),
                        Y = (values[i][1] / 128.0f),
                        Z = ((values[i][2] / 128.0f) * 2.0f),
                        Flags = (int)(values[i][3] & 0xFF),
                    };

                    Vertices.Add(vx);
                    
                    sb.AppendLine($"-> {vx.X:F4}, {vx.Y:F4}, {vx.Z:F4}, {vx.Flags}");
                }

                break;
            }

            if (sb.Length > 0)
                Console.WriteLine(sb.ToString());
        }
        
        public static void DumpVIFTag(Stream stream)
        {
            var vif = stream.ReadStruct<PS2.VifTag>();

            var imdt = new VifImmediate(vif.Imdt);
            var cmd = new VifCommand(vif.Cmd);
            
            var cmdName = "";
            var cmdInfo = "";

            var sb = new StringBuilder();

            switch ((VifCommandType)vif.Cmd)
            {
            case VifCommandType.Nop:
                cmdName = "NOP";
                stream.Position += 4;
                break;
            case VifCommandType.StCycl:
                VIFCycle = imdt.IMDT_STCYCL_CL;
                VIFCycleWriteLen = imdt.IMDT_STCYCL_WL;

                cmdName = "STCYCL";
                cmdInfo = String.Format("{0,-10}{1,-10}", 
                    $"CL:{VIFCycle},",
                    $"WL:{VIFCycleWriteLen}");
                break;
            case VifCommandType.Offset:
                cmdName = "OFFSET";
                cmdInfo = String.Format("{0,-10}", $"OFFSET:{imdt.IMDT_OFFSET:X}");
                stream.Position += 4;
                break;
            case VifCommandType.ITop:
                VIFITop += imdt.IMDT_ITOP;

                cmdName = "ITOP";
                cmdInfo = String.Format("{0,-10}", $"ADDR:{VIFITop:X} ({VIFITop} vertices)");

                stream.Position += 4;
                break;
            case VifCommandType.StMod:
                VIFMode = imdt.IMDT_STMOD;

                cmdName = "STMOD";
                cmdInfo = String.Format("{0,-10}", $"MODE:{VIFMode} ({VIFModeTypes[VIFMode]})");
                break;
            case VifCommandType.MsCal:
                cmdName = "MSCAL";
                cmdInfo = String.Format("{0,-10}", $"EXECADDR:{imdt.IMDT_MSCAL:X}");
                stream.Position += 4;
                break;
            case VifCommandType.MsCnt:
                cmdName = "MSCNT";
                break;
            case VifCommandType.StMask:
                var stmask = stream.ReadUInt32();

                cmdName = "STMASK";
                cmdInfo = String.Format("{0,-10}", $"MASK:{stmask :X8}");
                
                sb.AppendFormat("-> {0,-16}{1,-16}{2,-16}{3,-16}", 
                    "MASK_X", 
                    "MASK_Y", 
                    "MASK_Z", 
                    "MASK_W");

                sb.AppendLine();

                for (int m = 0; m < 4; m++)
                {
                    sb.Append("-> ");

                    VIFMasks[m] = new uint[4];

                    for (int mI = 0; mI < 4; mI++)
                    {
                        var msk = (stmask >> ((m * 8) + (mI * 2))) & 0x3;

                        VIFMasks[m][mI] = msk;

                        sb.AppendFormat("{0,-16}", VIFMaskTypes[msk]);
                    }

                    sb.AppendLine($"; V{m + 1}");
                }
                break;
            case VifCommandType.Flush:
                cmdName = "FLUSH";
                stream.Position += 4;
                break;
            case VifCommandType.Direct:
                cmdName = "DIRECT";
                cmdInfo = String.Format("{0,-10}", $"SIZE:{imdt.IMDT_DIRECT:X}");
                stream.Position += ((imdt.IMDT_DIRECT * 16) + 4);
                break;
            default:
                if (Enum.IsDefined(typeof(VifCommandType), (int)vif.Cmd))
                {
                    Console.WriteLine($">> Unhandled VIF command '{(VifCommandType)vif.Cmd}', I might crash!");
                    stream.Position += 4;
                }
                else
                {
                    if (cmd.P == 3)
                    {
                        cmdName = cmd.ToString();
                        cmdInfo = String.Format("{0,-10}{1,-10}",
                            $"ADDR:{imdt.ADDR:X} ({imdt.ADDR * 16:X}),",
                            $"NUM:{vif.Num}");
                    }
                    else
                    {
                        cmdName = $"$$CMD_{vif.Cmd:X2}$$";
                        cmdInfo = String.Format("{0,-10}{1,-10}{2,-10}",
                            $"ADDR:{imdt.ADDR:X} ({imdt.ADDR * 16:X}),",
                            $"NUM:{vif.Num},",
                            $"IRQ:{vif.Irq}");
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

            if (cmd.P == 3)
            {
                var packType = cmd.GetUnpackDataType();
                
                if (packType == VifUnpackType.Invalid)
                {
                    Console.WriteLine($"Invalid VIF unpack type '{vif.ToString()}'!");
                }
                else
                {
                    // packSize and packNum can be -1,
                    // but not since we're checking against invalid types
                    var packSize = cmd.GetUnpackDataSize();
                    var packNum = cmd.GetUnpackDataCount();

                    var wl = VIFCycleWriteLen;
                    
                    if (packNum > 4)
                        throw new InvalidOperationException("too many packed values!!!");

                    var vals = new long[vif.Num][];

                    // initialize arrays
                    for (int v = 0; v < vals.Length; v++)
                    {
                        vals[v] = new long[packNum];

                        for (int n = 0; n < packNum; n++)
                            vals[v][n] = 0;
                    }

                    for (int i = 0; i < vif.Num; i++)
                    {
                        switch (packSize)
                        {
                        // byte
                        case 1:
                            {
                                for (int n = 0; n < packNum; n++)
                                {
                                    long val = (imdt.USN) ? stream.ReadByte() : (sbyte)stream.ReadByte();

                                    if (imdt.FLG)
                                        val -= 128;

                                    vals[i][n] = val;
                                }
                            }
                            break;
                        // short
                        case 2:
                            {
                                for (int n = 0; n < packNum; n++)
                                {
                                    long val = (imdt.USN) ? stream.ReadUInt16() : (long)stream.ReadInt16();

                                    vals[i][n] = val;
                                }
                            }
                            break;
                        // int
                        case 4:
                            {
                                for (int n = 0; n < packNum; n++)
                                {
                                    long val = (imdt.USN) ? stream.ReadUInt32() : (long)stream.ReadInt32();

                                    vals[i][n] = val;
                                }
                            }
                            break;
                        }
                    }

                    UnpackValues(packType, wl, (cmd.M == 1), vals);
                }
            }
            else
            {
                // generic info
                if (sb.Length > 0)
                    Console.Write(sb.ToString());
            }
        }

        public static void DumpTextures(ModelPackagePS2 gmc2)
        {
            var sb = new StringBuilder();

            // dump textures
            for (int t = 0; t < gmc2.Textures.Count; t++)
            {
                var tex = gmc2.Textures[t];

                sb.AppendLine($"texture[{t + 1}] : {tex.Reserved:X16} {{");

                sb.AppendLine($"  type = {tex.Type};");
                sb.AppendLine($"  flags = 0x{tex.Flags:X};");
                sb.AppendLine($"  width = {tex.Width};");
                sb.AppendLine($"  height = {tex.Height};");
                sb.AppendLine($"  unknown1 = 0x{tex.Unknown1:X};");
                sb.AppendLine($"  dataOffset = 0x{tex.DataOffset:X};");
                sb.AppendLine($"  unknown2 = 0x{tex.Unknown2:X};");

                sb.AppendLine($"  cluts[{tex.Modes}] = [");

                foreach (var mode in tex.CLUTs)
                    sb.AppendLine($"    0x{mode:X},");

                sb.AppendLine("  ];");
                sb.AppendLine("}");
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
