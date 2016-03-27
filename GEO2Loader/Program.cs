using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using GEO2Loader;

namespace GEO2Loader
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();

            Console.Title = "GMC2 Snooper";
            
            Console.SetBufferSize(Console.BufferWidth, 8192);
            // Console.CursorVisible = false;
            // Console.OutputEncoding = Encoding.UTF8;

            string fpath = @"C:\Program Files (x86)\Atari\DRIV3R\__Research\PS2\";
            string fname = @"miami.vvs_003A9000_00048480.GMC2";

            string filename = fpath + fname;

            bool loadGMC2 = true;

            if (loadGMC2)
            {
                GMC2Model GMC2File = new GMC2Model();

                if (!filename.EndsWith("GMC2", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("The file:\r\n\r\n'{0}'\r\n\r\nIs not a GMC2 file.", filename);
                }
                else if (File.Exists(filename))
                {
                    OpenGMC2File(filename, ref GMC2File);
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

        static void OpenGMC2File(string filename, ref GMC2Model GMC2)
        {
            using (Stream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (BinaryReader f = new BinaryReader(fs))
                {
                    if (f.ReadUInt32() != (uint)BlockType.MPAK)
                    {
                        Console.WriteLine("Invalid GMC2 file!");
                        return;
                    }

                    Console.WriteLine("Opening file: '{0}'\r\n", filename);

                    GMC2.Type = (ModelType)f.ReadUInt32();

                    fs.Seek(0xC, SeekOrigin.Current);

                    GMC2.nGeometry = f.ReadUInt32();
                    GMC2.TSC2.Offset = f.ReadUInt32();

                    fs.Seek(0x4, SeekOrigin.Current);

                    GMC2.Geometry = new List<GEO2Block>((int)GMC2.nGeometry);

                    long holdPos;

                    for (int g = 0; g <= GMC2.nGeometry - 1; g++)
                    {
                        string b = String.Format("Block[{0}]", g);

                        GMC2.Geometry.Insert(g, new GEO2Block(f.ReadUInt32()));

                        holdPos = fs.Position;

                        fs.Seek(GMC2.Geometry[g].Offset, SeekOrigin.Begin);

                        // Console.WriteLine("============================================\r\n" + "{0} @ 0x{1:X}\r\n", b, fs.Position);

                        if (f.ReadUInt32() != (uint)GEO2Block.Magic)
                        {
                            Console.WriteLine("Invalid GMC2 file -- tried to read invalid GEO2 block @ 0x{0:X}", fs.Position);
                            return;
                        }

                        GMC2.Geometry[g].Thing1Count = f.ReadByte();
                        GMC2.Geometry[g].Thing2Count = f.ReadByte();
                        GMC2.Geometry[g].Thing3Count = f.ReadByte();
                        GMC2.Geometry[g].UnkCount = f.ReadByte();

                        // Console.WriteLine("{0}.Thing1Count = {1}", b, GMC2.Geometry[g].Thing1Count);
                        // Console.WriteLine("{0}.Thing2Count = {1}", b, GMC2.Geometry[g].Thing2Count);
                        // Console.WriteLine("{0}.Thing3Count = {1}", b, GMC2.Geometry[g].Thing3Count);
                        // Console.WriteLine("{0}.UnkCount = {1}\r\n", b, GMC2.Geometry[g].UnkCount);

                        GMC2.Geometry[g].UnkShort1 = f.ReadUInt16();
                        GMC2.Geometry[g].UnkShort2 = f.ReadUInt16();
                        GMC2.Geometry[g].UnkShort3 = f.ReadUInt16();
                        GMC2.Geometry[g].UnkShort4 = f.ReadUInt16();

                        //Console.WriteLine("{0}.UnkShort1 = 0x{1:X4}", b, GMC2.Geometry[g].UnkShort1);
                        //Console.WriteLine("{0}.UnkShort2 = 0x{1:X4}", b, GMC2.Geometry[g].UnkShort2);
                        //Console.WriteLine("{0}.UnkShort3 = 0x{1:X4}", b, GMC2.Geometry[g].UnkShort3);
                        //Console.WriteLine("{0}.UnkShort4 = 0x{1:X4}\r\n", b, GMC2.Geometry[g].UnkShort4);

                        fs.Seek(0x1C, SeekOrigin.Current);

                        GMC2.Geometry[g].UnkOffset = f.ReadUInt32();

                        //Console.WriteLine("{0}.UnkOffset = 0x{1:X}\r\n", b, GMC2.Geometry[g].UnkOffset);

                        GMC2.Geometry[g].Thing1Entries = new List<GEO2Block.Thing1>(GMC2.Geometry[g].Thing1Count);
                        GMC2.Geometry[g].Thing2Entries = new List<GEO2Block.Thing2>(GMC2.Geometry[g].Thing2Count);
                        GMC2.Geometry[g].Thing3Entries = new List<GEO2Block.Thing3>(GMC2.Geometry[g].Thing3Count);

                        fs.Seek(GMC2.Geometry[g].Offset + 0x40, SeekOrigin.Begin);

                        for (int t1 = 0; t1 <= GMC2.Geometry[g].Thing1Count - 1; t1++)
                        {
                            var t = String.Format("Thing1[{0}]", t1);

                            GMC2.Geometry[g].Thing1Entries.Insert(t1, new GEO2Block.Thing1());

                            GEO2Block.Thing1 Thing1 = GMC2.Geometry[g].Thing1Entries[t1];

                            // Console.WriteLine("---------------------------\r\n" +
                            //     "{0} @ 0x{1:X}\r\n", t, fs.Position);

                            Thing1.UPad1 = f.ReadSingle();
                            Thing1.UPad2 = f.ReadSingle();
                            Thing1.UPad3 = f.ReadSingle();
                            Thing1.UPad4 = f.ReadSingle();

                            Thing1.T2Count = f.ReadUInt32();
                            Thing1.T2Offset = f.ReadUInt32();
                            Thing1.Unknown = f.ReadUInt32();

                            fs.Seek(0x4, SeekOrigin.Current);

                            // Console.WriteLine("{0}.UPad1 = {1:F}", t, Thing1.UPad1);
                            // Console.WriteLine("{0}.UPad2 = {1:F}", t, Thing1.UPad2);
                            // Console.WriteLine("{0}.UPad3 = {1:F}", t, Thing1.UPad3);
                            // Console.WriteLine("{0}.UPad4 = {1:F}\r\n", t, Thing1.UPad4);

                            // Console.WriteLine("{0}.T2Count = {1}", t, Thing1.T2Count);
                            // Console.WriteLine("{0}.T2Offset = 0x{1:X}", t, Thing1.T2Offset);
                            // Console.WriteLine("{0}.Unknown = 0x{1:X}\r\n", t, Thing1.Unknown);
                        }

                        for (int t2 = 0; t2 <= GMC2.Geometry[g].Thing2Count - 1; t2++)
                        {
                            var t = String.Format("Thing2[{0}]", t2);

                            GMC2.Geometry[g].Thing2Entries.Insert(t2, new GEO2Block.Thing2());

                            GEO2Block.Thing2 Thing2 = GMC2.Geometry[g].Thing2Entries[t2];

                            // Console.WriteLine("---------------------------\r\n" +
                            //     "{0} @ 0x{1:X}\r\n", t, fs.Position);

                            Thing2.UnkGUID = f.ReadUInt32();
                            Thing2.T3Offset = f.ReadUInt32();
                            Thing2.Unk2 = f.ReadUInt32();

                            // Console.WriteLine("{0}.UnkGUID = 0x{1:X}", t, Thing2.UnkGUID);
                            // Console.WriteLine("{0}.T3Offset = 0x{1:X}", t, Thing2.T3Offset);
                            // Console.WriteLine("{0}.Unk2 = 0x{1:X}\r\n", t, Thing2.Unk2);
                        }

                        for (int t3 = 0; t3 <= GMC2.Geometry[g].Thing3Count - 1; t3++)
                        {
                            var t = String.Format("Thing2[{0}]", t3);

                            GMC2.Geometry[g].Thing3Entries.Insert(t3, new GEO2Block.Thing3());

                            GEO2Block.Thing3 Thing3 = GMC2.Geometry[g].Thing3Entries[t3];

                            // Console.WriteLine("---------------------------\r\n" +
                            //     "{0} @ 0x{1:X}\r\n", t, fs.Position);

                            Thing3.UFloat1 = f.ReadSingle();
                            Thing3.UFloat2 = f.ReadSingle();
                            Thing3.UFloat3 = f.ReadSingle();

                            Thing3.TexID = f.ReadUInt16();
                            Thing3.TexSrc = f.ReadUInt16();

                            Thing3.UFloat4 = f.ReadSingle();
                            Thing3.UFloat5 = f.ReadSingle();
                            Thing3.UFloat6 = f.ReadSingle();
                            
                            Thing3.UnkPad = f.ReadUInt32();

                            Thing3.Unk1 = f.ReadUInt16();

                            Thing3.UnkFlag1 = f.ReadByte();
                            Thing3.UnkFlag2 = f.ReadByte();

                            Thing3.T4Offset = f.ReadUInt32();

                            fs.Seek(0x8, SeekOrigin.Current);

                            // Console.WriteLine("{0}.UFloat1 = {1:F}", t, Thing3.UFloat1);
                            // Console.WriteLine("{0}.UFloat2 = {1:F}", t, Thing3.UFloat2);
                            // Console.WriteLine("{0}.UFloat3 = {1:F}\r\n", t, Thing3.UFloat3);

                            // Console.WriteLine("{0}.TexID = {1}", t, Thing3.TexID);
                            // Console.WriteLine("{0}.TexSrc = {1}\r\n", t,
                            //     ((Enum.IsDefined(typeof(TextureSource), (TextureSource)Thing3.TexSrc))
                            //     ? ((TextureSource)Thing3.TexSrc).ToString()
                            //     : Thing3.TexSrc.ToString("X")));

                            // Console.WriteLine("{0}.UFloat4 = {1:F}", t, Thing3.UFloat4);
                            // Console.WriteLine("{0}.UFloat5 = {1:F}", t, Thing3.UFloat5);
                            // Console.WriteLine("{0}.UFloat6 = {1:F}\r\n", t, Thing3.UFloat6);

                            // Console.WriteLine("{0}.UnkPad = {1:X}\r\n", t, Thing3.UnkPad);

                            // Console.WriteLine("{0}.Unk1 = {1:X}\r\n", t, Thing3.Unk1);

                            // Console.WriteLine("{0}.UnkFlag1 = {1:X}", t, Thing3.UnkFlag1);
                            // Console.WriteLine("{0}.UnkFlag2 = {1:X}\r\n", t, Thing3.UnkFlag2);

                            // Console.WriteLine("{0}.T4Offset = {1:X}", t, Thing3.T4Offset);
                        }

                        // Console.WriteLine();
                        // Console.WriteLine("\r\nBlock[{0}]: Finished reading up until 0x{1:X}\r\n", g, fs.Position);

                        fs.Seek(holdPos, SeekOrigin.Begin);
                    }

                    Console.WriteLine("\r\nDone reading GEO2 entries.");
                    
                    fs.Seek(GMC2.TSC2.Offset, SeekOrigin.Begin);

                    if (f.ReadUInt32() != (uint)BlockType.TSC2)
                    {
                        Console.WriteLine("Bad TSC2 header!");
                        return;
                    }

                    Console.WriteLine("Reading TSC2 data @ 0x{0:X}", fs.Position);

                    GMC2.TSC2.MatCount = f.ReadUInt16();
                    GMC2.TSC2.SubMatOffsetCount = f.ReadUInt16();
                    GMC2.TSC2.SubMatCount = f.ReadUInt16();
                    GMC2.TSC2.TexInfoOffsetCount = f.ReadUInt16();
                    GMC2.TSC2.TexInfoCount = f.ReadUInt16();

                    // skip version, padding
                    fs.Seek(sizeof(ushort) + sizeof(uint), SeekOrigin.Current);

                    for (int m = 0; m < GMC2.TSC2.MatCount; m++)
                    {
                        GMC2.TSC2.Materials.Insert(m, new TSC2Block.Material());

                        var mat = GMC2.TSC2.Materials[m];

                        mat.SubMaterialsCount = f.ReadUInt32();

                        // skip junk
                        fs.Seek(sizeof(uint) * 2, SeekOrigin.Current);

                        mat.SubMaterialsOffset = f.ReadUInt32();
                    }

                    for (int s = 0; s < GMC2.TSC2.SubMatOffsetCount; s++)
                    {
                        GMC2.TSC2.SubMaterials.Insert(s, new TSC2Block.SubMaterial());

                        var subMat = GMC2.TSC2.SubMaterials[s];

                        subMat.Offset = f.ReadUInt32();
                    }

                    for (int s = 0; s < GMC2.TSC2.SubMatCount; s++)
                    {
                        var subMat = GMC2.TSC2.SubMaterials[s];

                        subMat.Unk1 = f.ReadUInt16();
                        subMat.Unk2 = f.ReadUInt16();

                        fs.Seek(sizeof(uint), SeekOrigin.Current);

                        subMat.TexInfoOffset = f.ReadUInt32();
                    }

                    for (int t = 0; t < GMC2.TSC2.TexInfoOffsetCount; t++)
                    {
                        GMC2.TSC2.TexturesInfo.Insert(t, new TSC2Block.TextureInfo());

                        var texInfo = GMC2.TSC2.TexturesInfo[t];

                        texInfo.Offset = f.ReadUInt32();
                    }

                    long hold = fs.Position;

                    byte[] TSC2Data = new byte[fs.Length - GMC2.TSC2.Offset];

                    Console.WriteLine("0x{0:X}", GMC2.TSC2.Offset);
                    Console.WriteLine("0x{0:X}", TSC2Data.Length);

                    fs.Seek(GMC2.TSC2.Offset, SeekOrigin.Begin);
                    fs.Read(TSC2Data, 0, TSC2Data.Length);

                    BMPViewer viewer = new BMPViewer();

                    fs.Seek(hold, SeekOrigin.Begin);
                    Console.WriteLine("Seeking to 0x{0:X}", fs.Position);

                    for (int t = 0; t < GMC2.TSC2.TexInfoCount; t++)
                    {
                        var texInfo = GMC2.TSC2.TexturesInfo[t];

                        Console.WriteLine("TextureInfo @ 0x{0:X}", fs.Position);

                        texInfo.UnkFloat1 = f.ReadSingle();

                        texInfo.Unk1 = f.ReadUInt16();
                        texInfo.Unk2 = f.ReadUInt16();

                        texInfo.Flags = f.ReadUInt16();
                        texInfo.UnkFlags = f.ReadUInt16();

                        texInfo.Width = f.ReadUInt16();
                        texInfo.Height = f.ReadUInt16();

                        texInfo.UnkSize = f.ReadUInt32();
                        texInfo.TexDataOffset = f.ReadUInt32();

                        // skip padding
                        fs.Seek(sizeof(uint), SeekOrigin.Current);

                        texInfo.PaletteOffset = f.ReadUInt32();
                        texInfo.TextureOffset = f.ReadUInt32();
                        texInfo.TexUnknown = f.ReadUInt32();

                        if (texInfo.UnkFlags == 0x3 || texInfo.Flags == 1541)
                            fs.Seek(sizeof(uint) * 2, SeekOrigin.Current);
                        
                        Console.WriteLine(
                            "===================\n" +
                            "Flags: {0}\n" +
                            "Unk: {1}\n" +
                            "Width: {2}\n" +
                            "Height: {3}\n" +
                            "PaletteOffset: 0x{4:X}\n" +
                            "TextureOffset: 0x{5:X}\r\n",
                            texInfo.Flags, texInfo.UnkFlags, texInfo.Width, texInfo.Height, texInfo.PaletteOffset, texInfo.TextureOffset
                        );

                        if (texInfo.Width >= 128 && texInfo.Height >= 128 && texInfo.Flags != 771)
                        {

                            Texture8bpp Texture = new Texture8bpp(texInfo, TSC2Data);

                            GMC2.TSC2.Textures.Add(Texture);

                            viewer.AddImage(Texture.Bitmap);
                        }
                    }

                    // for (int i = 0; i < GMC2.TSC2.TexInfoCount; i++)
                    // {
                    //     TSC2Block.TextureInfo texInfo = GMC2.TSC2.TexturesInfo[i];
                    // 
                    //     
                    // }

                    viewer.Init();
                    Application.Run(viewer);

                    Console.WriteLine("Done collecting TSC2 data, stopped @ 0x{0:X}", fs.Position);
                }
            }
        }
    }
}
