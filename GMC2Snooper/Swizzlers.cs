using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GMC2Snooper
{
    public enum SwizzleType
    {
        Swizzle4bit,
        Swizzle8bit,
    }

    public static class Swizzlers
    {
        public static byte[] Swizzle4To32(byte[] pInTexels, int width, int height)
        {
            // this function works for the following resolutions
            // Width:       32, 64, 96, 128, any multiple of 128 smaller then or equal to 4096
            // Height:      16, 32, 48, 64, 80, 96, 112, 128, any multiple of 128 smaller then or equal to 4096

            // the texels must be uploaded as a 32bit texture
            // width_32bit = height_4bit / 2
            // height_32bit = width_4bit / 4
            // remember to adjust the mapping coordinates when
            // using a dimension which is not a power of two

            byte[] pSwizTexels = new byte[width * height / 2];

            Debug.WriteLine("SWIZZLE TIME!");

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // get the pen
                    int index = y * width + x;
                    byte uPen = (byte)((pInTexels[(index >> 1)] >> ((index & 1) * 4)) & 0xF);

                    // swizzle
                    int pageX = x & (~0x7F);
                    int pageY = y & (~0x7F);

                    int pageH = (width + 127) / 128;
                    int pageV = (height + 127) / 128;

                    int pageNumber = (pageY / 128) * pageH + (pageX / 128);

                    int page32Y = (pageNumber / pageV) * 32;
                    int page32X = (pageNumber % pageV) * 64;

                    int pageLocation = page32Y * height * 2 + page32X * 4;

                    int locX = x & 0x7F;
                    int locY = y & 0x7F;

                    int blockLocation = ((locX & (~0x1F)) >> 1) * height + (locY & (~0xF)) * 2;
                    int swapSelector = (((y + 2) >> 2) & 0x1) * 4;
                    int posY = (((y & (~3)) >> 1) + (y & 1)) & 0x7;

                    int columnLocation = posY * height * 2 + ((x + swapSelector) & 0x7) * 4;

                    int byteNum = (x >> 3) & 3; // 0, 1, 2, 3
                    int bitsSet = (y >> 1) & 1; // 0, 1

                    int offset = pageLocation + blockLocation + columnLocation + byteNum;

                    byte setPixel = (byte)(pSwizTexels[offset] & -bitsSet);
                    
                    pSwizTexels[offset] = (byte)(setPixel | (uPen << (bitsSet * 4)));
                }
            }

            return pSwizTexels;
        }

        public static byte[] Swizzle8To32(byte[] pInTexels, int width, int height)
        {
            // this function works for the following resolutions
            // Width:       any multiple of 16 smaller then or equal to 4096
            // Height:      any multiple of 4 smaller then or equal to 4096

            // the texels must be uploaded as a 32bit texture
            // width_32bit = width_8bit / 2
            // height_32bit = height_8bit / 2
            // remember to adjust the mapping coordinates when
            // using a dimension which is not a power of two

            byte[] pSwizTexels = new byte[pInTexels.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte uPen = pInTexels[y * width + x];

                    int blockLocation = (y & (~0xF)) * width + (x & (~0xF)) * 2;
                    uint swapSelector = (uint)(((y + 2) >> 2) & 0x1) * 4;
                    int posY = (((y & (~3)) >> 1) + (y & 1)) & 0x7;
                    int columnLocation = posY * width * 2 + ((x + (int)swapSelector) & 0x7)*4;

                    int byteNum = ((y >> 1) & 1) + ((x >> 2) & 2); // 0, 1, 2, 3

                    pSwizTexels[blockLocation + columnLocation + byteNum] = uPen;
                }
            }

            return pSwizTexels;
        }
        
        public static byte[] UnSwizzle4(byte[] buffer, int width, int height, int where)
        {
            // HUGE THANKS TO:
            // L33TMasterJacob for finding the information on unswizzling 4-bit textures
            // Dageron for his 4-bit unswizzling code; he's truly a genius!
            //
            // Source: https://gta.nick7.com/ps2/swizzling/unswizzle_delphi.txt

            byte[] InterlaceMatrix = {
                0x00, 0x10, 0x02, 0x12,
                0x11, 0x01, 0x13, 0x03,
            };

            int[] Matrix        = { 0, 1, -1, 0 };
            int[] TileMatrix    = { 4, -4 };

            var pixels = new byte[width * height];
            var newPixels = new byte[width * height];

            var d = 0;
            var s = where;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < (width >> 1); x++)
                {
                    var p = buffer[s++];

                    pixels[d++] = (byte)(p & 0xF);
                    pixels[d++] = (byte)(p >> 4);
                }
            }

            // not sure what this was for, but it actually causes issues
            // we can just use width directly without issues!
            //var mw = width;

            //if ((mw % 32) > 0)
            //    mw = ((mw / 32) * 32) + 32;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var oddRow = ((y & 1) != 0);

                    var num1 = (byte)((y / 4) & 1);
                    var num2 = (byte)((x / 4) & 1);
                    var num3 = (y % 4);

                    var num4 = ((x / 4) % 4);

                    if (oddRow)
                        num4 += 4;

                    var num5 = ((x * 4) % 16);
                    var num6 = ((x / 16) * 32);
                    
                    var num7 = (oddRow) ? ((y - 1) * width) : (y * width);

                    var xx = x + num1 * TileMatrix[num2];
                    var yy = y + Matrix[num3];

                    var i = InterlaceMatrix[num4] + num5 + num6 + num7;
                    var j = yy * width + xx;

                    newPixels[j] = pixels[i];
                }
            }

