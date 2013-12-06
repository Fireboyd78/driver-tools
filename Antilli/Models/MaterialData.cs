using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

using DSCript;

using FreeImageAPI;

namespace Antilli.Models
{
    public class PCMPTextureInfo
    {
        public uint BaseOffset { get; set; }

        public byte Unk1 { get; set; }
        public byte Unk2 { get; set; }
        public byte Unk3 { get; set; }
        public byte Unk4 { get; set; }

        public uint CRC32 { get; set; }
        public uint Offset { get; set; }
        public uint Size { get; set; }
        public uint Type { get; set; }

        public ushort Width { get; set; }
        public ushort Height { get; set; }

        public uint Unk5 { get; set; }
        public uint Unk6 { get; set; }

        public byte[] Buffer { get; set; }

        public void ExportFile(string filename)
        {
            //SaveFileDialog saveFile = new SaveFileDialog() {
            //    AddExtension = true,
            //    CheckPathExists = true,
            //    DefaultExt = ".dds",
            //    Filter = "DDS Image|*.dds",
            //    OverwritePrompt = true,
            //    RestoreDirectory = true,
            //    ValidateNames = true
            //};

            //DialogResult result = saveFile.ShowDialog();
            //
            //if (result == DialogResult.OK)
            //{
            //    
            //}

            using (MemoryStream f = new MemoryStream(Buffer))
            {
                if (!File.Exists(filename))
                    f.WriteTo(File.Create(filename, (int)f.Length));
            }
        }

        public Bitmap GetBitmap(bool useAlpha)
        {
            using (MemoryStream stream = new MemoryStream(Buffer))
            {
                FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_DDS;

                try
                {
                    FIBITMAP bmap = FreeImage.LoadFromStream(stream, FREE_IMAGE_LOAD_FLAGS.RAW_DISPLAY, ref format);

                    bmap = (useAlpha) ? FreeImage.ConvertTo32Bits(bmap) : FreeImage.ConvertTo24Bits(bmap);

                    return FreeImage.GetBitmap(bmap);
                }
                catch (System.BadImageFormatException)
                {
                    return null;
                }
            }
        }

        public BitmapSource GetBitmapSource(bool useAlpha)
        {
            Bitmap bitmap = GetBitmap(useAlpha);

            if (bitmap == null)
                return null;
            
            BitmapSource bitSrc = bitmap.ToBitmapSource();

            bitmap.Dispose();
            bitmap = null;

            return bitSrc;
        }
    }

    public class PCMPSubMaterial
    {
        public uint BaseOffset { get; set; }

        public uint Unk1 { get; set; }

        public ushort Unk2 { get; set; }
        public ushort Unk3 { get; set; }

        public List<PCMPTextureInfo> Textures { get; set; }

        public PCMPSubMaterial()
        {
            Textures = new List<PCMPTextureInfo>();
        }
    }

    public class PCMPMaterial
    {
        public uint Unk1 { get; set; }
        public uint Unk2 { get; set; }
        public uint Unk3 { get; set; }
        public uint Unk4 { get; set; }

        public List<PCMPSubMaterial> SubMaterials { get; set; }

        public PCMPMaterial()
        {
            SubMaterials = new List<PCMPSubMaterial>();
        }
    }

    public class PCMPData
    {
        public const uint Magic = 0x504D4350; // 'PCMP'

        public List<PCMPMaterial> Materials { get; set; }
        public List<PCMPSubMaterial> SubMaterials { get; set; }
        public List<PCMPTextureInfo> Textures { get; set; }

        public PCMPData(int numMaterials, int numSubMaterials, int numTextures)
        {
            Materials = new List<PCMPMaterial>(numMaterials);
            SubMaterials = new List<PCMPSubMaterial>(numSubMaterials);
            Textures = new List<PCMPTextureInfo>(numTextures);
        }
    }
}
