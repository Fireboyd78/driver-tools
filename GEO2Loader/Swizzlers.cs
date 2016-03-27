using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GEO2Loader
{
    public enum SwizzleType
    {
        Swizzle4bit,
        Swizzle8bit
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

            byte[] pSwizTexels = new byte[pInTexels.Length];

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
                    uint swapSelector = (uint)(((y + 2) >> 2) & 0x1) * 4;
                    int posY = (((y & (~3)) >> 1) + (y & 1)) & 0x7;

                    int columnLocation = posY * height * 2 + ((x + (int)swapSelector) & 0x7) * 4;

                    int byteNum = (x >> 3) & 3; // 0, 1, 2, 3
                    int bitsSet = (y >> 1) & 1; // 0, 1

                    byte setPixel = pSwizTexels[pageLocation + blockLocation + columnLocation + byteNum];

                    pSwizTexels[pageLocation + blockLocation + columnLocation + byteNum] = (byte)((setPixel & (-bitsSet)) | (uPen << (bitsSet * 4)));
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
            // Don't swizzle if size if width or height is less than 128
            if (width < 128 || height < 128)
                return new byte[] { };

            // Make a copy of the swizzled input and clear buffer
            byte[] pSwizTexels = new byte[buffer.Length - where];
            Array.Copy(buffer, where, pSwizTexels, 0, pSwizTexels.Length);

            buffer = new byte[buffer.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // get the pen
                    int index = y * width + x;
                    //byte uPen = (byte)(Swizzled[index >> 1] >> ((index & 1) * 4) & 0xF);

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
                    uint swapSelector = (uint)(((y + 2) >> 2) & 0x1) * 4;
                    int posY = (((y & (~3)) >> 1) + (y & 1)) & 0x7;

                    int columnLocation = posY * height * 2 + ((x + (int)swapSelector) & 0x7) * 4;

                    int byteNum = (x >> 3) & 3;     // 0,1,2,3
                    int bitsSet = (y >> 1) & 1;     // 0,1            (lower/upper 4 bits)

                    byte setPixel = (byte)((buffer[(index >> 1)] >> ((index & 1) * 4)) & 0xF);

                    byte uPen = (byte)(pSwizTexels[pageLocation + blockLocation + columnLocation + byteNum]);

                    buffer[(index >> 1)] = (byte)((setPixel & -bitsSet) | (uPen << (bitsSet * 4)));
                }
            }
            return buffer;
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
    }
}