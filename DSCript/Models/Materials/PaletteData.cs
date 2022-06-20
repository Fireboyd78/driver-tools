using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace DSCript.Models
{
    public class PaletteData
    {
        public int Count { get; }

        public byte[] Data { get; set; }

        public void Read(Stream stream)
        {
            stream.Read(Data, 0, Data.Length);
        }

        public void WriteTo(Stream stream)
        {
            stream.Write(Data, 0, Data.Length);
        }

        public PaletteData Clone()
        {
            var size = Data.Length;
            var data = new byte[size];

            Buffer.BlockCopy(Data, 0, data, 0, size);

            return new PaletteData(Count, data);
        }

        public void ToAlphaMask()
        {
            for (int i = 0; i < Count; i++)
            {
                var idx = (i * 4);

                var r = Data[idx + 2];
                var g = Data[idx + 1];
                var b = Data[idx + 0];
                
                // get the average of all channels
                var a = (byte)((r + g + b) / 3);

                // set RGB to black, set the alpha
                Data[idx + 2] = 0;
                Data[idx + 1] = 0;
                Data[idx + 0] = 0;
                Data[idx + 3] = a;
            }
        }

        public void Blend(int slotDst, PaletteData palette, int slotSrc = -1)
        {
            if (palette.Count != Count)
                throw new InvalidDataException($"Can't blend palette with {palette.Count} colors into palette with {Count} colors!");

            if (slotSrc == -1)
                slotSrc = slotDst;

            if ((slotDst & 0x3) != slotDst)
                throw new ArgumentOutOfRangeException(nameof(slotDst), "Bad RGBA color slot destination!");
            if ((slotSrc & 0x3) != slotSrc)
                throw new ArgumentOutOfRangeException(nameof(slotSrc), "Bad RGBA color slot source!");

            // RGBA->BGRA
            int[] remap = { 2, 1, 0, 3 };

            // remap the slots
            slotDst = remap[slotDst];
            slotSrc = remap[slotSrc];

            for (int i = 0; i < Count; i++)
            {
                var idx = (i * 4);

                Data[idx + slotDst] = (byte)(0xFF - (Data[idx + slotDst] - palette.Data[idx + slotSrc]));
            }
        }

        public void Merge(int slotDst, PaletteData palette, int slotSrc = -1)
        {
            if (palette.Count != Count)
                throw new InvalidDataException($"Can't merge palette with {palette.Count} colors into palette with {Count} colors!");

            if (slotSrc == -1)
                slotSrc = slotDst;

            if ((slotDst & 0x3) != slotDst)
                throw new ArgumentOutOfRangeException(nameof(slotDst), "Bad RGBA color slot destination!");
            if ((slotSrc & 0x3) != slotSrc)
                throw new ArgumentOutOfRangeException(nameof(slotSrc), "Bad RGBA color slot source!");

            // RGBA->BGRA
            int[] remap = { 2, 1, 0, 3 };

            // remap the slots
            slotDst = remap[slotDst];
            slotSrc = remap[slotSrc];

            for (int i = 0; i < Count; i++)
            {
                var idx = (i * 4);

                Data[idx + slotDst] = palette.Data[idx + slotSrc];
            }
        }

        public PaletteData(int count)
        {
            Count = count;
            Data = new byte[count * 4];
        }

        public PaletteData(int count, byte[] data)
        {
            Count = count;
            Data = data;
        }

        public PaletteData(ref PaletteInfo palette)
        {
            Count = palette.Count;
            Data = new byte[palette.DataSize];
        }
    }
}