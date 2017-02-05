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

            if (startIdx >= modPacks.Count)
            {
                Console.WriteLine($"ERROR: Index was larger than the actual number of models available.");
                return;
            }

            var idx = (startIdx - 1);
            
            while (idx < modPacks.Count)
            {
                Console.WriteLine($">> ModelPackage index: {startIdx}");

                var gmc2 = new ModelPackagePS2();
                var spooler = modPacks[startIdx];

                using (var ms = spooler.GetMemoryStream())
                {
                    gmc2.LoadBinary(ms);
                    Console.WriteLine($">> Processed {gmc2.Models.Count} models / {gmc2.Materials.Count} materials.");
                }
                
                Console.WriteLine(">> Dumping model info...");
                DumpModelInfo(gmc2);

                //Console.WriteLine(">> Dumping texture info...");
                //DumpTextures(gmc2);

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
            // vif tag info :)
            for (int i = 0; i < gmc2.Models.Count; i++)
            {
                var model = gmc2.Models[i];

                Console.WriteLine($"**** Model {i + 1} / {gmc2.Models.Count} *****");
                //Console.WriteLine($"Type: ({model.Type & 0xF}, {(model.Type & 0xF) >> 4})");
                //Console.WriteLine($"UID: {model.UID:X8}");
                //Console.WriteLine($"Handle: {model.Handle:X8}");
                //Console.WriteLine($"Unknown: ({model.Unknown1:X4},{model.Unknown2:X4})");
                //Console.WriteLine($"Transform1: ({model.Transform1.X:F4},{model.Transform1.Y:F4},{model.Transform1.Z:F4})");
                //Console.WriteLine($"Transform2: ({model.Transform2.X:F4},{model.Transform2.Y:F4},{model.Transform2.Z:F4})");

                for (int ii = 0; ii < model.SubModels.Count; ii++)
                {
                    var subModel = model.SubModels[ii];

                    Console.WriteLine($"******** Sub model {ii + 1} / {model.SubModels.Count} *********");
                    //Console.WriteLine($"Type: {subModel.Type}");
                    //Console.WriteLine($"Flags: {subModel.Flags}");
                    //Console.WriteLine($"Unknown: ({subModel.Unknown1},{subModel.Unknown2})");
                    //Console.WriteLine($"TexId: {subModel.TextureId}");
                    //Console.WriteLine($"TexSource: {subModel.TextureSource:X4}");
                    
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
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }
        
        public static void DumpVIFTag(Stream stream)
        {
            var vif = stream.ReadStruct<PS2.VifTag>();

            var imdt = new VifImmediate(vif.Imdt);
            var cmd = new VifCommand(vif.Cmd);
            
            var cmdName = "";
            var cmdInfo = "";

            switch ((VifCommandType)vif.Cmd)
            {
            case VifCommandType.Nop:
                cmdName = "NOP";
                stream.Position += 4;
                break;
            case VifCommandType.StCycl:
                cmdName = "STCYCL";
                cmdInfo = String.Format("{0,-10}{1,-10}", 
                    $"CL:{imdt.IMDT_STCYCL_CL},",
                    $"WL:{imdt.IMDT_STCYCL_WL}");
                break;
            case VifCommandType.Offset:
                cmdName = "OFFSET";
                cmdInfo = String.Format("{0,-10}", $"OFFSET:{imdt.IMDT_OFFSET:X}");
                stream.Position += 4;
                break;
            case VifCommandType.ITop:
                cmdName = "ITOP";
                cmdInfo = String.Format("{0,-10}", $"ADDR:{imdt.IMDT_ITOP:X}");
                stream.Position += 4;
                break;
            case VifCommandType.StMod:
                cmdName = "STMOD";
                cmdInfo = String.Format("{0,-10}", $"MODE:{imdt.IMDT_STMOD}");
                stream.Position += 4;
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
                cmdInfo = String.Format("{0,-10}", $"MASK:{stmask:X8}");
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
                            $"ADDR:{imdt.ADDR * 16:X},",
                            $"NUM:{vif.Num}");
                    }
                    else
                    {
                        cmdName = $"$$CMD_{vif.Cmd:X2}$$";
                        cmdInfo = String.Format("{0,-10}{1,-10}{2,-10}", 
                            $"ADDR:{imdt.ADDR * 16},",
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
                    var sb = new StringBuilder();

                    // packSize and packNum can be -1,
                    // but not since we're checking against invalid types
                    var packSize = cmd.GetUnpackDataSize();
                    var packNum = cmd.GetUnpackDataCount();
                    
                    for (int i = 0; i < vif.Num; i++)
                    {
                        // indent line
                        sb.Append($"  [{i + 1:D4}]: ");
                        switch (packSize)
                        {
                        // byte
                        case 1:
                            {
                                for (int n = 0; n < packNum; n++)
                                {
                                    long val = (imdt.USN) ? stream.ReadByte() : (sbyte)stream.ReadByte();
                                    
                                    sb.Append($"{val,-8}");
                                }
                            }
                            break;
                        // short
                        case 2:
                            {
                                for (int n = 0; n < packNum; n++)
                                {
                                    long val = (imdt.USN) ? stream.ReadUInt16() : (long)stream.ReadInt16();
                                    
                                    if (packType == VifUnpackType.V4_5551)
                                    {
                                        var r = (val >> 0) & 0x1F;
                                        var g = (val >> 5) & 0x1F;
                                        var b = (val >> 10) & 0x1F;
                                        var a = (val >> 15) & 0x1;

                                        sb.AppendFormat("r:{0,-6}", r);
                                        sb.AppendFormat("g:{0,-6}", g);
                                        sb.AppendFormat("b:{0,-6}", b);
                                        sb.AppendFormat("a:{0,-6}", a);
                                    }
                                    else
                                    {
                                        sb.Append($"{val,-8}");
                                    }
                                }
                            }
                            break;
                        // int
                        case 4:
                            {
                                for (int n = 0; n < packNum; n++)
                                {
                                    long val = (imdt.USN) ? stream.ReadUInt32() : (long)stream.ReadInt32();
                                    
                                    sb.Append($"{val,-8}");
                                }
                            }
                            break;
                        }

                        sb.AppendLine("");
                    }
                    
                    Console.WriteLine(sb.ToString());
                }

                /*
                switch (cmd.VN)
                {
                case 0:
                    switch (cmd.VL)
                    {
                    // S_32
                    case 0:
                        for (int s = 0; s < vif.Num; s++)
                        {
                            var s_32 = stream.ReadUInt32();
                        }
                        break;
                    // S_16
                    case 1:
                        for (int s = 0; s < vif.Num; s++)
                        {
                            var s_16 = stream.ReadUInt16();
                        }
                        break;
                    // S_8
                    case 2:
                        for (int s = 0; s < vif.Num; s++)
                        {
                            var s_8 = stream.ReadByte();
                        }
                        break;
                    } break;
                case 1:
                    switch (cmd.VL)
                    {
                    // V2_32
                    case 0:
                        for (int v = 0; v < vif.Num; v++)
                        {
                            float v1 = (imdt.USN) ? (stream.ReadUInt32() / 255.0f) : (stream.ReadInt32() / 255.0f);
                            float v2 = (imdt.USN) ? (stream.ReadUInt32() / 255.0f) : (stream.ReadInt32() / 255.0f);
                        }
                        break;
                    // V2_16
                    case 1:
                        for (int v = 0; v < vif.Num; v++)
                        {
                            float v1 = (imdt.USN) ? (stream.ReadUInt16() / 255.0f) : (stream.ReadInt16() / 255.0f);
                            float v2 = (imdt.USN) ? (stream.ReadUInt16() / 255.0f) : (stream.ReadInt16() / 255.0f);
                        }
                        break;
                    // V2_8
                    case 2:
                        for (int v = 0; v < vif.Num; v++)
                        {
                            float v1 = (imdt.USN) ? (stream.ReadByte() / 255.0f) : ((sbyte)stream.ReadByte() / 255.0f);
                            float v2 = (imdt.USN) ? (stream.ReadByte() / 255.0f) : ((sbyte)stream.ReadByte() / 255.0f);
                        }
                        break;
                    } break;
                case 2:
                    switch (cmd.VL)
                    {
                    // V3_32
                    case 0:
                        for (int v = 0; v < vif.Num; v++)
                        {
                            float v1 = (imdt.USN) ? (stream.ReadUInt32() / 255.0f) : (stream.ReadInt32() / 255.0f);
                            float v2 = (imdt.USN) ? (stream.ReadUInt32() / 255.0f) : (stream.ReadInt32() / 255.0f);
                            float v3 = (imdt.USN) ? (stream.ReadUInt32() / 255.0f) : (stream.ReadInt32() / 255.0f);
                            float v4 = (imdt.USN) ? (stream.ReadUInt32() / 255.0f) : (stream.ReadInt32() / 255.0f);
                        }
                        break;
                    // V3_16
                    case 1:
                        for (int v = 0; v < vif.Num; v++)
                        {
                            float v1 = (imdt.USN) ? (stream.ReadUInt16() / 255.0f) : (stream.ReadInt16() / 255.0f);
                            float v2 = (imdt.USN) ? (stream.ReadUInt16() / 255.0f) : (stream.ReadInt16() / 255.0f);
                            float v3 = (imdt.USN) ? (stream.ReadUInt16() / 255.0f) : (stream.ReadInt16() / 255.0f);
                        }
                        break;
                    // V3_8
                    case 2:
                        for (int v = 0; v < vif.Num; v++)
                        {
                            float v1 = (imdt.USN) ? (stream.ReadByte() / 255.0f) : ((sbyte)stream.ReadByte() / 255.0f);
                            float v2 = (imdt.USN) ? (stream.ReadByte() / 255.0f) : ((sbyte)stream.ReadByte() / 255.0f);
                            float v3 = (imdt.USN) ? (stream.ReadByte() / 255.0f) : ((sbyte)stream.ReadByte() / 255.0f);
                        }
                        break;
                    } break;
                case 3:
                    switch (cmd.VL)
                    {
                    // V4_32
                    case 0:
                        for (int v = 0; v < vif.Num; v++)
                        {
                            float v1 = (imdt.USN) ? (stream.ReadUInt32() / 255.0f) : (stream.ReadInt32() / 255.0f);
                            float v2 = (imdt.USN) ? (stream.ReadUInt32() / 255.0f) : (stream.ReadInt32() / 255.0f);
                            float v3 = (imdt.USN) ? (stream.ReadUInt32() / 255.0f) : (stream.ReadInt32() / 255.0f);
                            float v4 = (imdt.USN) ? (stream.ReadUInt32() / 255.0f) : (stream.ReadInt32() / 255.0f);
                        }
                        break;
                    // V4_16
                    case 1:
                        for (int v = 0; v < vif.Num; v++)
                        {
                            float v1 = (imdt.USN) ? (stream.ReadUInt16() / 255.0f) : (stream.ReadInt16() / 255.0f);
                            float v2 = (imdt.USN) ? (stream.ReadUInt16() / 255.0f) : (stream.ReadInt16() / 255.0f);
                            float v3 = (imdt.USN) ? (stream.ReadUInt16() / 255.0f) : (stream.ReadInt16() / 255.0f);
                            float v4 = (imdt.USN) ? (stream.ReadUInt16() / 255.0f) : (stream.ReadInt16() / 255.0f);
                        }
                        break;
                    // V4_8
                    case 2:
                        for (int v = 0; v < vif.Num; v++)
                        {
                            float v1 = (imdt.USN) ? (stream.ReadByte() / 255.0f) : ((sbyte)stream.ReadByte() / 255.0f);
                            float v2 = (imdt.USN) ? (stream.ReadByte() / 255.0f) : ((sbyte)stream.ReadByte() / 255.0f);
                            float v3 = (imdt.USN) ? (stream.ReadByte() / 255.0f) : ((sbyte)stream.ReadByte() / 255.0f);
                            float v4 = (imdt.USN) ? (stream.ReadByte() / 255.0f) : ((sbyte)stream.ReadByte() / 255.0f);
                        }
                        break;
                    // V4_5
                    case 3:
                        for (int v = 0; v < vif.Num; v++)
                        {
                            var value = stream.ReadUInt16();


                        }
                        break;
                    }
                    break;
                }
                */
            }
        }

        public static void DumpTextures(ModelPackagePS2 gmc2)
        {
            // dump textures
            foreach (var tex in gmc2.Textures)
            {
                Console.WriteLine($"texture {tex.Reserved:X16} {{");

                Console.WriteLine($"  type = {tex.Type};");
                Console.WriteLine($"  flags = 0x{tex.Flags:X};");
                Console.WriteLine($"  width = {tex.Width};");
                Console.WriteLine($"  height = {tex.Height};");
                Console.WriteLine($"  unknown1 = 0x{tex.Unknown1:X};");
                Console.WriteLine($"  dataOffset = 0x{tex.DataOffset:X};");
                Console.WriteLine($"  unknown2 = 0x{tex.Unknown2:X};");

                Console.WriteLine($"  cluts[{tex.Modes}] = [");

                foreach (var mode in tex.CLUTs)
                    Console.WriteLine($"    0x{mode:X},");

                Console.WriteLine("  ];");

                Console.WriteLine("}");
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
