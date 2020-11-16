using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DSCript
{
    public enum DDSFlags : int
    {
        Caps           = 0x1,
        Height         = 0x2,
        Width          = 0x4,
        Pitch          = 0x8,
        PixelFormat    = 0x1000,
        MipMapCount    = 0x20000,
        LinearSize     = 0x80000,
        Depth          = 0x800000,
    }

    public enum DDSPixelFormatFlags : int
    {
        AlphaPixels         = 0x0001,
        Alpha               = 0x0002,
        FourCC              = 0x0004,
        P4                  = 0x0008,
        PP8                 = 0x0010,
        P8                  = 0x0020,
        Rgb                 = 0x0040,
        Compressed          = 0x0080,
        RgbToYUV            = 0x0100,
        YUV                 = 0x0200,
        ZBuffer             = 0x0400,
        P1                  = 0x0800,
        P2                  = 0x1000,
        ZPixels             = 0x2000,
        StencilBuffer       = 0x4000,
        AlphaPreMultiplier  = 0x8000,
    }

    public enum D3DResourceType : int
    {
        Invalid         = -1,

        None            = 0,

        Surface         = 1,
        Volume          = 2,
        Texture         = 3,
        VolumeTexture   = 4,
        CubeTexture     = 5,
        VertexBuffer    = 6,
        IndexBuffer     = 7,

        PushBuffer      = 8,
        Palette         = 9,
        Fixup           = 10,
    }

    public enum D3DFormat : int
    {
        UNKNOWN         = -1,

        /* Swizzled formats */

        A8R8G8B8        = 0x06,
        X8R8G8B8        = 0x07,
        R5G6B5          = 0x05,
        R6G5B5          = 0x27,
        X1R5G5B5        = 0x03,
        A1R5G5B5        = 0x02,
        A4R4G4B4        = 0x04,
        A8              = 0x19,
        A8B8G8R8        = 0x3A,
        B8G8R8A8        = 0x3B,
        R4G4B4A4        = 0x39,
        R5G5B5A1        = 0x38,
        R8G8B8A8        = 0x3C,
        R8B8            = 0x29,
        G8B8            = 0x28,

        P8              = 0x0B,

        L8              = 0x00,
        A8L8            = 0x1A,
        AL8             = 0x01,
        L16             = 0x32,

        V8U8            = 0x28,
        L6V5U5          = 0x27,
        X8L8V8U8        = 0x07,
        Q8W8V8U8        = 0x3A,
        V16U16          = 0x33,

        D16             = 0x2C,
        D24S8           = 0x2A,
        F16             = 0x2D,
        F24S8           = 0x2B,

        /* YUV formats */

        YUY2            = 0x24,
        UYVY            = 0x25,

        /* Compressed formats */

        DXT1            = 0x0C,
        DXT2            = 0x0D,
        DXT3            = 0x0E,
        DXT5            = 0x0F,

        /* Linear formats */

        Linear_A1R5G5B5 = 0x10,
        Linear_A4R4G4B4 = 0x1D,
        Linear_A8       = 0x1F,
        Linear_A8B8G8R8 = 0x3F,
        Linear_A8R8G8B8 = 0x12,
        Linear_B8G8R8A8 = 0x40,
        Linear_G8B8     = 0x17,
        Linear_R4G4B4A4 = 0x3E,
        Linear_R5G5B5A1 = 0x3D,
        Linear_R5G6B5   = 0x11,
        Linear_R6G5B5   = 0x37,
        Linear_R8B8     = 0x16,
        Linear_R8G8B8A8 = 0x41,
        Linear_X1R5G5B5 = 0x1C,
        Linear_X8R8G8B8 = 0x1E,

        Linear_A8L8     = 0x20,
        Linear_AL8      = 0x1B,
        Linear_L16      = 0x35,
        Linear_L8       = 0x13,

        Linear_V16U16   = 0x36,
        Linear_V8U8     = 0x17,
        Linear_L6V5U5   = 0x37,
        Linear_X8L8V8U8 = 0x1E,
        Linear_Q8W8V8U8 = 0x12,

        Linear_D24S8    = 0x2E,
        Linear_F24S8    = 0x2F,
        Linear_D16      = 0x30,
        Linear_F16      = 0x31,

        VertexData      = 100,
        Index16         = 101,

        FORCE_DWORD     = 0x7fffffff
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x14)]
    public struct D3DResource
    {
        public int Common;
        public int Data;
        public int Lock;
        public int Format;
        public int Size;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x20)]
    public struct D3DSurfaceDesc
    {
        public int Format;
        public D3DResourceType Type;
        public int Usage;
        public int Pool;
        public int Size;

        public int MultiSampleType;

        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x20)]
    public struct DDSPixelFormat
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(DDSPixelFormat));

        public int Size;
        public DDSPixelFormatFlags Flags;
        public int FourCC;
        public int RGBBitCount;
        public int RBitMask;
        public int GBitMask;
        public int BBitMask;
        public int ABitMask;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x7C)]
    public unsafe struct DDSHeader
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(DDSHeader));

        public int Size;
        public DDSFlags Flags;
        public int Height;
        public int Width;
        public int PitchOrLinearSize;
        public int Depth;
        public int MipMapCount;

        private fixed int Reserved1[11];

        public DDSPixelFormat PixelFormat;

        public int Caps;
        public int Caps2;
        public int Caps3;
        public int Caps4;

        private int Reserved2;
    }

    public static class DDSUtils
    {
        static class Swizzlers
        {
            // source: https://github.com/aap/librw/blob/master/src/d3d/xbox.cpp#L613
            // special thanks to 'aap', a very talented fellow indeed!
            public static class aap
            {
                public static byte[] Unswizzle(byte[] src, int w, int h, int bpp)
                {
                    int maskU = 0;
                    int maskV = 0;
                    
                    int i = 1;
                    int j = 1;
                    int c = 0;

                    do
                    {
                        c = 0;
                        if (i < w)
                        {
                            maskU |= j;
                            j <<= 1;
                            c = j;
                        }
                        if (i < h)
                        {
                            maskV |= j;
                            j <<= 1;
                            c = j;
                        }
                        i <<= 1;
                    } while (c != 0);

                    var dst = new byte[src.Length];

                    int u = 0;
                    int v = 0;

                    for (int y = 0; y < h; y++)
                    {
                        u = 0;
                        for (int x = 0; x < w; x++)
                        {
                            var srcIdx = (u | v) * bpp;
                            var dstIdx = (y * w + x) * bpp;

                            Buffer.BlockCopy(src, srcIdx, dst, dstIdx, bpp);

                            u = (u - maskU) & maskU;
                        }
                        v = (v - maskV) & maskV;
                    }

                    return dst;
                }
            }

            // source: https://gtaforums.com/topic/213907-unswizzle-tool/?do=findComment&comment=3172924
            // originally written in VB.NET by 'aru' of GTAForums :)
            public static class aru
            {
                private static void UnswizBlock(ref byte[] srcBuf,
                    ref byte[] dstBuf,
                    ref int srcOffset,
                    int dstOffset,
                    int width,
                    int height,
                    int stride)
                {
                    if (srcOffset >= srcBuf.Length)
                        return;

                    if ((width < 2) || (height < 2))
                    {
                        var length = (width * height);

                        Buffer.BlockCopy(dstBuf, dstOffset, srcBuf, srcOffset, length);

                        dstOffset += length;
                    }
                    else if ((width == 2) && (height == 2))
                    {
                        // unswizzle block

                        dstBuf[dstOffset] = srcBuf[srcOffset];
                        dstBuf[dstOffset + 1] = srcBuf[srcOffset + 1];
                        dstBuf[dstOffset + stride] = srcBuf[srcOffset + 2];
                        dstBuf[dstOffset + stride + 1] = srcBuf[srcOffset + 3];

                        srcOffset += 4;
                    }
                    else
                    {
                        // break into 4 blocks and reprocess

                        var w = width / 2;
                        var h = height / 2;

                        UnswizBlock(ref srcBuf, ref dstBuf, ref srcOffset, dstOffset, w, h, stride);
                        UnswizBlock(ref srcBuf, ref dstBuf, ref srcOffset, dstOffset + w, w, h, stride);
                        UnswizBlock(ref srcBuf, ref dstBuf, ref srcOffset, dstOffset + (stride * h), w, h, stride);
                        UnswizBlock(ref srcBuf, ref dstBuf, ref srcOffset, dstOffset + (stride * h) + w, w, h, stride);
                    }
                }

                public static byte[] Unswizzle(byte[] data, int width, int height, ref int mipLevels)
                {
                    var minMax = (height > width) ? height : width;

                    mipLevels = (int)Math.Log(minMax) / (int)(Math.Log(2) + 1);

                    var offset = 0;
                    var length = data.Length;

                    var result = new byte[length];

                    var w = width;
                    var h = height;

                    for (int i = 1; i <= mipLevels; i++)
                    {
                        UnswizBlock(ref data, ref result, ref offset, offset, w, h, w);

                        w /= 2;
                        h /= 2;

                        if (offset >= length)
                            break;
                    }

                    return result;
                }
            }
        }

        public static readonly byte[] DDSMagic = { 0x44, 0x44, 0x53, 0x20 };

        // ugly pink 8x8 texture (DXT1)
        private static readonly byte[] NullTex = {
            0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 0x07, 0x10, 0x08, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x44, 0x58, 0x54, 0x31, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0xF8, 0x17, 0xF8,
            0xFF, 0xFF, 0xFF, 0xFF, 0x18, 0xF8, 0x17, 0xF8, 0xFF, 0xFF, 0xFF, 0xFF,
            0x18, 0xF8, 0x17, 0xF8, 0xFF, 0xFF, 0xFF, 0xFF, 0x18, 0xF8, 0x17, 0xF8,
            0xFF, 0xFF, 0xFF, 0xFF
        };

        private static readonly byte[] RGBHeader = {
            0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 0x07, 0x10, 0x08, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00,
            0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public static byte[] GetNullTex()
        {
            var result = new byte[NullTex.Length];
            Buffer.BlockCopy(NullTex, 0, result, 0, result.Length);

            return result;
        }

        public static byte[] MakeRGBATexture(Vector4 color)
        {
            var pixels = new byte[0x100];

            var r = (byte)(color.X * 255.999f);
            var g = (byte)(color.Y * 255.999f);
            var b = (byte)(color.Z * 255.999f);
            var a = (byte)(color.W * 255.999f);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i + 0] = b;
                pixels[i + 1] = g;
                pixels[i + 2] = r;
                pixels[i + 3] = a;
            }

            var result = new byte[RGBHeader.Length + pixels.Length];

            Buffer.BlockCopy(RGBHeader, 0, result, 0, RGBHeader.Length);
            Buffer.BlockCopy(pixels, 0, result, RGBHeader.Length, pixels.Length);

            return result;
        }

        public static unsafe bool GetHeaderInfo(Stream stream, ref DDSHeader header)
        {
            if (stream.ReadInt32() == 0x20534444)
            {
                var length = DDSHeader.SizeOf;

                var data = new byte[length];
                var ptr = __makeref(header);

                stream.Read(data, 0, length);
                Marshal.Copy(data, 0, *(IntPtr*)&ptr, length);

                return true;
            }

            // not a valid DDS texture
            return false;
        }

        public static bool GetHeaderInfo(byte[] buffer, ref DDSHeader header)
        {
            using (var ms = new MemoryStream(buffer))
                return GetHeaderInfo(ms, ref header);
        }

        // source: https://github.com/dstien/gameformats/blob/master/mm3/decdds/
        public static int GetDataSize(ref DDSHeader header)
        {
            // Find data length for texture/cube/volume with mips at given dimentions/bpp.
            var images = 1;

            if ((header.Caps2 & 0x200) != 0)
            {
                images = 0;

                // check bits 10-15 (0x400-0x8000)
                for (int n = 0; n < 6; n++)
                {
                    var mask = (1 << (10 + n));

                    if ((header.Caps2 & mask) != 0)
                        ++images;
                }
            }

            var imglen = 0;
            var mips = (header.MipMapCount != 0) ? header.MipMapCount : 1;

            var fourCC = header.PixelFormat.FourCC;
            var texel = ((fourCC & 0xFFFFFF) == 0x545844); // 'DXT' texture
            var type = ((fourCC >> 24) - 0x30);
            var bpp = -1;

            if (texel)
            {
                switch (type)
                {
                    case 1:
                        bpp = 4;
                        break;
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        bpp = 8;
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid texture format '{fourCC:X8}'.");
                }
            }
            else
            {
                bpp = header.PixelFormat.RGBBitCount;
            }

            var volume = (header.Caps2 & 0x200000) != 0;

            for (int mip = 0; mip < mips; mip++)
            {
                var width = Math.Max(header.Width >> mip, 1);
                var height = Math.Max(header.Height >> mip, 1);

                var depth = (header.Depth >> mip);

                var slices = (volume && (depth != 0)) ? depth : 1;

                if (texel)
                {
                    imglen += ((width + 3) / 4) * ((height + 3) / 4) * bpp * 2 * slices * images;
                }
                else
                {
                    imglen += (width * height * bpp) / 8 * slices * images;
                }
            }

            return imglen;
        }

        public static int GetTextureType(int fourCC)
        {
            switch (fourCC)
            {
                case 0x31545844: return 1;
                case 0x32545844: return 2;
                case 0x33545844: return 3;
                case 0x35545844: return 5;
                // RGB[:A] (non-spec)
                case 0:
                    return 128;
                default:
                    return 0;
            }
        }

        public static D3DResourceType GetResourceType(ref D3DResource resource, PlatformType platform)
        {
            if (platform == PlatformType.Xbox)
            {
                    var code = ((resource.Common & 0x70000) >> 16);

                    switch (code)
                    {
                        case 0:
                            return D3DResourceType.VertexBuffer;
                        case 1:
                            return D3DResourceType.IndexBuffer;
                        case 2:
                            return D3DResourceType.PushBuffer;
                        case 3:
                            return D3DResourceType.Palette;
                        case 4:
                            if ((resource.Format & 0x4) != 0)
                                return D3DResourceType.CubeTexture;

                            if ((resource.Format & 0xF0) > 0x20)
                                return D3DResourceType.VolumeTexture;

                            return D3DResourceType.Texture;
                        case 5:
                            if ((resource.Format & 0xF0) > 0x20)
                                return D3DResourceType.Volume;

                            return D3DResourceType.Surface;
                        case 6:
                            return D3DResourceType.Fixup;
                    }       
            }

            return D3DResourceType.Invalid;
        }

        private static readonly byte[] TextureFormats = {
            0x09, 0x09, 0x11, 0x91,
            0x11, 0x91, 0xA1, 0xA1,
            0x00, 0x00, 0x00, 0x09,
            0x04, 0x00, 0x08, 0x08,
            0x12, 0x92, 0xA2, 0x8A,
            0x00, 0x00, 0x12, 0x92,
            0x00, 0x09, 0x11, 0x0A,
            0x92, 0x12, 0xA2, 0x0A,
            0x12, 0x00, 0x00, 0x00,
            0x12, 0x12, 0x00, 0x11,
            0x11, 0x11, 0x61, 0x61,
            0x51, 0x51, 0x62, 0x62,
            0x52, 0x52, 0x11, 0x21,
            0x00, 0x12, 0x00, 0x12,
            0x11, 0x11, 0x21, 0x21,
            0x21, 0x12, 0x12, 0x22,
            0x22, 0x22, 0x00, 0x00,
        };

        public static bool IsCompressedFormat(D3DFormat format)
        {
            switch (format)
            {
                case D3DFormat.DXT1:
                case D3DFormat.DXT2:
                /* NOTE: 'DXT3' is uncompressed! */
                case D3DFormat.DXT5:
                    return true;
            }

            return false;
        }

        public static int GetFourCC(D3DFormat format)
        {
            switch (format)
            {
                case D3DFormat.DXT1: return 0x31545844;
                case D3DFormat.DXT2: return 0x32545844;
                case D3DFormat.DXT3: return 0x33545844;
                case D3DFormat.DXT5: return 0x35545844;
            }

            return 0;
        }

        public static bool EncodeHeader(D3DFormat format, int width, int height, int mipmaps, ref DDSHeader header)
        {
            header.Size = DDSHeader.SizeOf;
            header.Flags = DDSFlags.Caps | DDSFlags.Width | DDSFlags.Height | DDSFlags.PixelFormat;
            header.Width = width;
            header.Height = height;

            header.PixelFormat.Size = DDSPixelFormat.SizeOf;
            header.Caps = 0x1000;

            // calculate mipmaps?
            if (mipmaps == -1)
            {
                var minMax = (height > width) ? height : width;
                var mipLevels = (int)Math.Log(minMax) / (int)(Math.Log(2) + 1);

                mipmaps = (mipLevels > 0) ? mipLevels : 0;
            }

            if (mipmaps > 0)
            {
                header.Flags |= DDSFlags.MipMapCount;
                header.MipMapCount = mipmaps;

                header.Caps |= 0x400000;
                header.Caps |= 0x8;
            }

            switch (format)
            {
                case D3DFormat.P8:
                    header.PixelFormat.Flags = DDSPixelFormatFlags.Rgb | DDSPixelFormatFlags.AlphaPixels;
                    header.PixelFormat.RGBBitCount = 32;

                    header.PixelFormat.ABitMask = (0xFF << 24);
                    header.PixelFormat.RBitMask = (0xFF << 16);
                    header.PixelFormat.GBitMask = (0xFF << 8);
                    header.PixelFormat.BBitMask = (0xFF << 0);

                    header.Caps |= 0x2; // ???
                    break;

                case D3DFormat.DXT1:
                case D3DFormat.DXT2:
                case D3DFormat.DXT3:
                case D3DFormat.DXT5:
                    header.PixelFormat.Flags = DDSPixelFormatFlags.FourCC;
                    header.PixelFormat.FourCC = GetFourCC(format);

                    break;

                default:
                    return false;
            }

            return true;
        }

        public static bool EncodeHeader(D3DFormat format, int width, int height, ref DDSHeader header)
        {
            return EncodeHeader(format, width, height, -1, ref header);
        }

        public static bool EncodeSize(D3DFormat format, int width, int height, ref DDSHeader header)
        {
            switch (format)
            {
                case D3DFormat.P8:
                    // RGB textures don't define this
                    header.PitchOrLinearSize = 0;
                    break;

                case D3DFormat.DXT1:
                case D3DFormat.DXT2:
                case D3DFormat.DXT3:
                case D3DFormat.DXT5:
                    header.Flags |= DDSFlags.LinearSize;
                    header.PitchOrLinearSize = GetDataSize(ref header);
                    break;

                default:
                    return false;
            }

            return true;
        }

        public static byte[] EncodeTexture(byte[] data, ref DDSHeader header)
        {
            var size = data.Length;
            var buffer = new byte[size + DDSHeader.SizeOf + 4];

            var hdr = GCHandle.Alloc(header, GCHandleType.Pinned);
            var dds = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            var ptr = dds.AddrOfPinnedObject();

            try
            {
                Marshal.WriteInt32(ptr, 0x20534444);

                Marshal.Copy(hdr.AddrOfPinnedObject(), buffer, 4, DDSHeader.SizeOf);
                ptr += (DDSHeader.SizeOf + 4);

                Marshal.Copy(data, 0, ptr, size);
            }
            finally
            {
                dds.Free();
                hdr.Free();
            }

            return buffer;
        }

        public static byte[] EncodeTexture(D3DFormat format, int width, int height, int mipmaps, byte[] data)
        {
            var header = default(DDSHeader);

            if (!EncodeHeader(format, width, height, mipmaps, ref header))
                throw new InvalidOperationException($"Unhandled pixel format '{format.ToString()}'");

            EncodeSize(format, width, height, ref header);

            return EncodeTexture(data, ref header);
        }

        public static byte[] EncodeTexture(D3DFormat format, int width, int height, byte[] data)
        {
            return EncodeTexture(format, width, height, -1, data);
        }

        public static bool GetSurfaceDesc(ref D3DResource surface, PlatformType platform, ref D3DSurfaceDesc desc)
        {
            var type = GetResourceType(ref surface, platform);

            if (type == D3DResourceType.Invalid)
                throw new InvalidOperationException("GetSurfaceDesc - Invalid platform!");

            var format = (surface.Format >> 8) & 0xFF;

            if (format >= TextureFormats.Length)
                throw new InvalidOperationException("GetSurfaceDesc - What the fuck did you do?!");

            desc.Format = format;
            desc.Type = type;
            desc.Usage = 0;

            var flags = (sbyte)TextureFormats[format];

            if (flags >= 0)
            {
                if ((flags & 0x40) != 0)
                    desc.Usage = 2;
            }
            else
            {
                desc.Usage = 1;
            }

            return false;
        }

        public static int GetBytesPerPixel(D3DFormat format)
        {
            switch (format)
            {
                case D3DFormat.P8:
                    return 4;

                default:
                    throw new InvalidOperationException($"GetBytesPerPixel: {format.ToString()} not implemented.");
            }
        }

        public static bool HasPalette(D3DFormat format)
        {
            switch (format)
            {
                case D3DFormat.P8:
                    return true;
            }

            return false;
        }

        public static byte[] GetPalette(D3DFormat format, Stream paletteIO, int width, int height)
        {
            switch (format)
            {
                case D3DFormat.P8:
                    var palette = new byte[256 * 4];
                    paletteIO.Read(palette, 0, palette.Length);

                    return palette;
            }

            return null;
        }

        public static byte[] Depalettize(byte[] data, int width, int height, int bpp, byte[] palette)
        {
            var pixels = Swizzlers.aap.Unswizzle(data, width, height, (bpp < 8) ? 1 : bpp / 8);

            var result = new byte[(width * bpp) * height];

            var line = 0;
            var ptr = 0;
            var stride = (width * bpp);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var idx = pixels[line + x];

                    Buffer.BlockCopy(palette, (idx * bpp), result, ptr + (x * bpp), bpp);
                }

                line += width;
                ptr += stride;
            }

            return result;
        }
    }
}
