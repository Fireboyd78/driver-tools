using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

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
            
            // Console.CursorVisible = false;
            // Console.OutputEncoding = Encoding.UTF8;

            string fpath = @"C:\Dev\Research\Driv3r\__Research\PS2";
            string fname = @"miami.vvs_003A9000_00048480.GMC2";

            string filename = Path.Combine(fpath, fname);

            bool loadGMC2 = true;

            if (loadGMC2)
            {
                if (!filename.EndsWith("GMC2", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("The file:\r\n\r\n'{0}'\r\n\r\nIs not a GMC2 file.", filename);
                }
                else if (File.Exists(filename))
                {
                    var gmc2 = new ModelPackagePS2();

                    using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        gmc2.LoadBinary(fs);
                        Console.WriteLine($"Processed {gmc2.Models.Count} models / {gmc2.Materials.Count} materials.");
                    }

                    // vif tag info :)
                    foreach (var model in gmc2.Models)
                    {
                        foreach (var subModel in model.SubModels)
                        {
                            using (var ms = new MemoryStream(subModel.ModelDataBuffer))
                            {
                                Console.WriteLine("<<< BEGIN >>>");
                                while (ms.Position < ms.Length)
                                {
                                    // check alignment
                                    if ((ms.Position & 0x3) != 0)
                                        ms.Align(4);

                                    var vif = ms.ReadStruct<PS2.VifTag>();

                                    var imdt = new VifImmediate(vif.Imdt);
                                    var cmd = new VifCommand(vif.Cmd);

                                    Console.Write($"[{ms.Position:X8}:{vif.ToBinary():X8}] ");

                                    switch ((VifCommandType)vif.Cmd)
                                    {
                                    case VifCommandType.Nop:
                                        Console.WriteLine("NOP");
                                        ms.Position += 4;
                                        break;
                                    case VifCommandType.StCycl:
                                        Console.WriteLine("STCYCL");
                                        break;
                                    case VifCommandType.Offset:
                                        Console.WriteLine($"OFFSET");
                                        ms.Position += 4;
                                        break;
                                    case VifCommandType.ITop:
                                        Console.WriteLine("ITOP");
                                        ms.Position += 4;
                                        break;
                                    case VifCommandType.StMod:
                                        Console.WriteLine("STMOD");
                                        ms.Position += 4;
                                        break;
                                    case VifCommandType.MsCal:
                                        Console.WriteLine("MSCAL");
                                        ms.Position += 4;
                                        break;
                                    case VifCommandType.MsCnt:
                                        Console.WriteLine("MSCNT");
                                        break;
                                    case VifCommandType.StMask:
                                        Console.WriteLine("STMASK");
                                        ms.Position += 4;
                                        break;
                                    case VifCommandType.Flush:
                                        Console.WriteLine("FLUSH");
                                        ms.Position += 4;
                                        break;
                                    case VifCommandType.Direct:
                                        Console.WriteLine("DIRECT");
                                        ms.Position += ((imdt.ADDR * 16) + 4);
                                        break;
                                    default:
                                        string[] vntbl  = { "S", "V2", "V3", "V4" };
                                        uint[] vltbl    = { 32, 16, 8, 5 };

                                        Console.WriteLine($"({vntbl[cmd.VN]}_{vltbl[cmd.VL]}, M:{cmd.M}, P:{cmd.P}, ADDR:{imdt.ADDR:X4}, NUM:{vif.Num})");

                                        if (cmd.P == 3)
                                        {
                                            //if (imdt.FLG)
                                            //    Console.WriteLine(" +Flag");
                                            //if (imdt.USN)
                                            //    Console.WriteLine(" +Unsigned");
                                            //if (cmd.M == 1)
                                            //    Console.WriteLine(" +Mask");

                                            switch (cmd.VN)
                                            {
                                            case 0:
                                                {
                                                    if (cmd.VL == 1)
                                                    {
                                                        for (int vt = 0; vt < vif.Num; vt++)
                                                        {
                                                            int x, y;

                                                            if (imdt.USN)
                                                            {
                                                                x = ms.ReadByte();
                                                                y = ms.ReadByte();
                                                            }
                                                            else
                                                            {
                                                                x = (sbyte)ms.ReadByte();
                                                                y = (sbyte)ms.ReadByte();
                                                            }

                                                            //Console.WriteLine($"vn0_1 {x,-4} {y,-8}");
                                                        }
                                                        break;
                                                    }
                                                } goto UNKNOWN_VNVL;
                                            case 1:
                                                {
                                                    if (cmd.VL == 1)
                                                    {
                                                        for (int vt = 0; vt < vif.Num; vt++)
                                                        {
                                                            float u = (ms.ReadInt16() / 255.0f);
                                                            float v = (ms.ReadInt16() / 255.0f);

                                                            //Console.WriteLine($"vt {v:F4} {u:F4}");
                                                        }

                                                        break;
                                                    }
                                                } goto UNKNOWN_VNVL;
                                            case 2:
                                                {
                                                    if (cmd.VL == 1)
                                                    {
                                                        for (int v = 0; v < vif.Num; v++)
                                                        {
                                                            float x, y, z;

                                                            if (imdt.USN)
                                                            {
                                                                x = (ms.ReadUInt16() / 255.0f);
                                                                y = (ms.ReadUInt16() / 255.0f);
                                                                z = (ms.ReadUInt16() / 255.0f);
                                                            }
                                                            else
                                                            {
                                                                x = (ms.ReadInt16() / 255.0f);
                                                                y = (ms.ReadInt16() / 255.0f);
                                                                z = (ms.ReadInt16() / 255.0f);
                                                            }

                                                            //Console.WriteLine($"v {x:F4} {y:F4} {z:F4}");
                                                        }

                                                        break;
                                                    }

                                                    if (cmd.VL == 2)
                                                    {
                                                        for (int v = 0; v < vif.Num; v++)
                                                        {
                                                            var r = (byte)ms.ReadByte();
                                                            var g = (byte)ms.ReadByte();
                                                            var b = (byte)ms.ReadByte();

                                                            //Console.WriteLine($"rgb {r} {g} {b}");
                                                        }

                                                        break;
                                                    }
                                                } goto UNKNOWN_VNVL;
                                            case 3:
                                                {
                                                    if (cmd.VL == 1)
                                                    {
                                                        // v4-16
                                                        ms.Position += (vif.Num * 8);
                                                        break;
                                                    }
                                                    if (cmd.VL == 2)
                                                    {
                                                        // v4-8
                                                        ms.Position += (vif.Num * 4);
                                                        break;
                                                    }
                                                } goto UNKNOWN_VNVL;

                                            default:
                                            UNKNOWN_VNVL:
                                                Console.WriteLine($"Unknown VNVL combination ({cmd.VN},{cmd.VL})");
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (Enum.IsDefined(typeof(VifCommandType), (int)vif.Cmd))
                                            {
                                                Console.WriteLine($"Unhandled VIF command '{(VifCommandType)vif.Cmd}', program might crash!");
                                                ms.Position += 4;
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Unknown VIF command 0x{vif.Cmd:X2}");
                                            }
                                        }
                                        break;
                                    }
                                }
                                Console.WriteLine("<<< END >>>");
                            }
                        }
                    }

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

                Console.ReadKey();
            }
            else
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
}
