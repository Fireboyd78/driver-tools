using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Audiose
{
    public sealed class ADPCM
    {
        static readonly int[] StepTable = {
            7, 8, 9, 10, 11, 12, 13, 14,
            16, 17, 19, 21, 23, 25, 28, 31,
            34, 37, 41, 45, 50, 55, 60, 66,
            73, 80, 88, 97, 107, 118, 130, 143,
            157, 173, 190, 209, 230, 253, 279, 307,
            337, 371, 408, 449, 494, 544, 598, 658,
            724, 796, 876, 963, 1060, 1166, 1282, 1411,
            1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024,
            3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484,
            7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
            15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794,
            32767
        };

        static readonly int[] IndexTable = {
            -1, -1, -1, -1, 2, 4, 6, 8,
            -1, -1, -1, -1, 2, 4, 6, 8,
        };
        
        public static unsafe byte[] Decode(byte[] buffer, int sample_count, int sample_size)
        {
            // allocate just enough space to decompress the data
            // then resize it once we finish
            var result = new byte[buffer.Length * 4];
            
            var c = (sample_size - 8);

            // these names are shit :/
            var channel_size = (c / 2);
            var chunk_size = (c * 4);

            // idk wtf im doing
            var count = (buffer.Length / sample_count);
            
            fixed (byte *pSrc = buffer)
            fixed (byte *pDst = result)
            {
                byte *ptr = pSrc;
                byte *dest = pDst;

                for (int n = 0; n < count; n++)
                {
                    var num_bytes = sample_count;

                    do
                    {
                        short[] channel = {
                            *(short *)(ptr + 0),
                            *(short *)(ptr + 4),
                        };

                        int[] index = {
                            *(ptr + 2),
                            *(ptr + 6),
                        };

                        *(short*)(dest + 0) = channel[0];
                        *(short*)(dest + 2) = channel[1];

                        dest += 4;
                        ptr += 8;

                        var channel_ptr = (short*)dest;

                        for (int ch = 0; ch < 2; ch++)
                        {
                            short* pcm_data = channel_ptr;

                            if (channel_size > 0)
                            {
                                var step_index = index[ch];

                                for (int sample_index = 0; sample_index < channel_size; sample_index++)
                                {
                                    for (int i = 0; i < 2; i++)
                                    {
                                        var step = StepTable[step_index];
                                        var delta = (step >> 3);

                                        var nibble = (*ptr >> (i * 4)) & 0xF;

                                        if ((nibble & 1) != 0) delta += (step >> 2);
                                        if ((nibble & 2) != 0) delta += (step >> 1);
                                        if ((nibble & 4) != 0) delta += step;
                                        if ((nibble & 8) != 0) delta = -delta;

                                        delta += channel[ch];
                                        delta = Math.Min(32767, Math.Max(-32768, delta));
                                        
                                        channel[ch] = (*pcm_data = (short)delta);

                                        step_index += IndexTable[nibble];
                                        step_index = Math.Min(88, Math.Max(0, step_index));
                                        
                                        pcm_data += 2;
                                    }

                                    ptr++;
                                }

                                index[ch] = step_index;
                            }

                            channel_ptr++;
                        }

                        dest += chunk_size;
                        num_bytes -= sample_size;

                    } while (num_bytes > sample_size);
                }

                // make sure we don't leave any dead noise
                var data_size = (int)(dest - pDst);

                Array.Resize(ref result, data_size);
            }

            return result;
        }
    }
}