#if UNSWIZZLE_TO_4BIT
            var result = new byte[width * height];

            s = 0;
            d = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < (width >> 1); x++)
                    result[d++] = (byte)((newPixels[s++] & 0xF) | (newPixels[s++] << 4));
            }

# if DUMP_UNSWIZZLED_DATA
            var dumpName = "dump";
            var dumpIdx = 0;

            var dumpFile = dumpName;

            while (File.Exists(Path.Combine(Environment.CurrentDirectory, $"{dumpFile}_{dumpIdx}.dat")))
                ++dumpIdx;

            File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, $"{dumpFile}_{dumpIdx}.dat"), result);
# endif
            return result;
#else
            // return an 8-bit texture
            return newPixels;
#endif
        }

        public static byte[] UnSwizzle8(byte[] buffer, int width, int height, int where)
        {
            byte[] pSwizTexels = new byte[buffer.Length - where];
            Array.Copy(buffer, where, pSwizTexels, 0, pSwizTexels.Length);

            buffer = new byte[buffer.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int blockLocation = (y & (~0xF)) * width + (x & (~0xF)) * 2;
                    uint swapSelector = (uint)(((y + 2) >> 2) & 0x1) * 4;
                    int posY = (((y & (~3)) >> 1) + (y & 1)) & 0x7;
                    int columnLocation = posY * width * 2 + ((x + (int)swapSelector) & 0x7) * 4;

                    int byteNum = ((y >> 1) & 1) + ((x >> 2) & 2); // 0, 1, 2, 3

                    byte uPen;

                    if ((blockLocation + columnLocation + byteNum) >= pSwizTexels.Length)
                        uPen = (byte)(pSwizTexels[pSwizTexels.Length - 1]);
                    else
                        uPen = (byte)(pSwizTexels[(blockLocation + columnLocation + byteNum)]);

                    buffer[y * width + x] = (byte)uPen;
                }
            }

            return buffer;
        }

        private static int[] MakeTwiddleMap(int size)
        {
            var twiddleMap = new int[size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0, k = 1; k <= i; j++, k <<= 1)
                    twiddleMap[i] |= (i & k) << j;
            }

            return twiddleMap;
        }

        public static byte[] UnSwizzleVQ2(byte[] buffer, int width, int height, int where)
        {
            // Destination data & index
            byte[] destination = new byte[width * height * 4];
            int destinationIndex;

            // Twiddle map
            int[] twiddleMap = MakeTwiddleMap(width);

            buffer = UnSwizzle8(buffer, 32, 32, where);

#if VQ2_METHOD_A
            // Decode texture data
            for (int y = 0; y < height; y += 2)
            {
                var ty = twiddleMap[y >> 1];

                for (int x = 0; x < width; x += 2)
                {
                    var tx = twiddleMap[x >> 1];
                    var index = buffer[where + ((tx << 1) | ty)] * 4;

                    for (int x2 = 0; x2 < 2; x2++)
                    {
                        for (int y2 = 0; y2 < 2; y2++)
                        {
                            destinationIndex = (((y + y2) * width) + (x + x2)) * 4;

                            for (int i = 0; i < 4; i++)
                                destination[destinationIndex + i] = (byte)(index + i);

                            index++;
                        }
                    }
                }
            }
#elif VQ2_METHOD_B
            // Decode texture data
            for (int y = 0; y < height; y += 2)
            {
                for (int x = 0; x < width; x += 2)
                {
                    int index = (buffer[where + ((twiddleMap[x >> 1] << 1) | twiddleMap[y >> 1])]) * 4;

                    for (int x2 = 0; x2 < 2; x2++)
                    {
                        for (int y2 = 0; y2 < 2; y2++)
                        {
                            destinationIndex = ((((y + y2) * width) + (x + x2)) * 4);

                            for (int i = 0; i < 4; i++)
                            {
                                destination[destinationIndex] = (byte)(index + i);
                                destinationIndex++;
                            }

                            index++;
                        }
                    }
                }
            }
#else
            // Decode texture data
            for (int y = 0; y < height; y += 2)
            {
                for (int x = 0; x < width; x += 2)
                {
                    int index = (buffer[where + ((twiddleMap[x >> 1] << 1) | twiddleMap[y >> 1])]) * 4;

                    for (int x2 = 0; x2 < 2; x2++)
                    {
                        for (int y2 = 0; y2 < 2; y2++)
                        {
                            destinationIndex = ((((y + y2) * width) + (x + x2)) * 4);

                            for (int i = 0; i < 4; i++)
                            {
                                destination[destinationIndex] = (byte)(index + i);
                                destinationIndex++;
                            }

                            index++;
                        }
                    }
                }
            }
#endif

            return destination;
        }

        public static byte[] UnSwizzleVQ4(byte[] buffer, int width, int height, int where)
        {
            var result = new byte[width * height];
            int destinationIndex;

            // Get the size of each block to process.
            int size = Math.Min(width, height);

            // Twiddle map
            int[] twiddleMap = MakeTwiddleMap(size);

            // Decode texture data
            for (int y = 0; y < height; y += size)
            {
                for (int x = 0; x < width; x += size)
                {
                    for (int y2 = 0; y2 < size; y2++)
                    {
                        for (int x2 = 0; x2 < size; x2++)
                        {
                            byte index = (byte)((buffer[where + (((twiddleMap[x2] << 1) | twiddleMap[y2]) >> 1)] >> ((y2 & 0x1))) & 0xF);
                            destinationIndex = ((((y + y2) * width) + (x + x2)));

                            result[destinationIndex] = index;
                        }
                    }

                    where += size;
                }
            }

            return result;
        }
    }
}