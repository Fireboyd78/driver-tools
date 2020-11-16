using System;

namespace Audiose
{
    public static class VAG
    {
        // PSX ADPCM coefficients
        private static readonly double[] K0 = { 0, 0.9375, 1.796875, 1.53125, 1.90625 };
        private static readonly double[] K1 = { 0, 0, -0.8125, -0.859375, -0.9375 };

        // PSX ADPCM decoding routine - decodes a single sample
        public static short VagToPCM(byte soundParameter, int soundData, ref double vagPrev1, ref double vagPrev2)
        {
            if (soundData > 7)
                soundData -= 16;

            var sp1 = (soundParameter >> 0) & 0xF;
            var sp2 = (soundParameter >> 4) & 0xF;

            var dTmp1 = soundData * Math.Pow(2.0, (12.0 - sp1));

            var dTmp2 = vagPrev1 * K0[sp2];
            var dTmp3 = vagPrev2 * K1[sp2];

            vagPrev2 = vagPrev1;
            vagPrev1 = dTmp1 + dTmp2 + dTmp3;

            var result = (int)Math.Round(vagPrev1);

            return (short)Math.Min(32767, Math.Max(-32768, result));
        }
        
        public static unsafe byte[] DecodeSound(byte[] buffer, SamplerData sampler = null)
        {
            int numSamples = (buffer.Length >> 4) * 28; // PSX ADPCM data is stored in blocks of 16 bytes each containing 28 samples.

            int loopStart = 0;
            int loopLength = 0;

            var result = new byte[numSamples * 2];

            byte sp = 0;

            double vagPrev1 = 0.0;
            double vagPrev2 = 0.0;
            
            int k = 0;

            fixed (byte* r = result)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (i % 16 == 0)
                    {
                        var ld1 = buffer[i];
                        var ld2 = buffer[i + 1];

                        sp = ld1;

                        if ((ld2 & 0xE) == 6)
                            loopStart = k;

                        if ((ld2 & 0xF) == 3 || (ld2 & 0xF) == 7)
                            loopLength = (k + 28) - loopStart;

                        i += 2;
                    }
                    
                    for (int s = 0; s < 2; s++)
                    {
                        var sd = (buffer[i] >> (s * 4)) & 0xF;

                        ((short*)r)[k++] = VagToPCM(sp, sd, ref vagPrev1, ref vagPrev2);
                    }
                }
            }

            if (sampler != null)
            {
                sampler.Buffer = result;

                if (loopLength > 0)
                {
                    sampler.Loop.Start = loopStart;
                    sampler.Loop.End = (loopStart + loopLength);
                }

                result = sampler.Compile();
            }

            return result;
        }
    }
}
